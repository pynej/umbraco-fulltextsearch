using System.Collections.Generic;

namespace Governor.Umbraco.FullTextSearch
{
    /// <summary>
    /// Allows different classes implementing the same interfaces to be mapped to strings, 
    /// the string passed to CreateNew then decides which implementation gets instanciated and 
    /// passed back to the caller.
    /// </summary>
    /// <typeparam name="Interface"></typeparam>
    public class GenericFactory<Interface>
    {
        private Dictionary<string,CreateDelegate> mapping;
        private CreateDelegate defaultClass;
        private delegate Interface CreateDelegate();
        /// <summary>
        /// Register Class to be created when key is passed to CreateNew
        /// </summary>
        /// <typeparam name="Class">Any Class Implementing Interface</typeparam>
        /// <param name="key">the string to identify Class by</param>
        public void Register<Class>(string key) where Class : Interface, new()
        {
            if (mapping == null)
                mapping = new Dictionary<string, CreateDelegate>();

            CreateDelegate createme = CreateFunction<Class>;
            if(! mapping.ContainsKey(key) )
                mapping.Add(key,createme);
        }
        /// <summary>
        /// Register the class that will be returned when no string "key" is found
        /// in the dictionary by CreateNew
        /// </summary>
        /// <typeparam name="Class">Any Class Implementing Interface</typeparam>
        public void RegisterDefault<Class>() where Class : Interface, new()
        {
            CreateDelegate createme = CreateFunction<Class>;
            defaultClass = createme;
        }
        /// <summary>
        /// Instanciate the class regestered for key and return the object.
        /// return a default class if key is not found in the dictionary
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Interface CreateNew(string key)
        {
            CreateDelegate createme;
            if (mapping != null && mapping.ContainsKey(key))
                createme = mapping[key];
            else
                createme = defaultClass;
            return createme();
        }
        private Interface CreateFunction<Class>() where Class : Interface, new()
        {
            return new Class();
        }
    }
}