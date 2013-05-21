using Examine;

namespace Governor.Umbraco.FullTextSearch.HighlightTools
{
    public abstract class Summariser 
    {
        protected SummariserParameters parameters;

        public Summariser(SummariserParameters parameters)
        {
            this.parameters = parameters;
        }
        
        public abstract void GetTitle(SearchResult result, out string title);

        public abstract void GetSummary(SearchResult result, out string summary);
    }
}
