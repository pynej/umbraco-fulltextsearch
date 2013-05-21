using System;
using System.Linq;
using System.Web.Services;
using System.ComponentModel;
using Governor.Umbraco.FullTextSearch.Admin;

namespace Governor.Umbraco.FullTextSearch.WebService
{
    [WebService(Namespace = "http://umbraco.org/webservices/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    // Web service that inheits the basic umbraco web service functionality
    // See umbraco docs for how to use this
    public class FullTextService : umbraco.webservices.BaseWebService
    {

        override public Services Service
        {
            get
            {
                return Services.MaintenanceService;
            }
        }
        // Standard umbraco web service method
        [WebMethod]
        public string GetWebservicesVersion(string username, string password)
        {
            // We check if services are enabled and user has access
            Authenticate(username, password);

            var thisVersion = new Version(0, 9);
            return Convert.ToString(thisVersion);
        }
        /// <summary>
        /// Rebuild the entire full text index.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        [WebMethod]
        public void RebuildFullTextIndex(string username, string password)
        {
            // We check if services are enabled and user has access
            Authenticate(username, password);
            AdminActions.RebuildFullTextIndex();
        }
        /// <summary>
        /// Re-index the supplied list of nodes in the full text index
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="nodes"></param>
        [WebMethod]
        public void ReindexFullTextNodes(string username, string password, int[] nodes)
        {
            // We check if services are enabled and user has access
            Authenticate(username, password);
            if (nodes != null && nodes.Length > 0)
            {
                AdminActions.ReindexFullTextNodes(nodes.ToList());
            }
        }
        /// <summary>
        /// Re-index all nodes in the full text index, but do not delete and rebuld
        /// the entire index as with RebuildFullTextIndex
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        [WebMethod]
        public void ReindexAllFullTextNodes(string username, string password)
        {
            // We check if services are enabled and user has access
            Authenticate(username, password);
            AdminActions.ReindexAllFullTextNodes();
        }
        /// <summary>
        /// /// Re-index the supplied list of nodes and all descendants in the full text index
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="nodes"></param>
        [WebMethod]
        public void ReindexFullTextNodesAndChildren(string username, string password, int[] nodes)
        {
            // We check if services are enabled and user has access
            Authenticate(username, password);
            if (nodes != null && nodes.Length > 0)
            {
                AdminActions.ReindexFullTextNodesAndChildren(nodes);
            }
        }
    }
}
