using Governor.Umbraco.FullTextSearch.Utilities;

namespace Governor.Umbraco.FullTextSearch.FullTextIndexers
{
    /// <summary>
    /// This is used when publisheventrendering is active. HTML is just retrieved from the DB.
    /// </summary>
    public class CacheIndexer : DefaultIndexer
    {
        protected override bool GetHtml(out string fullHtml)
        {
            return HtmlCache.Retrieve(CurrentContent.Id, out fullHtml);
        }
    }
}