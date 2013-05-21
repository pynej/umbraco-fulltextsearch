using Examine;
using System.Text.RegularExpressions;
using Governor.Umbraco.FullTextSearch.SearchTools;

namespace Governor.Umbraco.FullTextSearch.HighlightTools
{
    /// <summary>
    /// Retieve the summary info (title and summary text) for each search result. 
    /// </summary>
    public class Plain : Summariser
    {
        protected string noSummary;
        protected string noTitle;

        public Plain(SummariserParameters parameters) : base(parameters) {
            noSummary = umbraco.library.GetDictionaryItem("FullTextSearch__NoSummary");
            if (string.IsNullOrEmpty(noSummary))
                noSummary = "Read More";
            noTitle = umbraco.library.GetDictionaryItem("FullTextSearch__NoTitle");
            if (string.IsNullOrEmpty(noTitle))
                noTitle = "Unknown Page";
        }
        
        public override void GetTitle(SearchResult result, out string title)
        {
            foreach (UmbracoProperty prop in parameters.TitleLinkProperties)
            {
                if (result.Fields.ContainsKey(prop.PropertyName))
                {
                    title = result.Fields[prop.PropertyName];
                    if (!string.IsNullOrEmpty(title))
                        return;
                }
            }
            title = noTitle;
        }

        public override void GetSummary(SearchResult result, out string summary)
        {
            foreach (UmbracoProperty prop in parameters.BodySummaryProperties)
            {
                if (result.Fields.ContainsKey(prop.PropertyName))
                {
                    summary = getSummaryText(result, prop.PropertyName);
                    if (!string.IsNullOrEmpty(summary))
                        return;
                }
            }
            summary = noSummary;
        }

        protected string getSummaryText(SearchResult result, string propertyName)
        {
            string summary;
            if (result.Fields[propertyName].Length > parameters.SummaryLength)
            {
                summary = result.Fields[propertyName].Substring(0, parameters.SummaryLength);
                summary = Regex.Replace(summary, @"\S*$", string.Empty, RegexOptions.Compiled);
            }
            else
            {
                summary = result.Fields[propertyName];
            }
            return summary;
        }
    }
}