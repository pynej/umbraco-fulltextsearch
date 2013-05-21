using System;
using System.Collections.Generic;
using Examine;
using Governor.Umbraco.FullTextSearch.Interfaces;
using Governor.Umbraco.FullTextSearch.Utilities;
using umbraco.cms.businesslogic.web;
using UmbracoExamine;
using System.Xml.Linq;
using Examine.Providers;

namespace Governor.Umbraco.FullTextSearch.Admin
{
    public class AdminActions
    {
        /// <summary>
        /// Rebuild the entire full text index. Re-render nodes if necessary
        /// </summary>
        public static void RebuildFullTextIndex()
        {
            if (Config.Instance.GetBooleanByKey("PublishEventRendering"))
            {
                Library.SetTimeout(Config.Instance.GetByKey("ScriptTimeout"));
                AdminActions.RenderAllNodesToCache();
            }
            RebuildIndex(Config.Instance.GetByKey("IndexProvider"));
        }
        /// <summary>
        /// rebuild the entire index with supplied name
        /// </summary>
        /// <param name="index"></param>
        public static void RebuildIndex(string index)
        {
            var indexer = ExamineManager.Instance.IndexProviderCollection[index];
            if (indexer != null)
            {
                indexer.RebuildIndex();
            }
        }
        /// <summary>
        /// Re-index all nodes in the full text index
        /// </summary>
        public static void ReindexAllFullTextNodes()
        {
            Document[] documents = Document.GetRootDocuments();
            BaseIndexProvider indexer = ExamineManager.Instance.IndexProviderCollection[Config.Instance.GetByKey("IndexProvider")];
            if (documents != null && indexer != null)
            {
                if (Config.Instance.GetBooleanByKey("PublishEventRendering"))
                {
                    Library.SetTimeout(Config.Instance.GetByKey("ScriptTimeout"));
                    AdminActions.RenderAllNodesToCache();
                }
                foreach (Document d in documents)
                {
                    recursiveIndexNodes(indexer, d);
                }
            }
        }
        /// <summary>
        /// Re-index all supplied nodes in the full text index, and all their descendants
        /// </summary>
        /// <param name="nodes"></param>
        public static void ReindexFullTextNodesAndChildren(int[] nodes)
        {
            BaseIndexProvider indexer = ExamineManager.Instance.IndexProviderCollection[Config.Instance.GetByKey("IndexProvider")];
            if (indexer != null && nodes != null && nodes.Length > 0)
            {
                if (Config.Instance.GetBooleanByKey("PublishEventRendering"))
                {
                    Library.SetTimeout(Config.Instance.GetByKey("ScriptTimeout"));
                    foreach (int node in nodes)
                    {
                        AdminActions.RenderNodeAndChildrenToCache(node);
                    }
                }
                foreach (int nodeId in nodes)
                {
                    if (Document.IsDocument(nodeId))
                    {
                        Document node = new Document(nodeId);
                        recursiveIndexNodes(indexer, node);
                    }
                }
            }
        }
        /// <summary>
        /// reindex the supplied list of full text nodes
        /// </summary>
        /// <param name="nodes"></param>
        public static void ReindexFullTextNodes(List<int> nodes)
        {
            if (Config.Instance.GetBooleanByKey("PublishEventRendering"))
            {
                Library.SetTimeout(Config.Instance.GetByKey("ScriptTimeout"));
                foreach (int node in nodes)
                {
                    AdminActions.RenderNodeToCache(node);
                }
            }
            ReindexNodes(Config.Instance.GetByKey("IndexProvider"), nodes);
        }
        /// <summary>
        /// reindex the supplied list of nodes in the given index
        /// </summary>
        /// <param name="index"></param>
        /// <param name="nodes"></param>
        public static void ReindexNodes(string index, List<int> nodes)
        {
            BaseIndexProvider indexer = ExamineManager.Instance.IndexProviderCollection[index];
            foreach (int node in nodes)
            {
                if (Document.IsDocument(node))
                {
                    Document doc = new Document(node);
                    reIndexNode(indexer, doc);
                }
            }
        }
        /// <summary>
        /// reindex this single document in the supplied index
        /// </summary>
        /// <param name="indexer"></param>
        /// <param name="doc"></param>
        protected static void reIndexNode(BaseIndexProvider indexer, Document doc)
        {
            if (doc != null)
            {
                XDocument xDocument = doc.ToXDocument(false);
                if (xDocument != null)
                {
                    XElement xElement = xDocument.Root;
                    try
                    {
                        indexer.ReIndexNode(xElement, IndexTypes.Content);
                    }
                    catch (Exception ex)
                    {
                        if (global::Governor.Umbraco.FullTextSearch.Utilities.Library.IsCritical(ex))
                            throw;
                    }
                }
            }
        }
        // Requres valid HTTP context
        /// <summary>
        /// Render single node ID to cache
        /// </summary>
        /// <param name="user"></param>
        /// <param name="nodeId"></param>
        public static void RenderNodeToCache(int nodeId)
        {
            if (Document.IsDocument(nodeId))
            {
                Document doc = new Document(nodeId);
                if (doc != null && doc.Published && ! doc.IsTrashed)
                {
                    renderNodeToCache(doc);
                }
            }
        }
        /// <summary>
        /// Render all nodes to cache
        /// </summary>
        /// <param name="user"></param>
        public static void RenderAllNodesToCache()
        {
            Document[] documents = umbraco.cms.businesslogic.web.Document.GetRootDocuments();
            if (documents != null)
            {
                foreach (Document d in documents)
                {
                    renderNodeAndChildrenToCache(d);
                }
            }
        }
        /// <summary>
        /// Render the given node ID and all children to cache
        /// </summary>
        /// <param name="user"></param>
        /// <param name="nodeId"></param>
        public static void RenderNodeAndChildrenToCache(int nodeId)
        {
            if (nodeId > 0)
            {
                if (Document.IsDocument(nodeId))
                {
                    Document node = new Document(nodeId);
                    if(node.Published && ! node.IsTrashed)
                        renderNodeAndChildrenToCache(node);
                }
            }
        }
        /// <summary>
        /// Helper function for ReindexAllFullTextNodes
        /// </summary>
        /// <param name="indexer"></param>
        /// <param name="document"></param>
        protected static void recursiveIndexNodes(BaseIndexProvider indexer, Document document)
        {
            if (document != null && document.Published && ! document.IsTrashed)
            {
                reIndexNode(indexer, document);
                if (document.HasChildren)
                {
                    foreach (Document child in document.Children)
                    {
                        recursiveIndexNodes(indexer, child);
                    }
                }
            }
        }
        /// <summary>
        /// Render a single document to cache
        /// </summary>
        /// <param name="user"></param>
        /// <param name="doc"></param>
        protected static void renderNodeToCache(Document doc)
        {
            if (doc != null && doc.IsTrashed != true && doc.Published == true)
            {
                /*if (doc.PublishWithResult(user))
                {
                    umbraco.library.UpdateDocumentCache(doc.Id);
                }*/
                string nodeTypeAlias = doc.ContentType.Alias;
                IDocumentRenderer renderer = Manager.Instance.DocumentRendererFactory.CreateNew(nodeTypeAlias);
                string fullHtml;
                if (renderer.Render(doc.Id, out fullHtml))
                    HtmlCache.Store(doc.Id, ref fullHtml);
                else
                    HtmlCache.Remove(doc.Id);
            }
        }
        /// <summary>
        /// Render a document and all it's children to cache
        /// </summary>
        /// <param name="user"></param>
        /// <param name="node"></param>
        protected static void renderNodeAndChildrenToCache(Document node)
        {
            if (node != null && node.Published && ! node.IsTrashed)
            {
                renderNodeToCache(node);
                if (node.HasChildren)
                {
                    foreach (Document child in node.Children)
                    {
                        renderNodeAndChildrenToCache(child);
                    }
                }
            }
        }
    }
}