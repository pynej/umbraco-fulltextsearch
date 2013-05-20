using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FullTextSearch.Interfaces;
using umbraco.cms.businesslogic.web;
using Examine;
using FullTextSearch.Utilities;

namespace FullTextSearch.FullTextIndexers
{
    /// <summary>
    /// Default indexer class. Used for all indexers in this project
    /// </summary>
    public class DefaultIndexer : IFullTextIndexer
    {
        protected Document currentDocument;

        /// <summary>
        /// Fully process the current node, check whether to cancel indexing, check whether to index the node
        /// retrieve the HTML and add it to the index. Then make a cup of tea. This is tiring. 
        /// </summary>
        /// <param name="e">IndexingNodeDataEventArgs from examine</param>
        public virtual void NodeProcessor(Document currentDocument, Dictionary<string,string> fields, out bool cancelIndexing)
        {
            cancelIndexing = false;
            // this can take a while, if we're running sync this is needed
            Library.SetTimeout(Config.Instance.GetByKey("ScriptTimeout"));
            if (currentDocument == null)
                return;
            this.currentDocument = currentDocument;
            string fullHtml;
            if (checkCancelIndexing())
            {
                cancelIndexing = true;
                return;
            }
            fields.Add(Config.Instance.GetPathPropertyName(), getPath());
            if(isIndexable())
                if(getHtml(out fullHtml))
                    fields.Add(Config.Instance.GetLuceneFTField(), getTextFromHtml(ref fullHtml));
        }
        /// <summary>
        /// Check whether to cancel indexing or not(generally if umbraco(Search/Navi/etc)Hide is set)
        /// </summary>
        /// <returns></returns>
        protected virtual bool checkCancelIndexing()
        {
            if (Library.IsSearchDisabledByProperty(currentDocument))
                return true;
            return false;
        }
        /// <summary>
        /// I'm pretty much assuming if we're here and we have a valid document object we should be
        /// trying to index, 
        /// </summary>
        /// <returns></returns>
        protected virtual bool isIndexable()
        {
            if (currentDocument == null)
                return false;
            return true;
        }
        protected virtual string getPath()
        {
            string path = currentDocument.Path.Replace(',', ' ');
            path = System.Text.RegularExpressions.Regex.Replace(path, @"^-1 ", string.Empty);
            return path;
        }
        /// <summary>
        /// Get the actual HTML, we use the DefaultHttpRenderer here ususally, unless it's been overriden, 
        /// in which case it should be noted that it should only be overriden by renderers using only the 
        /// </summary>
        /// <param name="fullHtml"></param>
        /// <returns></returns>
        protected virtual bool getHtml(out string fullHtml)
        {
            IDocumentRenderer renderer = Manager.Instance.DocumentRendererFactory.CreateNew(currentDocument.ContentType.Alias);
            fullHtml = "";
            if (renderer.Render(currentDocument.Id, out fullHtml))
                return true;
            return false;
        }
        /// <summary>
        /// Use Html Tag stripper to get text from the passed HTML. Certain tags specified in the
        /// config gile get removed entirely, head, script, possibly some relevant ids etc.
        /// </summary>
        /// <param name="fullHtml"></param>
        /// <returns>Text to add to index</returns>
        protected virtual string getTextFromHtml(ref string fullHtml)
        {
            Config config = Config.Instance;
            string[] tagsToStrip = config.GetMultiByKey("TagsToRemove").ToArray();
            string[] idsToStrip = config.GetMultiByKey("IdsToRemove").ToArray();

            HtmlStrip tagStripper = new HtmlStrip(tagsToStrip, idsToStrip,true);
            return tagStripper.TextFromHTML(ref fullHtml);
        }
    }
}