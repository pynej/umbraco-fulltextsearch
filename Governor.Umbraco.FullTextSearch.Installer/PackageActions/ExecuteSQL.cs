using System;
using Governor.Umbraco.FullTextSearch.Utilities;
using umbraco.interfaces;

namespace Governor.Umbraco.FullTextSearch.Installer.PackageActions
{
    /// <summary>
    /// This just executes a SQL statement. It's used by the installer to set up 
    /// database tables. 
    /// </summary>
    public class ExecuteSQL : IPackageAction
    {
        public string Alias()
        {
            return "FullTextSearch_ExecuteSQL";
        }

        public bool Execute(string packageName, System.Xml.XmlNode xmlData)
        {
            string query = xmlData.SelectSingleNode("//mssql").InnerText;
            if (umbraco.GlobalSettings.DbDSN.ToLower().Contains("datalayer=mysql"))
            {
                throw new NotImplementedException("mysql support not implemented yet. Feel free to add it");
            }
            else if (umbraco.GlobalSettings.DbDSN.ToLower().Contains("vistadb,vistadb"))
            {
                throw new NotImplementedException("vistadb support not implemented yet. Feel free to add it");
            }
            // If we're here we assume we're using MS SQL server
            if (DBAccess.ExecuteNonQuery(query))
            {
                return true;
            }
            throw new Exception("Database table could not be created. Install cannot proceed. Check database permissions");
        }

        public System.Xml.XmlNode SampleXml()
        {
            throw new NotImplementedException();
        }

        public bool Undo(string packageName, System.Xml.XmlNode xmlData)
        {
            throw new NotImplementedException();
        }
    }
}