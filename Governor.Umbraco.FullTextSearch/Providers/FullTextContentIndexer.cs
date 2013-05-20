using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using UmbracoExamine;
using Examine;
using System.IO;
using FullTextSearch.Interfaces;
namespace FullTextSearch.Providers
{
    /// <summary>
    /// We could probably just use the events built into the UmbracoContentIndexer for this, 
    /// however, this is just a little easier to implement for certain things, though it uses 99% just
    /// the functionality of the base class.
    /// </summary>
    public class FullTextContentIndexer : UmbracoContentIndexer
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public FullTextContentIndexer()
            : base() { }

        /// <summary>
        /// Constructor to allow for creating an indexer at runtime
        /// </summary>
        /// <param name="indexerData"></param>
        /// <param name="indexPath"></param>
        public FullTextContentIndexer(IIndexCriteria indexerData, DirectoryInfo indexPath)
            : base(indexerData, indexPath) { }
        
        /// <summary>
        /// We override this method, do some checks and add some stuff and then just call the base method. 
        /// We could do this from an event, but we want the full text renderer/retriever
        /// to run before any other events we might choose to hook into NodeIndexing so users will find 
        /// it easier to modify what's going on
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="writer"></param> 
        /// <param name="nodeId"></param>
        /// <param name="type"></param>
        protected override void AddDocument(Dictionary<string, string> fields, Lucene.Net.Index.IndexWriter writer, int nodeId, string type)
        {
            if( type == IndexTypes.Content && Config.Instance.GetBooleanByKey("Enabled")) 
            {
                // running this sync causes HORRIBLE issues when DefaultHttpRender is in use
                if (this.RunAsync != true && !Config.Instance.GetBooleanByKey("PublishEventRendering"))
                {
                    umbraco.BusinessLogic.Log.AddSynced(umbraco.BusinessLogic.LogTypes.Error, 0, nodeId, "FullTextSearch Cowardly refusing to break your site by firing up Http Render while indexer is in sync mode. If you see nothing in the index this is why!");
                }
                else
                {
                    umbraco.cms.businesslogic.web.Document currentDocument;
                    try
                    {
                        // this seems to have a lot of unidentifiable failure modes...
                        currentDocument = new umbraco.cms.businesslogic.web.Document(nodeId);
                    }
                    catch (Exception ex)
                    {
                        umbraco.BusinessLogic.Log.AddSynced(umbraco.BusinessLogic.LogTypes.Error, 0, nodeId, "Error getting document: (" + ex.ToString() + ")");
                        if (FullTextSearch.Utilities.Library.IsCritical(ex))
                            throw;
                        currentDocument = null;
                    }
                    if (currentDocument != null && currentDocument.Id > 0)
                    {
                        // check if document is protected
                        string path = currentDocument.Path;
                        if (!DataService.ContentService.IsProtected(nodeId, path))
                        {
                            bool cancel;
                            IFullTextIndexer indexer = Manager.Instance.FullTextIndexerFactory.CreateNew(currentDocument.ContentType.Alias);
                            indexer.NodeProcessor(currentDocument, fields, out cancel);
                            if (cancel)
                                return;
                        }
                    }
                }
            }
            base.AddDocument(fields, writer, nodeId, type);
        }
    }
}