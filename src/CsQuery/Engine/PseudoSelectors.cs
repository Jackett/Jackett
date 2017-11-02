using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Reflection;
using CsQuery.HtmlParser;
using CsQuery.StringScanner;
using CsQuery.StringScanner.Patterns;
using CsQuery.ExtensionMethods.Internal;
using CsQuery.Utility;

namespace CsQuery.Engine
{
    /// <summary>
    /// Factory class for PseudoSelectors: provides an API for managing selectors.
    /// </summary>

    public class PseudoSelectors
    {

        #region contructors

        static PseudoSelectors()
        {
            Items = new PseudoSelectors();
        }

        /// <summary>
        /// Default constructor/.
        /// </summary>
        ///
        /// <exception cref="Exception">
        /// Throws an exception if an instance has already been assigned to the static Items property.
        /// This class should instantiate itself as a singleton.
        /// </exception>

        public PseudoSelectors()
        {
            if (Items!=null) {
                throw new Exception("You can only create one instance of the PseudoSelectors class.");
            }
            InnerSelectors = new ConcurrentDictionary<string, Type>();
            PopulateInnerSelectors();
        }

        

        #endregion

        #region private properties

        private ConcurrentDictionary<string, Type> InnerSelectors;
        
        #endregion

        #region public properties

        /// <summary>
        /// Static instance of the PseudoSelectors singleton.
        /// </summary>

        public static PseudoSelectors Items { get; protected set; }

        #endregion

        #region public methods

        /// <summary>
        /// Gets an instance of a named pseudoselector
        /// </summary>
        ///
        /// <exception cref="ArgumentException">
        /// Thrown when the pseudoselector does not exist
        /// </exception>
        ///
        /// <param name="name">
        /// The name of the pseudoselector
        /// </param>
        ///
        /// <returns>
        /// A new instance
        /// </returns>

        public IPseudoSelector GetInstance(string name) 
        {
            IPseudoSelector ps;
            if (TryGetInstance(name, out ps))
            {
                return ps;
            }
            else 
            {
                throw new ArgumentException(String.Format("Attempt to use nonexistent pseudoselector :{0}", name));
            }
        }

        /// <summary>
        /// Gets a registered pseudoclass filter type. If the name is not registered, an exception is
        /// thrown.
        /// </summary>
        ///
        /// <param name="name">
        /// The name of the pseudoselector.
        /// </param>
        ///
        /// <returns>
        /// The registered type.
        /// </returns>

        public Type GetRegisteredType(string name)
        {
            Type type;
            if (TryGetRegisteredType(name, out type))
            {
                return type;
            }
            else
            {
                throw new KeyNotFoundException("The named pseudoclass filter is not registered.");
            }
        }

        /// <summary>
        /// Try to get the type of a registered pseudoclass filter.
        /// </summary>
        ///
        /// <param name="name">
        /// The name of the pseudoselector.
        /// </param>
        /// <param name="type">
        /// The type.
        /// </param>
        ///
        /// <returns>
        /// true if it the named filter was found, false if not.
        /// </returns>

        public bool TryGetRegisteredType(string name, out Type type)
        {
            return InnerSelectors.TryGetValue(name, out type);
        }

        /// <summary>
        /// Try to gets an instance of a named pseudoclass filter.
        /// </summary>
        ///
        /// <param name="name">
        /// The name of the pseudoselector.
        /// </param>
        /// <param name="instance">
        /// [out] The new instance.
        /// </param>
        ///
        /// <returns>
        /// true if succesful, false if a pseudoselector of that name doesn't exist.
        /// </returns>

        public bool TryGetInstance(string name, out IPseudoSelector instance) {
            Type type;
            if (InnerSelectors.TryGetValue(name, out type))
            {
                instance = (IPseudoSelector)Activator.CreateInstance(type);
                return true;
            }
            instance = null;
            return false;
        }

        /// <summary>
        /// Registers a new PseudoSelector type by name.
        /// </summary>
        ///
        /// <param name="name">
        /// The name of the pseudoselector.
        /// </param>
        /// <param name="type">
        /// The type.
        /// </param>
        ///
        /// <exception cref="ArgumentException">
        /// Throws an exception when the type does not inherit IPseudoSelector.
        /// </exception>

        public void Register(string name, Type type)
        {
            ValidateType(type);
            InnerSelectors[name]= type;
        }

        /// <summary>
        /// Registers all classes implementing IPseudoSelector in the namespace CsQuery.Extensions in the
        /// passed assembly. If no assembly is provided, then inspects the calling assembly instead.
        /// </summary>
        ///
        /// <remarks>
        /// This method is called when the LookForExtensions startup option is set. (This is the default
        /// setting).
        /// </remarks>
        ///
        /// <param name="assembly">
        /// The assembly to search.
        /// </param>
        ///
        /// <returns>
        /// The number of extensions added
        /// </returns>

        public int Register(Assembly assembly=null)
        {
            if (assembly != null)
            {
                return PopulateFromAssembly(assembly, "CsQuery.Engine.PseudoClassSelectors", "CsQuery.Extensions");
            }

            return 0;
        }

        /// <summary>
        /// Unregisters the names pseudoclass filter.
        /// </summary>
        ///
        /// <param name="name">
        /// The name of the pseudoselector.
        /// </param>

        public bool Unregister(string name)
        {
            Type value;
            return InnerSelectors.TryRemove(name, out value);
        }

        #endregion

        #region private methods

        private void ValidateType(Type value)
        {
            if (value.GetTypeInfo().GetInterface("IPseudoSelector") == null)
            {
                throw new ArgumentException("The type must implement IPseudoSelector.");
            }
        }


        private void PopulateInnerSelectors()
        {
            string defaultNamespace = "CsQuery.Engine.PseudoClassSelectors";
            PopulateFromAssembly(typeof(PseudoSelectors).GetTypeInfo().Assembly, defaultNamespace);
            if (InnerSelectors.Count == 0)
            {
                throw new InvalidOperationException(String.Format("I didn't find the native PseudoClassSelectors in the namespace {0}.",defaultNamespace));
            }

            if (CsQuery.Config.StartupOptions.HasFlag(StartupOptions.LookForExtensions))
            {
                Register();
            }
        }

        private int PopulateFromAssembly(Assembly assy, params string[] nameSpaces) 
        {
            int loaded = 0;
            foreach (var t in assy.GetTypes())
            {
                var ti = t.GetTypeInfo();
                if (ti.IsClass && ti.Namespace != null &&
                    !ti.IsAbstract &&
                    nameSpaces.Contains(ti.Namespace))
                {
                    if (ti.GetInterface("IPseudoSelector") != null)
                    {
                        IPseudoSelector instance = (IPseudoSelector)Activator.CreateInstance(t);
                        InnerSelectors[instance.Name]=t;
                        loaded++;
                    }

                }
            }
            return loaded;
        }

        #endregion
    }
}
