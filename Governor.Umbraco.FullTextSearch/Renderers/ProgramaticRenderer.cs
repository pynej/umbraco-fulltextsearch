using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FullTextSearch.Interfaces;
using umbraco.NodeFactory;
using System.Collections.Specialized;
using FullTextSearch.Utilities;
using umbraco.cms.businesslogic.web;

namespace FullTextSearch.Renderers
{
    /// <summary>
    /// This needs to be used when the umbraco core is active. It uses the current
    /// HTTP context, the node factory, and server.execute to render nodes for caching
    /// It can be subclassed using document objects from outside the core easily enough though
    /// see DefaultHttpRenderer
    /// </summary>
    public class ProgramaticRenderer : IDocumentRenderer
    {
        protected int nodeId;
        protected int templateId;
        protected string nodeTypeAlias;

        private object currentNodeOrDocumentBacking;
        protected object currentNodeOrDocument
        {
            get
            {
                return currentNodeOrDocumentBacking;
            }
            set
            {
                if (value is Document || value is Node)
                    currentNodeOrDocumentBacking = value;
                else
                    throw new ArgumentException("currentNodeOrDocument must be umbraco nodefactory or cms.businesslogic.web.Document object");
            }
        }
        

        /// <summary>
        /// Render the contents of node at nodeId into string fullHtml
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="fullHtml"></param>
        /// <returns>Bool indicating whether or not to store the result in the UmbracoFullText HTML cache</returns>
        public virtual bool Render(int nodeId, out string fullHtml)
        {
            Node currentNode = null;
            try
            {
                currentNode = new Node(nodeId);
            }
            catch(Exception ex)
            {
                umbraco.BusinessLogic.Log.AddSynced(umbraco.BusinessLogic.LogTypes.Error, 0, nodeId, "Error creating nodefactory node in renderer: (" + ex.ToString() + ")");
                if (Library.IsCritical(ex))
                    throw;
            }
            fullHtml = "";
            if (currentNode == null || currentNode.Id < 1)
                return false;
            this.nodeId = nodeId;
            this.templateId = currentNode.template;
            this.nodeTypeAlias = currentNode.NodeTypeAlias;
            this.currentNodeOrDocument = currentNode;
            if (!pageBelongsInIndex())
                return false;

            if (retrieveHTML(ref fullHtml))
                return true;

            return false;

        }
        /// <summary>
        /// Check whether this page should have the full text read for indexing
        /// </summary>
        /// <returns>true/false</returns>
        protected virtual bool pageBelongsInIndex()
        {
            // only index nodes with a template
            if (templateId < 1)
                return false;

            // check if the config specifies we shouldn't index this
            if (isDisallowedNodeType())
            {
                return false;
            }
            // or if there's a property (e.g. umbracoNaviHide)
            // that is keeping this page out of the index
            if (isSearchHideActive())
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// check the node type of currentNode against those listed in the config file
        /// to see if this page has full text indexing disabled
        /// </summary>
        /// <returns></returns>
        protected virtual bool isDisallowedNodeType()
        {
            Config config = Config.Instance;

            List<string> NoFullTextNodeTypes = config.GetMultiByKey("NoFullTextNodeTypes");
            if (NoFullTextNodeTypes != null && NoFullTextNodeTypes.Contains(nodeTypeAlias))
                return true;
            return false;
        }
        /// <summary>
        /// Check the properties of currentNode against thost listed in the config file to see if this page
        /// has been hidden from the search index
        /// </summary>
        /// <returns></returns>
        protected virtual bool isSearchHideActive()
        {
            return Library.IsSearchDisabledByProperty(currentNodeOrDocument);
        }
        /// <summary>
        /// Calls our custom Rendertemplate, sets up some parameters to pass to the child page
        /// </summary>
        protected virtual bool retrieveHTML(ref string fullHtml)
        {
            
            Dictionary<string,string> queryStringCollection = Library.getQueryStringCollection();
            try
            {
                fullHtml = Utilities.Library.RenderTemplate(nodeId, templateId, queryStringCollection);
                return true;
            }
            catch (Exception ex)
            {
                umbraco.BusinessLogic.Log.AddSynced(umbraco.BusinessLogic.LogTypes.Error, 0, nodeId, "Error rendering page in FullTextSearch: (" + ex.ToString() + ")");
                if (Library.IsCritical(ex))
                    throw;
                fullHtml = "";
                return false;
            }
        }
    }
}