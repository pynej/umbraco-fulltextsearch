using Governor.Umbraco.FullTextSearch.Interfaces;
using Governor.Umbraco.FullTextSearch.Utilities;
using umbraco;
using umbraco.cms.businesslogic;
using umbraco.cms.businesslogic.web;

namespace Governor.Umbraco.FullTextSearch.EventHandlers
{
    public class PublishingHandlers : umbraco.BusinessLogic.ApplicationBase
    {
        /// <summary>
        /// Constructor subscribes to umbraco publishing events to build a database containing current HTML for
        /// each page using the umbraco core when publisheventrendering is active
        /// </summary>
        public PublishingHandlers()
        {
            if (!checkConfig())
                return;
            Document.BeforePublish += Document_BeforePublish;
            content.AfterUpdateDocumentCache += content_AfterUpdateDocumentCache;
            Document.AfterDelete += Document_AfterDelete;
            Document.AfterMoveToTrash += Document_AfterMoveToTrash;
            Document.AfterUnPublish += Document_AfterUnPublish;
        }

        /// <summary>
        /// Republishing all nodes tends to throw timeouts if you have enough of them. This 
        /// should prevent that without modifying the default for the whole site...
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Document_BeforePublish(Document sender, PublishEventArgs e)
        {
            Library.SetTimeout(Config.Instance.GetByKey("ScriptTimeout"));
        }

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
        private void content_AfterUpdateDocumentCache(Document sender, DocumentCacheEventArgs e)
        {
            if (sender == null || sender.Id < 1)
                return;
            int id = sender.Id;
            // get config and check we're enabled and good to go
            if (!checkConfig())
                return;
            // this can take a while...
            Library.SetTimeout(Config.Instance.GetByKey("ScriptTimeout"));
            string nodeTypeAlias = sender.ContentType.Alias;

            IDocumentRenderer renderer = Manager.Instance.DocumentRendererFactory.CreateNew(nodeTypeAlias);
            string fullHtml;

            if (renderer.Render(id, out fullHtml))
                HtmlCache.Store(id, ref fullHtml);
            else
                HtmlCache.Remove(id);
        }

        /// <summary>
        /// Make sure HTML is deleted from storage when the node is
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Document_AfterDelete(Document sender, DeleteEventArgs e)
        {
            //FIXME: what happens when entire trees are deleted? does this get called multiple times?
            if (!checkConfig())
                return;
            var id = sender.Id;
            if (id > 0)
                HtmlCache.Remove(sender.Id);
        }

        /// <summary>
        /// Make sure HTML is deleted from storage when the node is moved to trash
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Document_AfterMoveToTrash(Document sender, MoveToTrashEventArgs e)
        {
            if (!checkConfig())
                return;
            int id = sender.Id;
            if (id > 0)
                HtmlCache.Remove(sender.Id);
        }

        /// <summary>
        /// Delete HTML on unpublish
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Document_AfterUnPublish(Document sender, UnPublishEventArgs e)
        {
            if (!checkConfig())
                return;
            var id = sender.Id;
            if (id > 0)
                HtmlCache.Remove(sender.Id);
        }

        /// <summary>
        /// Check that the config exists and rendering to cache on publish events is enabled
        /// </summary>
        /// <returns></returns>
        private bool checkConfig()
        {
            Config config = Config.Instance;
            if (config == null)
                return false;
            if (!Config.Instance.GetBooleanByKey("Enabled") || !Config.Instance.GetBooleanByKey("PublishEventRendering"))
                return false;
            return true;
        }
    }
}