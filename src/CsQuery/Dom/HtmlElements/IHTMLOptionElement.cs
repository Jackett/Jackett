using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery
{   
    /// <summary>
    /// An HTMLOPTION element
    /// </summary>
    /// <url>http://dev.w3.org/html5/spec/single-page.html#attr-option-disabled</url>

    public interface IHTMLOptionElement : IDomElement
    {
        /// <summary>
        /// The form with which the element is associated
        /// </summary>

        IHTMLFormElement Form {get;}

        /// <summary>
        /// Gets or sets the label attribute.
        /// </summary>

        string Label {get;set;}

        //bool DefaultSelected {get;set;}
          
        // inherited from IDomObject
        //bool Selected {get;set;}
          
        //string Value;
        // string Text;

        //int Index { get; }
    }
}
