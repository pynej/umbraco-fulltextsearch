using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Examine;

namespace FullTextSearch.XsltExtensions
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