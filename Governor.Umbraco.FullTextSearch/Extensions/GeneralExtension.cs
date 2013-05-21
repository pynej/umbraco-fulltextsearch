using System;

namespace Governor.Umbraco.FullTextSearch.Extensions
{
    /// <summary>
    /// Contains a few helper methods we call from FullTextSearch.xslt
    /// </summary>
    public class GeneralExtension
    {
        /// <summary>
        /// All this does is call the umbraco library function GetDictionaryItem
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string DictionaryHelper(string key)
        {
            return !string.IsNullOrEmpty(key) ? umbraco.library.GetDictionaryItem("FullTextSearch__" + key) : string.Empty;
        }

        /// <summary>
        /// This is budget. But params are not supported by MS XSLT, so we create a real method and a bunch of overloads. 
        /// As far as I'm aware, no, there isn't any way of doing this that is less painful and ugly
        /// than, say, root canal without anasthetic performed by the elephant man.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        static string StringFormatInternal(string format, params string[] args)
        {
            string result;
            try
            {
                result = string.Format(format, args);
            }
            catch (FormatException)
            {
                result = "Format string '" + format + "' incorrectly formated";
            }
            return result;
        }
        public static string StringFormat(string format, string arg1)
        {
            return StringFormatInternal(format, arg1);
        }
        public static string StringFormat(string format, string arg1, string arg2)
        {
            return StringFormatInternal(format, arg1, arg2);
        }
        public static string StringFormat(string format, string arg1, string arg2, string arg3)
        {
            return StringFormatInternal(format, arg1, arg2, arg3);
        }
        public static string StringFormat(string format, string arg1, string arg2, string arg3, string arg4)
        {
            return StringFormatInternal(format, arg1, arg2, arg3, arg4);
        }
        public static string StringFormat(string format, string arg1, string arg2, string arg3, string arg4, string arg5)
        {
            return StringFormatInternal(format, arg1, arg2, arg3, arg4, arg5);
        }
        public static string StringFormat(string format, string arg1, string arg2, string arg3, string arg4, string arg5, string arg6)
        {
            return StringFormatInternal(format, arg1, arg2, arg3, arg4, arg5, arg6);
        }
        // If you need more than 7 argmumennts you're SOL. Sorry. 
        public static string StringFormat(string format, string arg1, string arg2, string arg3, string arg4, string arg5, string arg6, string arg7)
        {
            return StringFormatInternal(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }
        /// <summary>
        /// Check whether the current page is being rendered by the indexer
        /// </summary>
        /// <returns>true if being indexed</returns>
        public static bool IsIndexingActive()
        {
            var searchActiveStringName = Config.Instance.GetByKey("SearchActiveStringName");
            if (!string.IsNullOrEmpty(searchActiveStringName))
            {
                if (!string.IsNullOrEmpty(umbraco.library.RequestQueryString(searchActiveStringName)))
                    return true;
                if (!string.IsNullOrEmpty(umbraco.library.RequestCookies(searchActiveStringName)))
                    return true;
            }
            return false;
        }
    }
}