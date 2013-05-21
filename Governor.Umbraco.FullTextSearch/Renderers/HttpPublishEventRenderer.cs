using System;
using Governor.Umbraco.FullTextSearch.Utilities;

namespace Governor.Umbraco.FullTextSearch.Renderers
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
        protected override bool RetrieveHtml(ref string fullHtml)
        {
            try
            {
                return Library.HttpRenderNode(NodeId, Library.GetQueryStringCollection(), out fullHtml);
            }
            catch (Exception ex)
            {
                umbraco.BusinessLogic.Log.AddSynced(umbraco.BusinessLogic.LogTypes.Error, 0, NodeId, "Error rendering node using Http Renderer: (" + ex + ")");
                if (Library.IsCritical(ex))
                    throw;
                fullHtml = string.Empty;
                return false;
            }
        }
    }
}