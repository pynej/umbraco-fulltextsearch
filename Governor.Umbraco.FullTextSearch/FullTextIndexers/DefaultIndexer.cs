using System.Collections.Generic;
using Governor.Umbraco.FullTextSearch.Interfaces;
using Governor.Umbraco.FullTextSearch.Utilities;
using umbraco.cms.businesslogic.web;

namespace Governor.Umbraco.FullTextSearch.FullTextIndexers
{
    /// <summary>
    /// Default indexer class. Used for all indexers in this project
    /// </summary>
    public class DefaultIndexer : IFullTextIndexer
    {
        protected Document CurrentDocument;

        /// <summary>
        /// Fully process the current node, check whether to cancel indexing, check whether to index the node
        /// retrieve the HTML and add it to the index. Then make a cup of tea. This is tiring. 
        /// </summary>
        /// <param name="currentDocument"></param>
        /// <param name="fields"></param>
        /// <param name="cancelIndexing"></param>
        public virtual void NodeProcessor(Document currentDocument, Dictionary<string,string> fields, out bool cancelIndexing)
        {
            cancelIndexing = false;
            // this can take a while, if we're running sync this is needed
            Library.SetTimeout(Config.Instance.GetByKey("ScriptTimeout"));
            if (currentDocument == null)
                return;
            CurrentDocument = currentDocument;
            string fullHtml;
            if (CheckCancelIndexing())
            {
                cancelIndexing = true;
                return;
            }
            fields.Add(Config.Instance.GetPathPropertyName(), GetPath());
            if(IsIndexable())
                if(GetHtml(out fullHtml))
                    fields.Add(Config.Instance.GetLuceneFtField(), GetTextFromHtml(ref fullHtml));
        }
        /// <summary>
        /// Check whether to cancel indexing or not(generally if umbraco(Search/Navi/etc)Hide is set)
        /// </summary>
        /// <returns></returns>
        protected virtual bool CheckCancelIndexing()
        {
            return Library.IsSearchDisabledByProperty(CurrentDocument);
        }

        /// <summary>
        /// I'm pretty much assuming if we're here and we have a valid document object we should be
        /// trying to index, 
        /// </summary>
        /// <returns></returns>
        protected virtual bool IsIndexable()
        {
            return CurrentDocument != null;
        }

        protected virtual string GetPath()
        {
            var path = CurrentDocument.Path.Replace(',', ' ');
            path = System.Text.RegularExpressions.Regex.Replace(path, @"^-1 ", string.Empty);
            return path;
        }
        /// <summary>
        /// Get the actual HTML, we use the DefaultHttpRenderer here ususally, unless it's been overriden, 
        /// in which case it should be noted that it should only be overriden by renderers using only the 
        /// </summary>
        /// <param name="fullHtml"></param>
        /// <returns></returns>
        protected virtual bool GetHtml(out string fullHtml)
        {
            var renderer = Manager.Instance.DocumentRendererFactory.CreateNew(CurrentDocument.ContentType.Alias);
            return renderer.Render(CurrentDocument.Id, out fullHtml);
        }
        /// <summary>
        /// Use Html Tag stripper to get text from the passed HTML. Certain tags specified in the
        /// config gile get removed entirely, head, script, possibly some relevant ids etc.
        /// </summary>
        /// <param name="fullHtml"></param>
        /// <returns>Text to add to index</returns>
        protected virtual string GetTextFromHtml(ref string fullHtml)
        {
            var config = Config.Instance;
            var tagsToStrip = config.GetMultiByKey("TagsToRemove").ToArray();
            var idsToStrip = config.GetMultiByKey("IdsToRemove").ToArray();

            var tagStripper = new HtmlStrip(tagsToStrip, idsToStrip);
            return tagStripper.TextFromHtml(ref fullHtml);
        }
    }
}