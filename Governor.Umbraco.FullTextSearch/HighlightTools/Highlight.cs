using System;
using System.Collections.Generic;
using System.Linq;
using Examine;
using Lucene.Net.Analysis;
using Lucene.Net.Highlight;
using Lucene.Net.Search;
using Lucene.Net.Index;
using Examine.Providers;
using Examine.LuceneEngine.Providers;
using Lucene.Net.QueryParsers;
using System.IO;
using System.Text;
using FullTextSearch.SearchTools;

namespace FullTextSearch.HighlightTools
{
    /// <summary>
    /// Retrieve summary (the title link and the bit of context that goes under it) for search results
    /// using Highligher.net (part of lucene) to do context highlighting.
    /// The class is instantiated once for every result set. 
    /// </summary>
    public class Highlight : Summariser
    {
        /// <summary>
        /// The highlighter will need to access lucene directly. 
        /// These objects cache some state
        /// </summary>
        Analyzer analyzer;
        Formatter formatter;
        IndexSearcher searcher;
        IndexReader reader;

        /// <summary>
        /// This speeds up higlighting, we create the highligter for each field once and cache it for
        /// the whole results set.
        /// </summary>
        protected Dictionary<string, Highlighter> highlighterCache = new Dictionary<string, Highlighter>();

        private Plain plainSummariser;

        public Highlight(SummariserParameters parameters)
            : base(parameters)
        {
            BaseSearchProvider searchProvider = ExamineManager.Instance.SearchProviderCollection[parameters.SearchProvider];
            if (searchProvider != null && searchProvider is LuceneSearcher)
            {
                this.searcher = (searchProvider as LuceneSearcher).GetSearcher();
                this.analyzer = (searchProvider as LuceneSearcher).IndexingAnalyzer;
                this.reader = this.searcher.GetIndexReader();
            }
            else
            {
                throw new ArgumentException("Supplied search provider not found, or is not a valid LuceneSearcher");
            }
            this.formatter = new SimpleHTMLFormatter(parameters.HighlightPreTag, parameters.HighlightPostTag);
            // fall back to plain summary if no highlight found
            plainSummariser = new Plain(parameters);
        }

        /// <summary>
        /// Get the summary text for a given search result
        /// </summary>
        /// <param name="result"></param>
        /// <param name="summary"></param>
        public override void GetSummary(SearchResult result, out string summary)
        {
            foreach (UmbracoProperty prop in parameters.BodySummaryProperties)
            {
                if (result.Fields.ContainsKey(prop.PropertyName))
                {
                    if (luceneHighlightField(result, prop, out summary))
                    {
                        return;
                    }
                }
            }
            plainSummariser.GetSummary(result, out summary);
        }
        /// <summary>
        /// Retrieve highlighted title
        /// </summary>
        /// <param name="result"></param>
        /// <param name="title"></param>
        public override void GetTitle(SearchResult result, out string title)
        {
            foreach (UmbracoProperty prop in parameters.TitleLinkProperties)
            {
                if (result.Fields.ContainsKey(prop.PropertyName))
                {
                    if (luceneHighlightField(result, prop, out title))
                    {
                        return;
                    }
                }
            }
            plainSummariser.GetTitle(result, out title);
            return;
        }
        /// <summary>
        /// highlight the search term in the supplied result
        /// </summary>
        /// <param name="result"></param>
        /// <param name="umbracoProperty"></param>
        /// <param name="summary"></param>
        /// <returns></returns>
        protected bool luceneHighlightField(SearchResult result, UmbracoProperty umbracoProperty, out string summary)
        {
            summary = string.Empty;
            string fieldName = umbracoProperty.PropertyName;
            if (!string.IsNullOrEmpty(result.Fields[fieldName]))
            {

                Highlighter highlighter;
                if (highlighterCache.ContainsKey(fieldName))
                {
                    highlighter = highlighterCache[fieldName];
                }
                else
                {
                    List<string> searchTerms = SearchUtilities.getSearchTermsSplit(parameters.SearchTerm);
                    string luceneQuery = queryHighlight(umbracoProperty, searchTerms);
                    QueryParser parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_29, fieldName, analyzer);
                    // This is needed to make wildcards highlight correctly
                    if (umbracoProperty.Wildcard)
                        parser.SetMultiTermRewriteMethod(MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE);
                    Query query = parser.Parse(luceneQuery);
                    query = query.Rewrite(reader);
                    QueryScorer scorer = new QueryScorer(query);
                    highlighter = new Highlighter(formatter, scorer);
                    highlighter.SetTextFragmenter(new SimpleFragmenter(parameters.SummaryLength));
                    highlighterCache.Add(fieldName, highlighter);
                }
                using (StringReader sr = new StringReader(result.Fields[fieldName]))
                {
                    TokenStream tokenstream = analyzer.TokenStream(fieldName, sr);
                    summary = highlighter.GetBestFragment(tokenstream, result.Fields[fieldName]);
                    if (!string.IsNullOrEmpty(summary))
                    {
                        return true;
                    }
                }

            }
            return false;
        }
        /// <summary>
        /// Construct the lucene query to feed to the highlighter
        /// </summary>
        /// <param name="umbracoProperty"></param>
        /// <param name="searchTerms"></param>
        /// <returns></returns>
        protected string queryHighlight(UmbracoProperty umbracoProperty, List<string> searchTerms)
        {
            StringBuilder query = new StringBuilder();
            foreach (string term in searchTerms)
            {
                string fuzzyString = string.Empty;
                if (!term.Contains('"'))
                {
                    // wildcard queries get lower relevance than exact matches, and ignore fuzzieness
                    if (umbracoProperty.Wildcard)
                    {
                        query.AppendFormat("{0}*^{1} ", term, 0.5);
                    }
                    else
                    {
                        double fuzzyLocal = umbracoProperty.FuzzyMultiplier;
                        if (fuzzyLocal < 1.0 && fuzzyLocal > 0.0)
                        {
                            fuzzyString = "~" + fuzzyLocal.ToString();
                        }
                    }
                }
                query.AppendFormat("{0}{1} ", term, fuzzyString);
            }
            return query.ToString();
        }
    }
}