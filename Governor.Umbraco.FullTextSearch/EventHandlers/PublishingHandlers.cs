using System.Linq;
using Governor.Umbraco.FullTextSearch.Utilities;
using Umbraco.Core.Services;
using umbraco;
using umbraco.cms.businesslogic;
using umbraco.cms.businesslogic.web;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Web.UI.JavaScript;
using System.Web;
using System;
using System.Web.Routing;
using System.Collections.Generic;
using Governor.Umbraco.FullTextSearch.Controllers;
using Umbraco.Core.Logging;
using System.Web.Mvc;
using Umbraco.Web;
using Governor.Umbraco.FullTextSearch.Admin;

namespace Governor.Umbraco.FullTextSearch.EventHandlers
{
    public class PublishingHandlers : ApplicationEventHandler
    {
        /// <summary>
        /// The event this handles fires after a document is published in the back office and the cache is updated.
        /// We render out the page and store it's HTML in the database for retrieval by the indexer.
        /// </summary>
        /// <param name="sender">Document being published</param>
        /// <param name="e">Event Arguments</param>
        /// <remarks>
        /// the indexer thread doesn't always access to a fully initialised umbraco core to do the rendering, 
        /// whereas this event always should, hence this method rather than doing both rendering and indexing
        /// in the same thread
        /// </remarks>
        private void ContentAfterUpdateDocumentCache(Document sender, DocumentCacheEventArgs e)
        {
            if (sender == null || sender.Id < 1)
                return;
            var id = sender.Id;
            // get config and check we're enabled and good to go
            if (!CheckConfig())
                return;
            // this can take a while...
            Library.SetTimeout(Config.Instance.GetByKey("ScriptTimeout"));
            var nodeTypeAlias = sender.ContentType.Alias;

            var renderer = Manager.Instance.DocumentRendererFactory.CreateNew(nodeTypeAlias);
            string fullHtml;

            if (renderer.Render(id, out fullHtml))
                HtmlCache.Store(id, ref fullHtml);
            else
                HtmlCache.Remove(id);
        }

        /// <summary>
        /// Check that the config exists and rendering to cache on publish events is enabled
        /// </summary>
        /// <returns></returns>
        private bool CheckConfig()
        {
            var config = Config.Instance;
            if (config == null)
                return false;
            return Config.Instance.GetBooleanByKey("Enabled") && Config.Instance.GetBooleanByKey("PublishEventRendering");
        }

        /// <summary>
        /// OnApplicationStarted handler - subscribes to umbraco publishing events to build a database containing current HTML for
        /// each page using the umbraco core when publisheventrendering is active
        /// </summary>
        /// <param name="umbracoApplication"></param>
        /// <param name="applicationContext"></param>
        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            // These events alwayse fire.
            ServerVariablesParser.Parsing += ServerVariablesParser_Parsing;

            if (!CheckConfig())
                return;

            // These events onlt fire is using PublishEventRendering
            ContentService.Publishing += ContentService_Publishing;
            content.AfterUpdateDocumentCache += ContentAfterUpdateDocumentCache;
            ContentService.Deleted += ContentServiceDeleted;
            ContentService.Trashed += ContentServiceTrashed;
            ContentService.UnPublished += ContentServiceUnPublished;
        }

        private void ServerVariablesParser_Parsing(object sender, System.Collections.Generic.Dictionary<string, object> e)
        {
            if (HttpContext.Current == null) throw new InvalidOperationException("HttpContext is null");

            var urlHelper = new UrlHelper(new RequestContext(new HttpContextWrapper(HttpContext.Current), new RouteData()));

            ((Dictionary<string, object>)e["umbracoUrls"]).Add("fullTextSearchApiBaseUrl",
                urlHelper.GetUmbracoApiServiceBaseUrl<FullTextApiController>(controller => controller.IsAvailable()));

            LogHelper.Info<FullTextApiController>("Api Service Added");
        }

        /// <summary>
        /// Republishing all nodes tends to throw timeouts if you have enough of them. This 
        /// should prevent that without modifying the default for the whole site...
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ContentService_Publishing(global::Umbraco.Core.Publishing.IPublishingStrategy sender, global::Umbraco.Core.Events.PublishEventArgs<IContent> e)
        {
            Library.SetTimeout(Config.Instance.GetByKey("ScriptTimeout"));
        }

        /// <summary>
        /// Make sure HTML is deleted from storage when the node is
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ContentServiceDeleted(IContentService sender, global::Umbraco.Core.Events.DeleteEventArgs<IContent> e)
        {
            if (!CheckConfig())
                return;

            foreach (var content in e.DeletedEntities.Where(content => content.Id > 0))
            {
                HtmlCache.Remove(content.Id);
            }
        }

        /// <summary>
        /// Make sure HTML is deleted from storage when the node is moved to trash
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ContentServiceTrashed(IContentService sender, global::Umbraco.Core.Events.MoveEventArgs<IContent> e)
        {
            if (!CheckConfig())
                return;

            foreach (var content in e.MoveInfoCollection.Where(content => content.Entity.Id > 0))
            {
                HtmlCache.Remove(content.Entity.Id);
            }
        }

        /// <summary>
        /// Delete HTML on unpublish
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ContentServiceUnPublished(global::Umbraco.Core.Publishing.IPublishingStrategy sender, global::Umbraco.Core.Events.PublishEventArgs<IContent> e)
        {
            if (!CheckConfig())
                return;

            foreach (var content in e.PublishedEntities.Where(content => content.Id > 0))
            {
                HtmlCache.Remove(content.Id);
            }
        }
    }
}