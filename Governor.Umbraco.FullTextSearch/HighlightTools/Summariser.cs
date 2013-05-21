using Examine;

namespace Governor.Umbraco.FullTextSearch.HighlightTools
{
    public abstract class Summariser 
    {
        protected SummariserParameters Parameters;

        protected Summariser(SummariserParameters parameters)
        {
            Parameters = parameters;
        }
        
        public abstract void GetTitle(SearchResult result, out string title);

        public abstract void GetSummary(SearchResult result, out string summary);
    }
}
