using System;
using System.Collections.Generic;
using Examine;
using Governor.Umbraco.FullTextSearch.Utilities;
using umbraco.cms.businesslogic.web;
using UmbracoExamine;
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
                RenderAllNodesToCache();
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
            var documents = Document.GetRootDocuments();
            var indexer = ExamineManager.Instance.IndexProviderCollection[Config.Instance.GetByKey("IndexProvider")];
            if (documents != null && indexer != null)
            {
                if (Config.Instance.GetBooleanByKey("PublishEventRendering"))
                {
                    Library.SetTimeout(Config.Instance.GetByKey("ScriptTimeout"));
                    RenderAllNodesToCache();
                }
                foreach (var d in documents)
                {
                    RecursiveIndexNodes(indexer, d);
                }
            }
        }
        /// <summary>
        /// Re-index all supplied nodes in the full text index, and all their descendants
        /// </summary>
        /// <param name="nodes"></param>
        public static void ReindexFullTextNodesAndChildren(int[] nodes)
        {
            var indexer = ExamineManager.Instance.IndexProviderCollection[Config.Instance.GetByKey("IndexProvider")];
            if (indexer != null && nodes != null && nodes.Length > 0)
            {
                if (Config.Instance.GetBooleanByKey("PublishEventRendering"))
                {
                    Library.SetTimeout(Config.Instance.GetByKey("ScriptTimeout"));
                    foreach (var node in nodes)
                    {
                        RenderNodeAndChildrenToCache(node);
                    }
                }
                foreach (var nodeId in nodes)
                {
                    if (Document.IsDocument(nodeId))
                    {
                        var node = new Document(nodeId);
                        RecursiveIndexNodes(indexer, node);
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
                foreach (var node in nodes)
                {
                    RenderNodeToCache(node);
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
            var indexer = ExamineManager.Instance.IndexProviderCollection[index];
            foreach (var node in nodes)
            {
                if (Document.IsDocument(node))
                {
                    var doc = new Document(node);
                    ReIndexNode(indexer, doc);
                }
            }
        }
        /// <summary>
        /// reindex this single document in the supplied index
        /// </summary>
        /// <param name="indexer"></param>
        /// <param name="doc"></param>
        protected static void ReIndexNode(BaseIndexProvider indexer, Document doc)
        {
            if (doc != null)
            {
                var xDocument = doc.ToXDocument(false);
                if (xDocument != null)
                {
                    var xElement = xDocument.Root;
                    try
                    {
                        indexer.ReIndexNode(xElement, IndexTypes.Content);
                    }
                    catch (Exception ex)
                    {
                        if (Library.IsCritical(ex))
                            throw;
                    }
                }
            }
        }
        // Requres valid HTTP context
        /// <summary>
        /// Render single node ID to cache
        /// </summary>
        /// <param name="nodeId"></param>
        public static void RenderNodeToCache(int nodeId)
        {
            if (Document.IsDocument(nodeId))
            {
                var doc = new Document(nodeId);
                if (doc.Published && ! doc.IsTrashed)
                {
                    RenderNodeToCache(doc);
                }
            }
        }

        /// <summary>
        /// Render all nodes to cache
        /// </summary>
        public static void RenderAllNodesToCache()
        {
            var documents = Document.GetRootDocuments();
            if (documents != null)
            {
                foreach (var d in documents)
                {
                    RenderNodeAndChildrenToCache(d);
                }
            }
        }

        /// <summary>
        /// Render the given node ID and all children to cache
        /// </summary>
        /// <param name="nodeId"></param>
        public static void RenderNodeAndChildrenToCache(int nodeId)
        {
            if (nodeId > 0)
            {
                if (Document.IsDocument(nodeId))
                {
                    var node = new Document(nodeId);
                    if(node.Published && ! node.IsTrashed)
                        RenderNodeAndChildrenToCache(node);
                }
            }
        }
        /// <summary>
        /// Helper function for ReindexAllFullTextNodes
        /// </summary>
        /// <param name="indexer"></param>
        /// <param name="document"></param>
        protected static void RecursiveIndexNodes(BaseIndexProvider indexer, Document document)
        {
            if (document != null && document.Published && ! document.IsTrashed)
            {
                ReIndexNode(indexer, document);
                if (document.HasChildren)
                {
                    foreach (var child in document.Children)
                    {
                        RecursiveIndexNodes(indexer, child);
                    }
                }
            }
        }

        /// <summary>
        /// Render a single document to cache
        /// </summary>
        /// <param name="doc"></param>
        protected static void RenderNodeToCache(Document doc)
        {
            if (doc != null && doc.IsTrashed != true && doc.Published)
            {
                /*if (doc.PublishWithResult(user))
                {
                    umbraco.library.UpdateDocumentCache(doc.Id);
                }*/
                var nodeTypeAlias = doc.ContentType.Alias;
                var renderer = Manager.Instance.DocumentRendererFactory.CreateNew(nodeTypeAlias);
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
        /// <param name="node"></param>
        protected static void RenderNodeAndChildrenToCache(Document node)
        {
            if (node != null && node.Published && ! node.IsTrashed)
            {
                RenderNodeToCache(node);
                if (node.HasChildren)
                {
                    foreach (var child in node.Children)
                    {
                        RenderNodeAndChildrenToCache(child);
                    }
                }
            }
        }
    }
}