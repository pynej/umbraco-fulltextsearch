using System;
using System.Collections.Generic;
using System.Web;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;

namespace Governor.Umbraco.FullTextSearch
{
    /// <summary>
    /// Singleton configuration object for FullTextSearch
    /// </summary>
    public sealed class Config
    {
        // configuration cache
        private XmlDocument configDocument = null;
        private XmlDocument config
        {
            get
            {
                checkReadConfig();
                return configDocument;
            }
        }

        private string filePath;
        private Dictionary<string, string> singleCache;
        private DateTime lastModified;
        private Config()
        {
            singleCache = new Dictionary<string, string>();
        }
        /// <summary>
        /// singleton
        /// </summary>
        public static Config Instance
        {
            get { return Nested.instance; }
        }

        private class Nested
        {
            static Nested()
            {
            }
            internal static readonly Config instance = new Config();
        }
        /// <summary>
        /// Gets the InnerText of a configuration key, to just have a simple string value, but use as you like 
        /// </summary>
        /// <param name="key">key name</param>
        /// <returns>value</returns>
        public string GetByKey(string key)
        {
            checkReadConfig();
            if (singleCache.ContainsKey(key))
                return singleCache[key];
            key = Regex.Replace(key, @"[^A-Za-z0-9_\-]", string.Empty);
            XmlNode node = config.SelectSingleNode("/FullTextSearch/" + key);
            if (node != null)
            {
                singleCache.Add(key, node.InnerText);
                return node.InnerText;
            }
            singleCache.Add(key, string.Empty);
            return string.Empty;
        }
        /// <summary>
        /// Get a multi value key
        /// </summary>
        /// <param name="key">name of the outermost tag</param>
        /// <returns>list of strings</returns>
        public List<string> GetMultiByKey(string key)
        {
            List<string> values = new List<string>();
            key = Regex.Replace(key, @"[^A-Za-z0-9_\-]", string.Empty);
            foreach (XmlNode node in config.SelectNodes("/FullTextSearch/"+key+"/add"))
            {
                string name = node.Attributes["name"].Value.ToString();
                if (!string.IsNullOrEmpty(name))
                    values.Add(name);
            }
            return values;
        }
        /// <summary>
        /// Check whether the a given key is set to boolean True/False
        /// </summary>
        /// <returns>true on enabled</returns>
        public bool GetBooleanByKey(string key)
        {
            string s = GetByKey(key);
            if (!string.IsNullOrWhiteSpace(s) && (s == "1" || s.ToLower() == "true"))
                return true;
            return false;
        }
        public double? GetDoubleByKey(string key)
        {
            double d;
            string s = GetByKey(key);
            if (string.IsNullOrEmpty(s) || !double.TryParse(s, out d))
            {
                return null;
            }
            return d;
        }
        /// <summary>
        /// return the name of the lucene field we fill with the full text
        /// </summary>
        public string GetLuceneFTField()
        {
            string value = GetByKey("LuceneFTField");
            if (string.IsNullOrEmpty(value))
                return "FullTextSearch";
            return value;
        }
        public double GetSearchFuzzieness()
        {
            double? fuzz = GetDoubleByKey("SearchFuzzieness");
            if (fuzz == null)
                fuzz = 1.0;
            return (double)fuzz;
        }
        public double GetSearchTitleBoost()
        {
            double? boost = GetDoubleByKey("SearchTitleBoost");
            if (boost == null)
                boost = 10.0;
            return (double)boost;
        }
        /// <summary>
        /// Needs to be kept track of, but not really changed.
        /// </summary>
        public string GetPathPropertyName()
        {
            return "FullTextPath";
        }
        /// <summary>
        /// Check if the config file has been loaded, if not read it into memory
        /// </summary>
        /// <returns>bool indicating sucessfull/unsucessfull load</returns>
        private bool checkReadConfig()
        {
            if (configDocument == null || string.IsNullOrEmpty(filePath) || File.GetLastWriteTime(filePath).CompareTo(lastModified) > 0)
            {
                configDocument = new XmlDocument();
                if(HttpContext.Current != null)
                    filePath = HttpContext.Current.Server.MapPath("/config/FullTextSearch.config");
                else
                    filePath = System.Web.Hosting.HostingEnvironment.MapPath("/config/FullTextSearch.config");
                try
                {
                    configDocument.Load(filePath);
                    lastModified = File.GetLastWriteTime(filePath);
                    singleCache = new Dictionary<string, string>();
                    return true;
                }
                catch (IOException ex)
                {
                    configDocument = null;
                    filePath = string.Empty;
                    umbraco.BusinessLogic.Log.AddSynced(umbraco.BusinessLogic.LogTypes.Error, 0, 0, "Error loading configuration in FullTextSearch: (" + ex.ToString() + ")");
                }
                catch (XmlException ex)
                {
                    configDocument = null;
                    filePath = string.Empty;
                    umbraco.BusinessLogic.Log.AddSynced(umbraco.BusinessLogic.LogTypes.Error, 0, 0, "Error parsing configuration in FullTextSearch: (" + ex.ToString() + ")");
                }
                return false;
            }
            return true;
        }
    }
}