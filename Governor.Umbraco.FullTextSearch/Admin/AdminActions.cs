using System;
using System.Collections.Generic;
using Examine;
using Governor.Umbraco.FullTextSearch.Utilities;
using Umbraco.Core;
using umbraco.cms.businesslogic.web;
using UmbracoExamine;
using Examine.Providers;
using Umbraco.Core.Services;
using Umbraco.Core.Models;

namespace Governor.Umbraco.FullTextSearch.Admin
{
    public class AdminActions
    {
        private static ServiceContext Services
        {
            get
            {
                return ApplicationContext.Current.Services;
            }
        }

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
            var content = Services.ContentService.GetRootContent();
            var indexer = ExamineManager.Instance.IndexProviderCollection[Config.Instance.GetByKey("IndexProvider")];
            if (content != null && indexer != null)
            {
                if (Config.Instance.GetBooleanByKey("PublishEventRendering"))
                {
                    Library.SetTimeout(Config.Instance.GetByKey("ScriptTimeout"));
                    RenderAllNodesToCache();
                }
                foreach (var c in content)
                {
                    RecursiveIndexNodes(indexer, c);
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
                    if (ApplicationContext.Current.Services.ContentService.GetById(nodeId) != null)
                    {
                        var node = ApplicationContext.Current.Services.ContentService.GetById(nodeId);
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
                if (ApplicationContext.Current.Services.ContentService.GetById(node) != null)
                {
                    var content = ApplicationContext.Current.Services.ContentService.GetById(node);
                    ReIndexNode(indexer, content);
                }
            }
        }
        /// <summary>
        /// reindex this single document in the supplied index
        /// </summary>
        /// <param name="indexer"></param>
        /// <param name="content"></param>
        protected static void ReIndexNode(BaseIndexProvider indexer, IContent content)
        {
            if (content != null)
            {
                var xElement = content.ToXml();
                if (xElement != null)
                {
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
            if (ApplicationContext.Current.Services.ContentService.GetById(nodeId) != null)
            {
                var content = ApplicationContext.Current.Services.ContentService.GetById(nodeId);
                if (content.Published && ! content.Trashed)
                {
                    RenderNodeToCache(content);
                }
            }
        }

        /// <summary>
        /// Render all nodes to cache
        /// </summary>
        public static void RenderAllNodesToCache()
        {
            var content = ApplicationContext.Current.Services.ContentService.GetRootContent();
            if (content != null)
            {
                foreach (var c in content)
                {
                    RenderNodeAndChildrenToCache(c);
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
                if (ApplicationContext.Current.Services.ContentService.GetById(nodeId) != null)
                {
                    var node = ApplicationContext.Current.Services.ContentService.GetById(nodeId);
                    if(node.Published && ! node.Trashed)
                        RenderNodeAndChildrenToCache(node);
                }
            }
        }
        /// <summary>
        /// Helper function for ReindexAllFullTextNodes
        /// </summary>
        /// <param name="indexer"></param>
        /// <param name="content"></param>
        protected static void RecursiveIndexNodes(BaseIndexProvider indexer, IContent content)
        {
            if (content != null && content.Published && ! content.Trashed)
            {
                ReIndexNode(indexer, (Content)content);
                if (ApplicationContext.Current.Services.ContentService.HasChildren(content.Id))
                {
                    foreach (var child in ApplicationContext.Current.Services.ContentService.GetChildren(content.Id))
                    {
                        RecursiveIndexNodes(indexer, child);
                    }
                }
            }
        }

        /// <summary>
        /// Render a single document to cache
        /// </summary>
        /// <param name="content"></param>
        protected static void RenderNodeToCache(IContent content)
        {
            if (content != null && content.Trashed != true && content.Published)
            {
                /*if (doc.PublishWithResult(user))
                {
                    umbraco.library.UpdateDocumentCache(doc.Id);
                }*/
                var nodeTypeAlias = content.ContentType.Alias;
                var renderer = Manager.Instance.DocumentRendererFactory.CreateNew(nodeTypeAlias);
                string fullHtml;
                if (renderer.Render(content.Id, out fullHtml))
                    HtmlCache.Store(content.Id, ref fullHtml);
                else
                    HtmlCache.Remove(content.Id);
            }
        }

        /// <summary>
        /// Render a document and all it's children to cache
        /// </summary>
        /// <param name="content"></param>
        protected static void RenderNodeAndChildrenToCache(IContent content)
        {
            if (content != null && content.Published && ! content.Trashed)
            {
                RenderNodeToCache(content);
                if (ApplicationContext.Current.Services.ContentService.HasChildren(content.Id))
                {
                    foreach (var child in ApplicationContext.Current.Services.ContentService.GetChildren(content.Id))
                    {
                        RenderNodeAndChildrenToCache(child);
                    }
                }
            }
        }
    }
}