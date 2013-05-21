using System;
using Examine;

namespace Governor.Umbraco.FullTextSearch.Extensions
{
    /// <summary>
    /// Allow modifying search results from code before they get sent to XSLT
    /// </summary>
    public class ResultOutputEventArgs : EventArgs
    {
        public SearchResult SearchResult { get; set; }
        public int ResultNumber { get; set; }
        public int PageNumber { get; set; }
        public int NumberOnPage { get; set; }

        public ResultOutputEventArgs(SearchResult searchResult, int pageNumber, int resultNumber, int numberOnPage)
        {
            this.SearchResult = searchResult;
            this.PageNumber = pageNumber;
            this.ResultNumber = resultNumber;
            this.NumberOnPage = numberOnPage;
        }
    }
}