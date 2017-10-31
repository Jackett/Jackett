using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.HtmlParser;

namespace CsQuery.Implementation
{
    /// <summary>
    /// An HTML option element.
    /// </summary>

    public class HTMLOptionElement : DomElement, IHTMLOptionElement
    {
        /// <summary>
        /// Default constructor.
        /// </summary>

        public HTMLOptionElement()
            : base(HtmlData.tagOPTION)
        {
        }

        /// <summary>
        /// The value of the OPTIOn element, or empty string if none specified.
        /// </summary>

        public override string Value
        {
            get
            {
                var attrValue = GetAttribute(HtmlData.ValueAttrId, null);
                return attrValue ?? InnerText;
            }
            set
            {
                base.Value = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this object is disabled.
        /// </summary>

        public override bool Disabled
        {
            get
            {
                if (HasAttribute(HtmlData.attrDISABLED)) {
                    return true;
                } else {
                    if (ParentNode.NodeNameID == HtmlData.tagOPTION || ParentNode.NodeNameID == HtmlData.tagOPTGROUP)
                    {
                        var disabled = ((DomElement)ParentNode).HasAttribute(HtmlData.attrDISABLED);
                        if (disabled)
                        {
                            return true;
                        }
                        else
                        {
                            return ParentNode.ParentNode.NodeNameID == HtmlData.tagOPTGROUP &&
                                ((DomElement)ParentNode.ParentNode).HasAttribute(HtmlData.attrDISABLED);                   
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            set
            {
                SetProp(HtmlData.attrDISABLED, value);
            }
        }

        /// <summary>
        /// The form with which the element is associated.
        /// </summary>

        public IHTMLFormElement Form
        {
            get
            {
                IHTMLSelectElement optionOwner = OptionOwner();
                if (optionOwner != null)
                {
                    return optionOwner.Form;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets or sets the label for this Option element
        /// </summary>

        public string Label
        {
            get
            {
                return GetAttribute(HtmlData.tagLABEL);
            }
            set
            {
                SetAttribute(HtmlData.tagLABEL,value);
            }
        }
        /// <summary>
        /// Indicates whether the element is selected or not. This value is read-only. To change the
        /// selection, set either the selectedIndex or selectedItem property of the containing element.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/XUL/Attribute/selected
        /// </url>

        public override bool Selected
        {
            get
            {
                var owner = OptionOwner();
                if (owner != null)
                {
                    return ((DomElement)this).HasAttribute(HtmlData.SelectedAttrId) ||
                        OwnerSelectOptions(owner).SelectedItem == this;
                }
                else
                {
                    return ((DomElement)this).HasAttribute(HtmlData.SelectedAttrId);
                }
            }
            set
            {
                var owner = OptionOwner();
                if (owner != null)
                {
                    if (value)
                    {
                        OwnerSelectOptions(owner).SelectedItem = (DomElement)this;
                    }
                    else
                    {
                        ((DomElement)this).RemoveAttribute(HtmlData.SelectedAttrId);
                    }
                }
                else
                {
                    ((DomElement)this).SetAttribute(HtmlData.SelectedAttrId);
                }
            }
        }

        private IHTMLSelectElement OptionOwner()
        {
            var node = this.ParentNode == null ?
                null :
                this.ParentNode.NodeNameID == HtmlData.tagSELECT ?
                    this.ParentNode :
                        this.ParentNode.ParentNode == null ?
                            null :
                            this.ParentNode.ParentNode.NodeNameID == HtmlData.tagSELECT ?
                                this.ParentNode.ParentNode :
                                null;
            return (IHTMLSelectElement)node;
        }

        private HTMLOptionsCollection OwnerSelectOptions()
        {

            var node = OptionOwner();
            if (node == null)
            {
                throw new InvalidOperationException("The selected property only applies to valid OPTION nodes within a SELECT node");
            }
            return OwnerSelectOptions(node);

        }
        private HTMLOptionsCollection OwnerSelectOptions(IDomElement owner)
        {
            return new HTMLOptionsCollection((IDomElement)owner);
        }
    }
}
