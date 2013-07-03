using System;
using System.Collections.Generic;
using System.Linq;
using Examine;
using Governor.Umbraco.FullTextSearch.SearchTools;
using Lucene.Net.Analysis;
using Lucene.Net.Highlight;
using Lucene.Net.Search;
using Lucene.Net.Index;
using Examine.LuceneEngine.Providers;
using Lucene.Net.QueryParsers;
using System.IO;
using System.Text;

namespace Governor.Umbraco.FullTextSearch.HighlightTools
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
        readonly Analyzer _analyzer;

        readonly Formatter _formatter;
        readonly IndexSearcher _searcher;
        readonly IndexReader _reader;

        /// <summary>
        /// This speeds up higlighting, we create the highligter for each field once and cache it for
        /// the whole results set.
        /// </summary>
        protected Dictionary<string, Highlighter> HighlighterCache = new Dictionary<string, Highlighter>();

        private readonly Plain _plainSummariser;

        public Highlight(SummariserParameters parameters)
            : base(parameters)
        {
            var searchProvider = ExamineManager.Instance.SearchProviderCollection[parameters.SearchProvider];
            if (searchProvider is LuceneSearcher)
            {
                _searcher = (searchProvider as LuceneSearcher).GetSearcher();
                _analyzer = (searchProvider as LuceneSearcher).IndexingAnalyzer;
                _reader = _searcher.GetIndexReader();
            }
            else
            {
                throw new ArgumentException("Supplied search provider not found, or is not a valid LuceneSearcher");
            }
            _formatter = new SimpleHTMLFormatter(parameters.HighlightPreTag, parameters.HighlightPostTag);
            // fall back to plain summary if no highlight found
            _plainSummariser = new Plain(parameters);
        }

        /// <summary>
        /// Get the summary text for a given search result
        /// </summary>
        /// <param name="result"></param>
        /// <param name="summary"></param>
        public override void GetSummary(SearchResult result, out string summary)
        {
            foreach (var prop in Parameters.BodySummaryProperties.Where(prop => result.Fields.ContainsKey(prop.PropertyName)))
            {
                if (LuceneHighlightField(result, prop, out summary))
                {
                    return;
                }
            }
            _plainSummariser.GetSummary(result, out summary);
        }

        /// <summary>
        /// Retrieve highlighted title
        /// </summary>
        /// <param name="result"></param>
        /// <param name="title"></param>
        public override void GetTitle(SearchResult result, out string title)
        {
            foreach (var prop in Parameters.TitleLinkProperties.Where(prop => result.Fields.ContainsKey(prop.PropertyName)))
            {
                if (LuceneHighlightField(result, prop, out title))
                {
                    return;
                }
            }
            _plainSummariser.GetTitle(result, out title);
        }

        /// <summary>
        /// highlight the search term in the supplied result
        /// </summary>
        /// <param name="result"></param>
        /// <param name="umbracoProperty"></param>
        /// <param name="summary"></param>
        /// <returns></returns>
        protected bool LuceneHighlightField(SearchResult result, UmbracoProperty umbracoProperty, out string summary)
        {
            summary = string.Empty;
            var fieldName = umbracoProperty.PropertyName;
            if (!string.IsNullOrEmpty(result.Fields[fieldName]))
            {
                Highlighter highlighter;
                if (HighlighterCache.ContainsKey(fieldName))
                {
                    highlighter = HighlighterCache[fieldName];
                }
                else
                {
                    var searchTerms = SearchUtilities.GetSearchTermsSplit(Parameters.SearchTerm);
                    var luceneQuery = QueryHighlight(umbracoProperty, searchTerms);
                    var parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_29, fieldName, _analyzer);
                    // This is needed to make wildcards highlight correctly
                    if (umbracoProperty.Wildcard)
                        parser.SetMultiTermRewriteMethod(MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE);
                    var query = parser.Parse(luceneQuery);
                    query = query.Rewrite(_reader);
                    var scorer = new QueryScorer(query);
                    highlighter = new Highlighter(_formatter, scorer);
                    highlighter.SetTextFragmenter(new SimpleFragmenter(Parameters.SummaryLength));
                    HighlighterCache.Add(fieldName, highlighter);
                }
                using (var sr = new StringReader(result.Fields[fieldName]))
                {
                    var tokenstream = _analyzer.TokenStream(fieldName, sr);
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
        protected string QueryHighlight(UmbracoProperty umbracoProperty, List<string> searchTerms)
        {
            var query = new StringBuilder();
            foreach (var term in searchTerms)
            {
                var fuzzyString = string.Empty;
                if (!term.Contains('"'))
                {
                    // wildcard queries get lower relevance than exact matches, and ignore fuzzieness
                    if (umbracoProperty.Wildcard)
                    {
                        query.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0}*^{1} ", term, 0.5);
                    }
                    else
                    {
                        var fuzzyLocal = umbracoProperty.FuzzyMultiplier;
                        if (fuzzyLocal < 1.0 && fuzzyLocal > 0.0)
                        {
                            fuzzyString = "~" + fuzzyLocal;
                        }
                    }
                }
                query.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0}{1} ", term, fuzzyString);
            }
            return query.ToString();
        }
    }
}