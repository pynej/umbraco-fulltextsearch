// Quick and dirty console app to access the Full text search webservice.
// I don't warranty that any part of this application actually works. 

using System;
using Governor.Umbraco.FullTextSearch.Console.FullTextService;

namespace Governor.Umbraco.FullTextSearch.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args == null || args.Length < 1)
            {
                showInstructions();
                return;
            }
            string fnName = args[0].ToLower();
            string username = System.Configuration.ConfigurationManager.AppSettings["username"];
            string password = System.Configuration.ConfigurationManager.AppSettings["password"];
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                showInstructions();
                System.Console.WriteLine("Error: Username or password not found");
                return;
            }
            FullTextServiceSoapClient client = new FullTextServiceSoapClient();
            try
            {
                switch (fnName)
                {
                    case "rebuildfulltextindex":
                        client.RebuildFullTextIndex(username, password);
                        break;
                    case "reindexfulltextnodes":
                        if (string.IsNullOrWhiteSpace(args[1]))
                        {
                            showInstructions();
                            System.Console.WriteLine("Error: No nodes input");
                            return;
                        }
                        ArrayOfInt nodes = getNodesFromString(args[1]);
                        if (nodes.Count > 0)
                            client.ReindexFullTextNodes(username, password, nodes);
                        break;
                    case "reindexallfulltextnodes":
                        client.ReindexAllFullTextNodes(username, password);
                        break;
                    case "reindexfulltextnodesandchildren":
                        if (string.IsNullOrWhiteSpace(args[1]))
                        {
                            showInstructions();
                            System.Console.WriteLine("Error: No nodes input");
                            return;
                        }
                        ArrayOfInt nodesm = getNodesFromString(args[1]);
                        if (nodesm.Count > 0)
                            client.ReindexFullTextNodesAndChildren(username, password, nodesm);
                        break;
                    default:
                        showInstructions();
                        System.Console.WriteLine("Error: Function name not recognised");
                        return;
                }
            }
            catch (System.ServiceModel.ProtocolException ex)
            {
                // URL Not Found? 
                System.Console.WriteLine("Error: " + ex.ToString());
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Error: " + ex.ToString());
            }
        }
        static void showInstructions()
        {
            System.Console.WriteLine("FullTextConsole");
            System.Console.WriteLine("Usage: FullTextConsole RebuildFullTextIndex - Rebuild the entire full text index");
            System.Console.WriteLine("Usage: FullTextConsole ReindexAllFullTextNodes - Reindex all published documents on the site");
            System.Console.WriteLine("Usage: FullTextConsole ReindexFullTextNodes \"1000,1240,...\" - rebuild the specified list of document ids");
            System.Console.WriteLine("Usage: FullTextConsole ReindexFullTextNodesAndChildren \"1000,1240,...\" - rebuild the specified list of document ids and all children");
            System.Console.WriteLine("Add the webservice URL, your username and your password to the .exe.config file.");
        }
        static ArrayOfInt getNodesFromString(string nodeString)
        {
            string[] txts = nodeString.Split(',');
            ArrayOfInt nodes = new ArrayOfInt();
            foreach (string t in txts)
            {
                int i;
                if (Int32.TryParse(t, out i))
                    nodes.Add(i);
            }
            return nodes;
        }
    }
}
