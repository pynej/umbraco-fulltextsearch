using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using umbraco.NodeFactory;
using FullTextSearch.EventHandlers;

namespace FullTextSearch.Interfaces
{
    /// <summary>
    /// Any class implementing this interface can register itself
    /// as a renderer (retrieve HTML from umbraco to index/cache) for any or all 
    /// node types using the Manager singleton. 
    /// </summary>
    public interface IDocumentRenderer
    {
        bool Render(int nodeId, out string fullHtml);
    }
}
