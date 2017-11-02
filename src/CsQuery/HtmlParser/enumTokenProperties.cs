using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.HtmlParser
{
    /// <summary>
    /// Bitfield of flags for specifying properties that may be tested on HTML tokens using a bitmap.
    /// </summary>

    [Flags]
    public enum TokenProperties: ushort
    {
        /// <summary>
        /// the element is an HTML block-level element
        /// </summary>
        
        BlockElement=1,

        /// <summary>
        /// the attribute is a boolean property e.g. 'checked'
        /// </summary>
        
        BooleanProperty=2,
        
        /// <summary>
        /// the tag is automatically closing, e.g. 'p'.
        /// </summary>
        
        AutoOpenOrClose=4,
        
        /// <summary>
        /// the tag may not have children
        /// </summary>
        
        ChildrenNotAllowed=8,

        /// <summary>
        /// the tag may not have HTML children (but could possibly have children)
        /// </summary>
        
        HtmlChildrenNotAllowed=16,

        /// <summary>
        /// this tag causes an open p tag to close
        /// </summary>
        
        ParagraphCloser=32,

        /// <summary>
        /// The tag may appear in HEAD
        /// </summary>
        
        MetaDataTags = 64,
       
        /// <summary>
        /// election of attribute values is not case sensitive
        /// </summary>
        
        CaseInsensitiveValues = 128,

        /// <summary>
        /// Has a VALUE property
        /// </summary>
        
        HasValue = 256,

        /// <summary>
        /// Element is a form input control
        /// </summary>
        
        FormInputControl = 512
    }
}
