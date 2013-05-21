using System;
using Governor.Umbraco.FullTextSearch.Utilities;
using umbraco.cms.businesslogic.web;

namespace Governor.Umbraco.FullTextSearch.Renderers
{
    /// <summary>
    /// This can be used when the umbraco core is not active. 
    /// it uses the Document object and HttpWebRequests to render HTML for nodes
    /// </summary>
    public class DefaultHttpRenderer : ProgramaticRenderer
    {
        /// <summary>
        /// Render the contents of node at nodeId into string fullHtml
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="fullHtml"></param>
        /// <returns>Bool indicating whether or not to store the result</returns>
        public override bool Render(int nodeId, out string fullHtml)
        {
            Document currentDocument = null;
            try
            {
                currentDocument = new Document(nodeId);
            }
            catch (Exception ex)
            {
                umbraco.BusinessLogic.Log.AddSynced(umbraco.BusinessLogic.LogTypes.Error, 0, nodeId, "Error creating Document in renderer: (" + ex + ")");
                if (Library.IsCritical(ex))
                    throw;
            }
            fullHtml = "";
            if (currentDocument == null || currentDocument.Id < 1)
                return false;
            NodeId = nodeId;

            NodeTypeAlias = currentDocument.ContentType.Alias;
            TemplateId = currentDocument.Template;
            CurrentNodeOrDocument = currentDocument;

            return PageBelongsInIndex() && RetrieveHtml(ref fullHtml);
        }
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