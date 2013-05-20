using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FullTextSearch.Utilities;

namespace FullTextSearch.FullTextIndexers
{
    /// <summary>
    /// This is used when publisheventrendering is active. HTML is just retrieved from the DB.
    /// </summary>
    public class CacheIndexer : DefaultIndexer
    {
        protected override bool getHtml(out string fullHtml)
        {
            return HtmlCache.Retrieve(currentDocument.Id, out fullHtml);
        }
    }
}