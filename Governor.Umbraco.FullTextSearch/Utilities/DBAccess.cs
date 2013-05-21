using System.Collections.Generic;
using umbraco.DataLayer;

namespace Governor.Umbraco.FullTextSearch.Utilities
{
    public class DBAccess
    {
        /// <summary>
        /// Retrieve HTML from the database cache for the given node
        /// </summary>
        /// <param name="nodeId">Id of the node</param>
        /// <param name="fullHTML">string to fill with HTML</param>
        /// <returns>bool indicating success/failure</returns>
        public static bool GetRecord(int nodeId, out string fullHTML)
        {
            fullHTML = "";
            
            if (nodeId < 1)
                return false;
            
            bool success = false;
            ISqlHelper SqlHelper = null;
            IRecordsReader result = null;
            try
            {
                string sqlQuery = "SELECT fullHTML FROM fullTextCache WHERE nodeId = @nodeId";
                SqlHelper = DataLayerHelper.CreateSqlHelper(umbraco.GlobalSettings.DbDSN);
                result = SqlHelper.ExecuteReader(sqlQuery,SqlHelper.CreateParameter("@nodeId", nodeId));
                if (result != null && result.HasRecords && result.Read() && result.ContainsField("fullHTML"))
                {
                    fullHTML = result.GetString("fullHTML");
                    success = true;
                }
            }
            catch (umbraco.UmbracoException ex)
            {
                umbraco.BusinessLogic.Log.AddSynced(umbraco.BusinessLogic.LogTypes.Error, 0, 0, "Error In Database Query to fullTextCache: (" + ex.ToString() + ")");
                fullHTML = "";
            }
            finally
            {
                if(result != null)
                    result.Close();
                if (SqlHelper != null)
                    SqlHelper.Dispose();
            }
            return success;
        }
        /// <summary>
        /// Delete old record(if present) and add a new one
        /// </summary>
        /// <param name="nodeId">The ID of the node</param>
        /// <param name="nodeHTML">The HTML generated when the page is viewed</param>
        /// <returns>bool indicating success/failure</returns>
        public static bool UpdateRecord(int nodeId, ref string nodeHTML)
        {
            if (nodeId < 1)
                return false;
            
            if(DeleteRecord(nodeId))
                if (AddRecord(nodeId, ref nodeHTML))
                    return true;

            return false;
        }
        
        /// <summary>
        /// Add a record to the database containing full HTML for given node
        /// </summary>
        /// <param name="nodeId">The ID of the node</param>
        /// <param name="nodeHTML">The HTML generated when the page is viewed</param>
        /// <returns>bool indicating success/failure</returns>
        public static bool AddRecord(int nodeId, ref string nodeHTML)
        {
            if (nodeId < 1)
                return false;

            Dictionary<string,object> parameters = new Dictionary<string,object>();
            string sqlQuery = @"INSERT INTO fullTextCache (nodeId,fullHTML) VALUES (@nodeId,@fullHTML)";
            parameters.Add("@nodeId",nodeId);
            parameters.Add("@fullHTML",nodeHTML);
            // we need to see rows added to indicate success
            int? res = nonQuery(sqlQuery,parameters);
            if( res != null && res > 0)
                return true;

            return false;
        }
        /// <summary>
        /// Remove a record from the fullText cache.
        /// </summary>
        /// <param name="nodeId">the ID of the node to remove</param>
        /// <returns>bool indicating success/failure</returns>
        public static bool DeleteRecord(int nodeId)
        {
            if (nodeId < 1)
                return false;

            Dictionary<string, object> parameters = new Dictionary<string, object>();
            string sqlQuery = @"DELETE FROM fullTextCache WHERE nodeId = @nodeId";
            parameters.Add("@nodeId",nodeId);
            // only a SQL error counts as failure here. No rows deleted is fine (there might not be any)
            int? res = nonQuery(sqlQuery, parameters);
            if(res != null)
                return true;
            return false;
        }
        public static bool ExecuteNonQuery(string query)
        {
            int? res = nonQuery(query, new Dictionary<string, object>());
            if (res != null)
                return true;
            return false;
        }
        /// <summary>
        /// Wrapper around umbraco's sqlHelper.ExecuteNonQuery that handles a few exceptions
        /// </summary>
        /// <param name="query">The SQL Query to execute</param>
        /// <param name="parameters">Dictionary mapping parameter name to parameter value</param>
        /// <returns>The number of rows affected, or -1 on failure</returns>
        private static int? nonQuery(string query, Dictionary<string,object> parameters)
        {
            int? numRows = null;
            ISqlHelper SqlHelper = null;
            try
            {
                SqlHelper = DataLayerHelper.CreateSqlHelper(umbraco.GlobalSettings.DbDSN);
                int numParams = parameters.Count;
                if (numParams < 1)
                {
                    numRows = SqlHelper.ExecuteNonQuery(query);
                }
                else
                {
                    IParameter[] sqlParameters = new IParameter[numParams];
                    int i = 0;
                    foreach (KeyValuePair<string, object> parameter in parameters)
                    {
                        sqlParameters[i++] = SqlHelper.CreateParameter(parameter.Key, parameter.Value);
                    }
                    numRows = SqlHelper.ExecuteNonQuery(query,sqlParameters);
                }
            }
            catch (umbraco.UmbracoException ex)
            {
                umbraco.BusinessLogic.Log.AddSynced(umbraco.BusinessLogic.LogTypes.Error, 0, 0, "Error In Database Query to fullTextCache: (" + ex.ToString() + ")");
                numRows = null;
            }
            finally
            {
                if (SqlHelper != null)
                    SqlHelper.Dispose();
            }
            return numRows;
        }
    }
}