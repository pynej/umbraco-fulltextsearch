using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.IO;
using System.Collections.Specialized;
using umbraco.NodeFactory;
using FullTextSearch.Utilities;

namespace FullTextSearch.Renderers
{
    /// <summary>
    /// This needs to be used when the umbraco core is active. It uses HttpWebReqeust and
    /// the umbraco node factory to render HTML content for nodes
    /// </summary>
    public class HttpPublishEventRenderer : ProgramaticRenderer
    {
        /// <summary>
        /// Retrieves HTML for the current node using an HttpWebRequest
        /// </summary>
        /// <param name="fullHtml">string to fill with HTML</param>
        /// <returns>success/failure</returns>
        protected override bool retrieveHTML(ref string fullHtml)
        {
            try
            {
                return Library.HttpRenderNode(nodeId, Library.getQueryStringCollection(), out fullHtml);
            }
            catch (Exception ex)
            {
                umbraco.BusinessLogic.Log.AddSynced(umbraco.BusinessLogic.LogTypes.Error, 0, nodeId, "Error rendering node using Http Renderer: (" + ex.ToString() + ")");
                if (Library.IsCritical(ex))
                    throw;
                fullHtml = string.Empty;
                return false;
            }
        }
    }
}