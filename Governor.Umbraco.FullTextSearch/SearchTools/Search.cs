using System;
using System.Collections.Generic;
using System.Linq;
using Examine;
using System.Text;
using Lucene.Net.QueryParsers;
using System.Text.RegularExpressions;
using Examine.Providers;
using Lucene.Net.Highlight;
using Lucene.Net.Search;
using Lucene.Net.Index;
using Lucene.Net.Analysis;
using Examine.LuceneEngine.Providers;
using System.IO;

namespace FullTextSearch.SearchTools
{
    /// <summary>
    /// This class constructs the actual lucene query from a bunch of 
    /// parameters and returns umbraco examine ISearchResults
    /// </summary>
    public class Search
    {
        /// <summary>
        /// There's a lot of parameters, this is the container object
        /// </summary>
        protected SearchParameters parameters;
        
        const string indexTypePropertyName = "__IndexType";
        
        public Search(SearchParameters parameters)
        {
            this.parameters = parameters;
        }

        /// <summary>
        /// "multi relevance" search does the following... roughly
        /// The index is searched for, in order of decreasing relevance
        /// 1) the exact phrase entered in any of the title properties
        /// 2) any of the terms entered in any of the title properties
        /// 3) a fuzzy match for any of the terms entered in any of the title properties
        /// 4) the exact phrase entered in any of the body properties
        /// 5) any of the terms entered in any of the body properties
        /// 6) a fuzzy match for any of the terms entered in any of the body properties
        /// </summary>
        /// <returns></returns>
        public ISearchResults ResultsMultiRelevance()
        {
            StringBuilder query = new StringBuilder();
            // We formulate the query differently depending on the input.
            if (parameters.SearchTerm.Contains('"'))
            {
                // If the user has enetered double quotes we don't bother 
                // searching for the full string
                query.Append(queryAllPropertiesOr(SearchUtilities.getSearchTermsSplit(parameters.SearchTerm), 1));
            }
            else if (!parameters.SearchTerm.Contains('"') && !parameters.SearchTerm.Contains(' '))
            {
                // if there's no spaces or quotes we don't need to get the quoted term and boost it
                query.Append(queryAllPropertiesOr(SearchUtilities.getSearchTermsSplit(parameters.SearchTerm), 1));
            }
            else
            {
                // otherwise we search first for the entire query in quotes, 
                // then for each term in the query OR'd together.
                query.AppendFormat("({0} OR {1})",
                    queryAllPropertiesOr(SearchUtilities.getSearchTermQuoted(parameters.SearchTerm), 2)
                    , queryAllPropertiesOr(SearchUtilities.getSearchTermsSplit(parameters.SearchTerm), 1)
                );
            }
            return executeSearch(wrapQuery(query));
        }

        /// <summary>
        /// Execute a search requiring all terms in the query to be present, not necessarily in 
        /// the order entered, though that will boost the relevance. Pretty much the same
        /// as SearchMultiRelevance, except terms are AND'd rather than OR'd 
        /// </summary>
        /// <returns></returns>
        public ISearchResults ResultsMultiAnd()
        {
            StringBuilder query = new StringBuilder();

            if (parameters.SearchTerm.Contains('"'))
            {
                // If the user has enetered double quotes we don't bother 
                // searching for the full string
                query.Append(queryAllPropertiesAnd(SearchUtilities.getSearchTermsSplit(parameters.SearchTerm), 1.0));
            }
            else if (!parameters.SearchTerm.Contains('"') && !parameters.SearchTerm.Contains(' '))
            {
                // if there's no spaces or quotes we don't need to get the quoted term and boost it
                query.Append(queryAllPropertiesAnd(SearchUtilities.getSearchTermsSplit(parameters.SearchTerm), 1));
            }
            else
            {
                // otherwise we search first for the entire query in quotes, 
                // then for each term in the query OR'd together.
                query.AppendFormat("{0} OR {1}",
                    queryAllPropertiesAnd(SearchUtilities.getSearchTermQuoted(parameters.SearchTerm), 2)
                    , queryAllPropertiesAnd(SearchUtilities.getSearchTermsSplit(parameters.SearchTerm), 1)
                );
            }
            return executeSearch(wrapQuery(query));
        }
        /// <summary>
        /// Simple search for any term in the query. Make this simpler so it executes faster
        /// </summary>
        /// <returns></returns>
        public ISearchResults ResultsSimpleOr()
        {
            StringBuilder query = new StringBuilder();
            query.Append(queryAllProperties(SearchUtilities.getSearchTermsSplit(parameters.SearchTerm),1.0,"OR",true));
            return executeSearch(wrapQuery(query));
        }
        /// <summary>
        /// Run a quoted query 
        /// </summary>
        /// <returns></returns>
        public ISearchResults ResultsAsEntered()
        {
            StringBuilder query = new StringBuilder();
            query.Append(queryAllPropertiesAnd(SearchUtilities.getSearchTermQuoted(parameters.SearchTerm), 1.0));
            return executeSearch(wrapQuery(query));
        }

        /// <summary>
        /// prepend the query with the necessary queries for index type and root nodes
        /// </summary>
        /// <param name="toWrap">Query to wrap</param>
        /// <returns>query</returns>
        protected StringBuilder wrapQuery(StringBuilder toWrap)
        {
            StringBuilder query = new StringBuilder();
            // first check add the required index type to the query
            StringBuilder indexTypesQuery = queryIndexTypes();
            if (indexTypesQuery != null)
                query.AppendFormat("{0} AND ", indexTypesQuery );
            // now check the node has a parent in the supplied root node list, blank means search all 
            StringBuilder rootNodeQuery = queryRootNodes();
            if (rootNodeQuery != null && rootNodeQuery.Length > 0)
                query.AppendFormat("{0} AND (", rootNodeQuery);
            else
                query.Append("(");

            query.Append(toWrap);
            query.Append(")");
            return query;
        }
        /// <summary>
        /// Get the lucene query to pick out only nodes that have a parent in the list of root nodes
        /// </summary>
        /// <param name="rootNodes"></param>
        /// <returns></returns>
        protected StringBuilder queryRootNodes()
        {
            if (parameters.RootNodes == null || parameters.RootNodes.Count < 1 || parameters.RootNodes.Contains(-1))// -1 is uber root node of site
                return null;
            StringBuilder query = new StringBuilder();

            query.Append("+(");
            int i = 0;
            string pathName = Config.Instance.GetPathPropertyName();
            foreach (int node in parameters.RootNodes)
            {
                if (i++ > 0)
                    query.Append(" OR ");
                query.AppendFormat("{0}:{1}", pathName, node);
            }
            query.Append(")");
            return query;
        }
        protected StringBuilder queryIndexTypes()
        {
            if (parameters.IndexTypes == null || parameters.IndexTypes.Count < 1)
                return null;
            StringBuilder query = new StringBuilder();

            query.Append("+(");
            int i = 0;
            foreach (string indexType in parameters.IndexTypes)
            {
                if (i++ > 0)
                    query.Append(" OR ");
                query.AppendFormat("{0}:\"{1}\"", indexTypePropertyName, QueryParser.Escape(indexType));
            }
            query.Append(")");
            return query;
        }
        /// <summary>
        /// OR's together all the passed search terms into a query
        /// for each property in the properties list
        /// 
        /// </summary>
        /// <param name="searchTerms">A list of fully escaped search terms</param>
        /// <param name="boostAll">all terms are boosted by this amount, multiplied by the amount in the property/boost dictionary</param>
        /// <returns>a query fragment</returns>
        protected StringBuilder queryAllPropertiesOr(ICollection<string> searchTerms, double boostAll)
        {
            if (searchTerms == null || searchTerms.Count < 1)
                return new StringBuilder();

            return queryAllProperties(searchTerms, boostAll, "OR");
        }
        /// <summary>
        /// AND's together all the passed search terms into a query
        /// for each property in the properties list
        /// </summary>
        /// <param name="searchTerms">A list of fully escaped search terms</param>
        /// <param name="boostAll">all terms are boosted by this amount, multiplied by the amount in the property/boost dictionary</param>
        /// <returns>a query fragment</returns>
        protected StringBuilder queryAllPropertiesAnd(ICollection<string> searchTerms, double boostAll)
        {
            if (searchTerms == null || searchTerms.Count < 1)
                return new StringBuilder();

            return queryAllProperties(searchTerms, boostAll, "AND");
        }
        /// <summary>
        /// Called by queryAllPropertiesOr, queryAllPropertiesAnd
        /// Creates a somewhat convuleted lucene query string.
        /// Each search term is applied to each property in the umbracoProperties list, 
        /// boosted by the boost value associated with the property, multiplied by
        /// the boost value passed to the function. 
        /// The global fuzziness level is applied, multiplied by the fuzzyness value 
        /// associated with the relevant property.
        /// Terms are ether OR'd or AND'd (or theoretically anything else
        /// you stick into joinWith'd, though I can't think of much that would 
        /// actually be useful) according to the contents of joinWith
        /// </summary>
        /// <param name="searchTerms">A list of fully escaped search terms</param>
        /// <param name="boostAll">Boost all terms by this amount</param>
        /// <param name="joinWith">Join terms with this string, should be AND/OR</param>
        /// <returns></returns>
        protected StringBuilder queryAllProperties(ICollection<string> searchTerms, double boostAll, string joinWith, bool simplify = false)
        {
            List<StringBuilder> queryBuilder = new List<StringBuilder>();
            foreach (string term in searchTerms)
            {
                StringBuilder termQuery = new StringBuilder();
                foreach (UmbracoProperty property in parameters.SearchProperties)
                {
                    if(simplify)
                        termQuery.Append(querySingleItemSimple(term, property));
                    else
                        termQuery.Append(querySingleItem(term, property, boostAll));
                    
                }
                if (termQuery.Length > 0)
                    queryBuilder.Add(termQuery);
            }
            StringBuilder query = new StringBuilder();
            int count = queryBuilder.Count;
            if (count < 1)
                return query;
            int i = 0;
            for (; ; )
            {
                query.AppendFormat(" ({0}) ", queryBuilder[i]);
                if (++i >= count)
                    break;
                query.AppendFormat("{0} ", joinWith);
            }
            return query;
        }
        protected string querySingleItem(string term, UmbracoProperty property, double boostAll)
        {
            double boost = property.BoostMultiplier * boostAll;
            string boostString = string.Empty;
            if (boost != 1.0)
            {
                boostString = "^" + boost;
            }
            string fuzzyString = string.Empty;
            string wildcardQuery = string.Empty;
            if (!term.Contains('"'))
            {
                // wildcard queries get lower relevance than exact matches, and ignore fuzzieness
                if (property.Wildcard)
                {
                    wildcardQuery = string.Format("{0}:{1}*^{2} ", property.PropertyName, term, boost * 0.5);
                }
                else
                {
                    double fuzzyLocal = property.FuzzyMultiplier;
                    if (fuzzyLocal < 1.0 && fuzzyLocal > 0.0)
                    {
                        fuzzyString = "~" + fuzzyLocal.ToString();
                    }
                }
            }
            return string.Format("{0}:{1}{2}{3} {4}", property.PropertyName, term, fuzzyString, boostString, wildcardQuery);
        }
        protected string querySingleItemSimple(string term, UmbracoProperty property)
        {
            string fuzzyString = string.Empty;
            string wildcard = string.Empty;
            if (!term.Contains('"'))
            {
                if (property.Wildcard)
                {
                    wildcard = "*";
                }
                else
                {
                    double fuzzyLocal = property.FuzzyMultiplier;
                    if (fuzzyLocal < 1.0 && fuzzyLocal > 0.0)
                    {
                        fuzzyString = "~" + fuzzyLocal.ToString();
                    }
                }
            }
            return string.Format("{0}:{1}{2}{3} ", property.PropertyName, term, fuzzyString, wildcard);
        }

        /// <summary>
        /// Call Examine to execute the generated query
        /// </summary> 
        /// <param name="query">query to execute</param>
        /// <returns>ISearchResults object or null</returns>
        protected ISearchResults executeSearch(StringBuilder query)
        {
            var provider = ExamineManager.Instance.SearchProviderCollection[parameters.SearchProvider];
            if (provider == null)
                throw new ArgumentException("Supplied search provider not found. Check FullTextSearch.config");
            var filter = provider.CreateSearchCriteria().RawQuery(query.ToString());
            if (filter != null)
                return provider.Search(filter);
            return null;
        }
    }
}