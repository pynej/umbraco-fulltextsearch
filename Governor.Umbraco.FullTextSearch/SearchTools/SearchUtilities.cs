using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;
using Lucene.Net.QueryParsers;

namespace FullTextSearch.SearchTools
{
    /// <summary>
    /// Helper functions for search stuff
    /// </summary>
    public class SearchUtilities
    {
        /// <summary>
        /// split the search term into component parts, separate on space
        /// acknowledge and use quoted queries entered by user, no other
        /// special constructs (+- OR AND) handled
        /// </summary>
        /// <param name="searchTerm">the search term to split</param>
        /// <returns>list of terms properly escaped</returns> 
        public static List<string> getSearchTermsSplit(string searchTerm)
        {
            List<string> terms = new List<string>();
            if (searchTerm.Contains('"'))
            {
                // pull any quoted bits out of the query string, escape them, and add to our terms list

                searchTerm = Regex.Replace(searchTerm, @"\s*""([^""]+)""\s*", delegate(Match match)
                {
                    if (match.Success && match.Groups.Count >= 2)
                    {
                        string term = QueryParser.Escape(match.Groups[1].Value);
                        if (!string.IsNullOrEmpty(term))
                            terms.Add('"' + term + '"');
                    }
                    return " ";
                });
            }
            // now handle simple spaces
            foreach (string term in searchTerm.Split(' '))
            {
                if (!string.IsNullOrEmpty(term))
                    terms.Add(QueryParser.Escape(term));
            }
            return terms;
        }
        /// <summary>
        /// Return the quoted and escaped search term as a list
        /// </summary>
        /// <param name="searchTerm"></param>
        /// <returns></returns>
        public static List<string> getSearchTermQuoted(string searchTerm)
        {
            return new List<string> { '"' + QueryParser.Escape(searchTerm) + '"' };
        }
        /// <summary>
        /// Return the escaped search term as a list
        /// </summary>
        /// <param name="searchTerm"></param>
        /// <returns></returns>
        public static List<string> getSearchTermEscaped(string searchTerm)
        {
            return new List<string> { QueryParser.Escape(searchTerm) };
        }
    }
}