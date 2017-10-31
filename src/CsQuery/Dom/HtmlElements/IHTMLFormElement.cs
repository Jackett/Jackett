using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery
{   
    /// <summary>
    /// A FORM element.
    /// </summary>
    ///
    /// <url>
    /// http://dev.w3.org/html5/spec/single-page.html#the-form-element
    /// </url>

    public interface IHTMLFormElement : IDomElement, INodeList<IDomElement>
    {
        /// <summary>
        /// The accept-charset content attribute.
        /// </summary>

        string AcceptCharset {get;set;}

        /// <summary>
        /// The action attribute
        /// </summary>

        string Action {get;set;}

        /// <summary>
        /// The automcomplete attribute
        /// </summary>

        string Autocomplete {get;set;}

        /// <summary>
        /// Gets or sets the enctype.
        /// </summary>

        string Enctype {get;set;}

        /// <summary>
        /// Gets or sets the encoding.
        /// </summary>

        string Encoding {get;set;}

        /// <summary>
        /// Gets or sets the method attribute.
        /// </summary>

        string Method {get;set;}

        /// <summary>
        /// Gets or sets a value indicating whether the no validate.
        /// </summary>

        bool NoValidate {get;set;}

        /// <summary>
        /// Gets or sets the target attribute
        /// </summary>

        string Target {get;set;}

        /// <summary>
        /// An INodeList containing the form elements.
        /// </summary>

        INodeList<IDomElement> Elements {get;}
        
        // could be implemented?
        // 
        //bool CheckValiditry()
        //void Reset();
        
        // no
        // 
        //void Submit();
    }
}
