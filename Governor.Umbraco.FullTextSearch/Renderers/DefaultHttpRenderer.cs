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
                umbraco.BusinessLogic.Log.AddSynced(umbraco.BusinessLogic.LogTypes.Error, 0, nodeId, "Error creating Document in renderer: (" + ex.ToString() + ")");
                if (Library.IsCritical(ex))
                    throw;
            }
            fullHtml = "";
            if (currentDocument == null || currentDocument.Id < 1)
                return false;
            this.nodeId = nodeId;

            this.nodeTypeAlias = currentDocument.ContentType.Alias;
            this.templateId = currentDocument.Template;
            this.currentNodeOrDocument = currentDocument;

            if (!pageBelongsInIndex())
                return false;

            if (retrieveHTML(ref fullHtml))
                return true;

            return false;
        }
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