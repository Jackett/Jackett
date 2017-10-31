using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Implementation
{

    /// <summary>
    /// Base class for anything that exists in the DOM
    /// </summary>
    /// 
    public abstract class DomObject<T> : DomObject, IDomObject<T> where T : IDomObject, new()
    {
        /// <summary>
        /// Default constructor for the abstract class.
        /// </summary>

        public DomObject()
        {
            
        }

        /// <summary>
        /// Makes a deep copy of this object.
        /// </summary>
        ///
        /// <returns>
        /// A copy of this object.
        /// </returns>

        public abstract new T Clone();

        /// <summary>
        /// This is called by the base class DomObject, and ensures that the typed Clone implementations
        /// get called when the object is accessed through the IDomObject interface.
        /// </summary>
        ///
        /// <returns>
        /// A new IDomObject
        /// </returns>

        protected override IDomObject CloneImplementation()
        {
            return Clone();
        }


        IDomNode IDomNode.Clone()
        {
            return Clone();
        }
        
        
    }
    
}
