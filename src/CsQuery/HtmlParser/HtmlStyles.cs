using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using CsQuery.Implementation;
using CsQuery.Utility;

namespace CsQuery.HtmlParser
{
    /// <summary>
    /// A dictionary of valid styles, based on a Visual Studio format XML schema. 
    /// </summary>
    public static class HtmlStyles
    {
        /// <summary>
        /// Dictionary mapping style names to CssStyle style definitions
        /// </summary>

        public static Dictionary<string, CssStyle> StyleDefs = new Dictionary<string, CssStyle>();
        
        private static char[] StringSep = new char[] { ' ' };
        
        static HtmlStyles()
        {
            XmlDocument xDoc = new XmlDocument();
            //Stream dataStream = Support.GetResourceStream("CsQuery.Resources." + CssDefs);
            xDoc.Load(new StringReader(Resources.Css3Xml));

            XmlNamespaceManager nsMan = new XmlNamespaceManager(xDoc.NameTable);
            nsMan.AddNamespace("cssmd", "http://schemas.microsoft.com/Visual-Studio-Intellisense/css");

            var nodes = xDoc.DocumentElement.SelectNodes("cssmd:property-set/cssmd:property-def", nsMan);

            string type;

            foreach (XmlNode  el in nodes)
            {
                CssStyle st = new CssStyle();
                st.Name = el.Attributes["_locID"].Value;
                type = el.Attributes["type"].Value;
                switch (type)
                {
                    case "length": st.Type = CSSStyleType.Unit; break;
                    case "color": st.Type = CSSStyleType.Color; break;
                    case "composite": st.Type = CSSStyleType.Composite;
                        st.Format = el.Attributes["syntax"].Value;
                        break;
                    case "enum":
                    case "enum-length":
                        
                        if (type == "enum-length")
                        {
                            st.Type = CSSStyleType.UnitOption;
                        } else {
                            st.Type = CSSStyleType.Option;
                        }
                        st.Options = new HashSet<string>(el.Attributes["enum"].Value
                            .Split(StringSep, StringSplitOptions.RemoveEmptyEntries));
                        break;
                    case "font":
                        st.Type = CSSStyleType.Font;
                        break;
                    case "string":
                        st.Type = CSSStyleType.String;
                        break;
                    case "url":
                        st.Type = CSSStyleType.Url;
                        break;
                    default:
                        throw new NotImplementedException("Error parsing css xml: unknown type '" + type + "'");
                }
                st.Description = el.Attributes["description"].Value;
                StyleDefs[st.Name] = st;
            }
        }
            


    }
}
