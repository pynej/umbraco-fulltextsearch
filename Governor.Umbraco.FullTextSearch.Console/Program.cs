// Quick and dirty console app to access the Full text search webservice.
// I don't warranty that any part of this application actually works. 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Governor.Umbraco.FullTextSearch.Console.FullTextService;

namespace FullTextConsole
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
                Console.WriteLine("Error: Username or password not found");
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
                            Console.WriteLine("Error: No nodes input");
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
                            Console.WriteLine("Error: No nodes input");
                            return;
                        }
                        ArrayOfInt nodesm = getNodesFromString(args[1]);
                        if (nodesm.Count > 0)
                            client.ReindexFullTextNodesAndChildren(username, password, nodesm);
                        break;
                    default:
                        showInstructions();
                        Console.WriteLine("Error: Function name not recognised");
                        return;
                        break;
                }
            }
            catch (System.ServiceModel.ProtocolException ex)
            {
                // URL Not Found? 
                Console.WriteLine("Error: " + ex.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.ToString());
            }
        }
        static void showInstructions()
        {
            Console.WriteLine("FullTextConsole");
            Console.WriteLine("Usage: FullTextConsole RebuildFullTextIndex - Rebuild the entire full text index");
            Console.WriteLine("Usage: FullTextConsole ReindexAllFullTextNodes - Reindex all published documents on the site");
            Console.WriteLine("Usage: FullTextConsole ReindexFullTextNodes \"1000,1240,...\" - rebuild the specified list of document ids");
            Console.WriteLine("Usage: FullTextConsole ReindexFullTextNodesAndChildren \"1000,1240,...\" - rebuild the specified list of document ids and all children");
            Console.WriteLine("Add the webservice URL, your username and your password to the .exe.config file.");
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
