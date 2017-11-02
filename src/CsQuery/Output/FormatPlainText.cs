using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CsQuery;
using CsQuery.StringScanner;
using CsQuery.ExtensionMethods.Internal;

namespace CsQuery.Output
{
    /// <summary>
    /// A formatter that converts a DOM to a basic plain-text version.
    /// </summary>
    
    public class FormatPlainText : IOutputFormatter
    {
        private IStringInfo stringInfo;

        /// <summary>
        /// Renders this object to the passed TextWriter.
        /// </summary>
        ///
        /// <param name="node">
        /// The node.
        /// </param>
        /// <param name="writer">
        /// The writer.
        /// </param>

        public void Render(IDomObject node, TextWriter writer)
        {
            stringInfo = CharacterData.CreateStringInfo();

            StringBuilder sb = new StringBuilder();
            AddContents(sb, node,true);
            writer.Write(sb.ToString());
        }

        /// <summary>
        /// Renders this object and returns the output as a string.
        /// </summary>
        ///
        /// <param name="node">
        /// The node.
        /// </param>
        ///
        /// <returns>
        /// A string of HTML.
        /// </returns>

        public string Render(IDomObject node)
        {
            using (StringWriter writer = new StringWriter())
            {
                Render(node, writer);
                return writer.ToString();
            }
        }

        /// <summary>
        /// Adds the contents to 'node' to the StringBuilder.
        /// </summary>
        ///
        /// <param name="sb">
        /// The StringBuilder.
        /// </param>
        /// <param name="node">
        /// The node.
        /// </param>
        /// <param name="skipWhitespace">
        /// true to skip any leading whitespace for this node.
        /// </param>

        protected void AddContents(StringBuilder sb, IDomObject node, bool skipWhitespace)
        {
            // always skip the opening whitespace of a new child block
            
            if (node.HasChildren)
            {
                foreach (IDomObject el in node.ChildNodes)
                {
                    if (el.NodeType == NodeType.TEXT_NODE)
                    {
                        IDomText txtNode = (IDomText)el;
                        stringInfo.Target = el.NodeValue;

                        if (stringInfo.Whitespace)
                        {
                            if (!skipWhitespace)
                            {
                                sb.Append(" ");
                                skipWhitespace = true;
                            }
                        }
                        else
                        {
                            string val = CleanFragment(el.Render());
                            if (skipWhitespace)
                            {
                                val = val.TrimStart();
                                skipWhitespace = false;
                            }

                            sb.Append(val);
                        }
                        
                        
                    }
                    else if (el.NodeType == NodeType.ELEMENT_NODE)
                    {
                        IDomElement elNode = (IDomElement)el;
                        // first add any inner contents

                        if (el.NodeName != "HEAD" && el.NodeName != "STYLE" && el.NodeName != "SCRIPT")
                        {
                            switch (elNode.NodeName)
                            {
                                case "BR":
                                    sb.Append(System.Environment.NewLine);
                                    break;
                                case "PRE":
                                    RemoveTrailingWhitespace(sb);
                                    sb.Append(System.Environment.NewLine);
                                    sb.Append(ToStandardLineEndings(el.TextContent));
                                    RemoveTrailingWhitespace(sb);
                                    sb.Append(System.Environment.NewLine);
                                    skipWhitespace = true;
                                    break;
                                case "A":
                                    sb.Append(el.TextContent + " (" + el["href"] + ")");
                                    break;
                                default:
                                    
                                    if (elNode.IsBlock && sb.Length>0)
                                    {
                                        RemoveTrailingWhitespace(sb);
                                        sb.Append(System.Environment.NewLine);
                                    }

                                    AddContents(sb, el,elNode.IsBlock);
                                    RemoveTrailingWhitespace(sb);
                                    
                                    if (elNode.IsBlock)
                                    {
                                        sb.Append(System.Environment.NewLine);
                                        skipWhitespace = true;
                                    }
                                   
                                    break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Converts the newline characters in a string to standard system line endings
        /// </summary>
        ///
        /// <param name="text">
        /// The text.
        /// </param>
        ///
        /// <returns>
        /// The converted string
        /// </returns>

        protected string ToStandardLineEndings(string text)
        {
            return text.Replace("\r\n", "\n").Replace("\n", "\r\n");
        }

        /// <summary>
        /// Removes trailing whitespace in this StringBuilder
        /// </summary>
        ///
        /// <param name="sb">
        /// The StringBuilder.
        /// </param>

        protected void RemoveTrailingWhitespace(StringBuilder sb)
        {
            // erase ending whitespace -- scan backwards until non-whitespace
                
            int i = sb.Length - 1;
            int count = 0;
            while (i >= 0 && CharacterData.IsType(sb[i], CharacterType.Whitespace))
            {
                i--;
                count++;
            }
            if (i < sb.Length - 1)
            {
                sb.Remove(i + 1, count);
            }
                
        }

        /// <summary>
        /// Clean a string fragment for output as text
        /// </summary>
        ///
        /// <param name="text">
        /// The text.
        /// </param>
        ///
        /// <returns>
        /// The clean text
        /// </returns>

        protected string CleanFragment(string text)
        {
            var charInfo = CharacterData.CreateCharacterInfo();

            StringBuilder sb = new StringBuilder();
            int index = 0;
            bool trimmed = true;
            while (index < text.Length)
            {
                charInfo.Target = text[index];
                if (!trimmed && !charInfo.Whitespace)
                {
                    trimmed = true;
                }
                if (trimmed)
                {
                    if (charInfo.Whitespace)
                    {
                        // convert all whitespace blocks into a single space
                        sb.Append(" ");
                        trimmed = false;
                    }
                    else
                    {
                        sb.Append(text[index]);
                    }
                }
                index++;
            }

            return sb.ToString();
        }

    }
}
