using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI;
using umbraco;
using System.Text;
using System.IO;
using System.Collections;
using umbraco.cms.businesslogic.web;
using umbraco.NodeFactory;
using System.Net;

namespace Governor.Umbraco.FullTextSearch.Utilities
{
    public class Library
    {
        /// <summary>
        /// Use Http Web Requests to render a node to a string
        /// </summary>
        /// <remarks>
        /// this calls umbraco's default.aspx rather than attempt to figure out
        /// the standard umbraco "nice" url. Simply because we can't get the
        /// nice URL without a valid Http Context in the first place. Also, 
        /// the query string we pass to the client page in RenderTemplate
        /// is replaced with a cookie here, simply because adding items
        /// to the query string for default.aspx doesn't actually make
        /// them visible to the page being rendererd. Grrrrrrr. 
        /// </remarks>
        /// <param name="pageId"></param>
        /// <param name="cookieDictionary"></param>
        /// <param name="fullHtml"></param>
        /// <returns></returns>
        public static bool HttpRenderNode(int pageId, Dictionary<string, string> cookieDictionary, out string fullHtml)
        {
            var config = Config.Instance;
            var defaultUrl = config.GetByKey("HttpUrl");
            if (string.IsNullOrEmpty(defaultUrl))
                throw new ArgumentException("RenderingURL must be set in FullTextSearch config file to use Http node rendering");
            var firstSeparator = "?";
            if(defaultUrl.Contains('?'))
                firstSeparator = "&";
            var url = string.Format("{0}{1}umbpageid={2}", defaultUrl, firstSeparator, pageId);
            //get timeout
            int timeout;
            var httpTimeout = config.GetByKey("HttpTimeout");
            if (string.IsNullOrEmpty(httpTimeout) || !Int32.TryParse(httpTimeout, out timeout))
            {
                timeout = 120;
            }
            timeout *= 1000;
            //get host header
            var host = config.GetByKey("HttpHost");
            // setup request
            try
            {
                var webRequest = (HttpWebRequest)WebRequest.Create(url);

                if (!string.IsNullOrEmpty(host))
                    webRequest.Host = host;

                webRequest.Timeout = timeout;
                webRequest.UserAgent = "FullTextIndexer";
                if (cookieDictionary != null && cookieDictionary.Count > 0)
                {
                    var container = new CookieContainer();
                    var domain = webRequest.Address.DnsSafeHost;
                    foreach (var cookie in cookieDictionary)
                    {
                        container.Add(new Cookie(cookie.Key, cookie.Value,"/",domain));
                    }
                    webRequest.CookieContainer = container;
                }
                var webResponse = (HttpWebResponse)webRequest.GetResponse();
                using (var sr = new StreamReader(webResponse.GetResponseStream()))
                {
                    fullHtml = sr.ReadToEnd();
                }
                return true;
            }
            catch(WebException ex)
            {
                umbraco.BusinessLogic.Log.AddSynced(umbraco.BusinessLogic.LogTypes.Error, 0, pageId, "HTTP error in FullTextSearch retrieval: (" + ex + ")");
                fullHtml = string.Empty;
            }
            return false;
        }

        /// <summary>
        /// RenderTemplate, shamelessly lifted from the umbraco library and modified
        /// to avoid passing the contents of request.form to the child page, and instead pass
        /// some parameters of our choice
        /// </summary>
        /// <param name="pageId">Page ID</param>
        /// <param name="templateId"></param>
        /// <param name="queryStringCollection">query strings to pass to child page</param>
        /// <returns>The page output HTML</returns>
        public static string RenderTemplate(int pageId, int templateId, Dictionary<string,string> queryStringCollection)
        {

            if (UmbracoSettings.UseAspNetMasterPages)
            {
                var items = GetCurrentContextItems();

                if (!umbraco.presentation.UmbracoContext.Current.LiveEditingContext.Enabled)
                {
                    var context = HttpContext.Current;
                    foreach (var key in context.Request.QueryString.Cast<object>().Where(key => ! queryStringCollection.ContainsKey(key.ToString())))
                    {
                        queryStringCollection.Add(key.ToString(),context.Request.QueryString[key.ToString()]);
                    }
                    var queryString = QueryStringBuilder(queryStringCollection);
                    using (var sw = new StringWriter())
                    {
                        context.Server.Execute(
                            string.Format("/default.aspx?umbpageid={0}&alttemplate={1}&{2}",
                            pageId, new template(templateId).TemplateAlias, queryString), sw, false);
                        // update the local page items again
                        UpdateLocalContextItems(items, context);
                        return sw.ToString();
                    }
                }
                else
                {
                    return "RenderTemplate not supported in Canvas";
                }
            }
            else
            {
                var p = new page(((System.Xml.IHasXmlNode)library.GetXmlNodeById(pageId.ToString(CultureInfo.InvariantCulture)).Current).GetNode());
                p.RenderPage(templateId);
                var c = p.PageContentControl;
                using (var sw = new StringWriter())
                {
                    var hw = new HtmlTextWriter(sw);
                    c.RenderControl(hw);

                    return sw.ToString();
                }
            }

        }
        /// <summary>
        /// From umbraco library
        /// </summary>
        /// <param name="items"></param>
        /// <param name="context"></param>
        private static void UpdateLocalContextItems(IDictionary items, HttpContext context)
        {
            context.Items.Clear();
            var ide = items.GetEnumerator();
            while (ide.MoveNext())
            {
                context.Items.Add(ide.Key, ide.Value);
            }
        }
        /// <summary>
        /// From umbraco library
        /// </summary>
        /// <returns></returns>
        private static Dictionary<object, object> GetCurrentContextItems()
        {
            var items = HttpContext.Current.Items;
            var currentItems = new Dictionary<object, object>();
            var ide = items.GetEnumerator();
            while (ide.MoveNext())
            {
                currentItems.Add(ide.Key, ide.Value);
            }
            return currentItems;
        }
        /// <summary>
        /// build a query string from a dictionary
        /// </summary>
        /// <param name="queryStringCollection"></param>
        /// <returns></returns>
        public static string QueryStringBuilder(Dictionary<string,string> queryStringCollection)
        {
            var queryString = new StringBuilder();
            const string oneQsParam = "&{0}={1}";
            foreach (var item in queryStringCollection.Where(item => ! item.Key.ToLower().Equals("umbpageid")))
            {
                queryString.Append(string.Format(oneQsParam, item.Key, item.Value));
            }
            // remove leading &
            if(queryString.Length > 0)
                queryString.Remove(0, 1);
            return queryString.ToString();
        }

        /// <summary>
        /// Given a string (from config) denoting number of minutes, set HTTP timout
        /// to the proper number of sectonds
        /// </summary>
        public static void SetTimeout(string secondsString)
        {
            int secondsInt;
            if (string.IsNullOrEmpty(secondsString) || !Int32.TryParse(secondsString, out secondsInt))
            {
                return;
            }
            if (HttpContext.Current != null)
                HttpContext.Current.Server.ScriptTimeout = secondsInt;
        }
        /// <summary>
        /// We need to be able to check whether a given Node object, or Document object, dependant on 
        /// where this is called from, has a property listed in DisableSearchPropertyNames in the config
        /// file set to true
        /// </summary>
        /// <param name="documentObject">Must be type Document or type Node</param>
        /// <returns>bool indicating whether this property exists and is enabled</returns>
        public static bool IsSearchDisabledByProperty(object documentObject)
        {
            if (documentObject == null)
                return false;
            if (!(documentObject is Document || documentObject is Node))
                throw new ArgumentException("documentObject must be umbraco.cms.businesslogic.web.Document or umbraco.NodeFactory.Node");
            var config = Config.Instance;
            var searchHides = config.GetMultiByKey("DisableSearchPropertyNames");
            if (searchHides != null)
            {
                foreach (var searchHide in searchHides)
                {
                    var val = "0";
                    if (documentObject is Document)
                    {
                        var d = documentObject as Document;
                        var property = d.getProperty(searchHide);
                        if (property != null)
                        {
                            val = property.Value.ToString().ToLower();
                        }
                    }
                    else
                    {
                        var n = documentObject as Node;
                        var property = n.GetProperty(searchHide);
                        if (property != null)
                        {
                            val = property.Value.ToLower();
                        }
                    }
                    // true/false property set to false in umbraco back office returns integer zero
                    if (! string.IsNullOrEmpty(val) && val != "0")
                        return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Gets the name of the query string variable to pass to rendered pages from the config, and sticks it
        /// into a dictionary. Hardly needs it's own method, but it's used in a few places so...
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, string> GetQueryStringCollection()
        {
            var queryString = new Dictionary<string, string>();
            var config = Config.Instance;
            var getStringName = config.GetByKey("SearchActiveStringName");
            if (!string.IsNullOrWhiteSpace(getStringName))
            {
                queryString.Add(getStringName, "1");
            }
            return queryString;
        }
        /// <summary>
        /// There are some places in the code where we swallow more exceptions
        /// than is perhaps generally good practice. This is esentially because
        /// the indexer has a nasty habbit of dumping out and requiring an app 
        /// pool recycle if we don't do this... 
        /// This lets us rethrow the most critical exception types
        /// </summary>
        /// <param name="ex">The Exception</param>
        /// <returns>true on critical exception</returns>
        public static bool IsCritical(Exception ex)
        {
            if (ex is OutOfMemoryException) return true;
            if (ex is AppDomainUnloadedException) return true;
            if (ex is BadImageFormatException) return true;
            if (ex is CannotUnloadAppDomainException) return true;
            if (ex is InvalidProgramException) return true;
            if (ex is StackOverflowException) return true;
            if (ex is System.Threading.ThreadAbortException)
                return true;
            return false;
        }
    }
}