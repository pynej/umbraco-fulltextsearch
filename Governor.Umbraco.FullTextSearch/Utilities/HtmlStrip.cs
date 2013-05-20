using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using HtmlAgilityPack;
using System.Text;
using System.Text.RegularExpressions;

namespace FullTextSearch.Utilities
{
    /// <summary>
    /// Uses HTML agility pack to do custom tag stripping
    /// </summary>
    class HtmlStrip
    {
        private string[] tagsToStripContentsOf;
        private string[] idsToStripContentsOf;

        private bool continueOnAgilityException;
        /// <summary>
        /// Default constructor, set up sane values
        /// </summary>
        public HtmlStrip()
        {
            this.tagsToStripContentsOf = new string[] { "head", "script" };
            this.idsToStripContentsOf = new string[] { };
            this.continueOnAgilityException = true;
        }
        /// <summary>
        /// Overloaded Constructor, allows specifying which tags get stripped
        /// </summary>
        /// <param name="tagsToStripContentsOf">an array of tags the FULL contents of which will be removed, not just the tags themselves</param>
        /// <param name="idsToStripContentsOf">an array of HTML element IDs to fully remove</param>
        public HtmlStrip(string[] tagsToStripContentsOf, string[] idsToStripContentsOf, bool continueOnAgilityException = true)
        {
            this.tagsToStripContentsOf = tagsToStripContentsOf;
            this.idsToStripContentsOf = idsToStripContentsOf;
            this.continueOnAgilityException = continueOnAgilityException;
        }
        /// <summary>
        /// Strips HTML according to the parameters set in the constructor
        /// </summary>
        /// <param name="fullHTML">HTML to strip</param>
        /// <returns>Text</returns>
        public string TextFromHTML(ref string fullHTML)
        {
            if (fullHTML.Length < 1)
            {
                return "";
            }
            string fullText = AgilityTagStrip(fullHTML);
            // the above leaves some residual tags. Search me why, probably the HTML input isn't perfectly
            // formed and the parser is choking on it. Help it along
            fullText = Regex.Replace(fullText, "<[^>]*>", String.Empty);
            // Decode any HTML entities
            fullText = HttpUtility.HtmlDecode(fullText);
            // replace multiple spaces with single spaces. 
            fullText = Regex.Replace(fullText, @"(\s)(\s+)","$1",RegexOptions.Singleline);
            return fullText;
        }
        /// <summary>
        /// Run a tag stripper based on the HTML Agility pack
        /// </summary>
        /// <param name="fullHTML">Html to strip</param>
        /// <returns>Text. Hopefully.</returns>
        private string AgilityTagStrip(string fullHTML)
        {
            HtmlDocument doc = new HtmlDocument();
            try
            {
                //Does this break stuff?
                doc.OptionReadEncoding = false;
                doc.LoadHtml(fullHTML);
            }
            catch (Exception ex)
            {
                if(System.Web.HttpContext.Current != null)
                    System.Web.HttpContext.Current.Trace.Warn("Search", "There was an exception cleaning HTML: " + ex.ToString());
                umbraco.BusinessLogic.Log.AddSynced(umbraco.BusinessLogic.LogTypes.Error, 0, 0, "HTML Strip exception: " + ex.ToString());
                // swallow the exception and run the regex based tag stripper on it anyway. Won't be perfect but better than nothing. 
                if(continueOnAgilityException)
                    return fullHTML;
                return string.Empty;
            }
            foreach (string tag in tagsToStripContentsOf)
            {
                HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//" + tag);
                if (nodes != null)
                {
                    foreach (HtmlNode h in nodes)
                    {
                        h.ParentNode.RemoveChild(h, false);
                    }
                }
            }
            foreach (string id in idsToStripContentsOf)
            {
                var h = doc.GetElementbyId(id);
                if (h != null)
                {
                    h.ParentNode.RemoveChild(h, false);
                }
            }
            StringBuilder fullTextBuilder = new StringBuilder();
            TagStripHTML(doc.DocumentNode, fullTextBuilder);
            return fullTextBuilder.ToString();
        }
        private void TagStripHTML(HtmlNode root, StringBuilder fullTextBuilder)
        {
            if (root.HasChildNodes)
            {
                foreach (var node in root.ChildNodes)
                {
                    TagStripHTML(node, fullTextBuilder);
                }
            }
            else if (root.NodeType == HtmlNodeType.Text)
            {
                fullTextBuilder.Append(root.InnerText);
                fullTextBuilder.Append(" ");
            }
        }
    }
}