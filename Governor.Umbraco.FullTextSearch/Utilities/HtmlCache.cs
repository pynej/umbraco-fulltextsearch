namespace Governor.Umbraco.FullTextSearch.Utilities
{
    public class HtmlCache
    {
        public static bool Store(int nodeId, ref string fullHtml)
        {
            return DBAccess.UpdateRecord(nodeId, ref fullHtml);
        }
        public static bool Remove(int nodeId)
        {
            return DBAccess.DeleteRecord(nodeId);
        }
        public static bool Retrieve(int nodeId, out string fullHtml)
        {
            return DBAccess.GetRecord(nodeId, out fullHtml);                
        }
    }
}