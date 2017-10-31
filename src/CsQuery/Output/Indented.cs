using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery;
using CsQuery.StringScanner;
using CsQuery.ExtensionMethods.Internal;

//namespace CsQuery.OutputFormatters
//{
//    /// <summary>
//    /// Format with indents, etc. This is totally incomplete.
//    /// </summary>
//    public class Structured : IOutputFormatter
//    {
//        #region constructors

//        public Structured()
//        {
//            Initialize();
//        }
//        public Structured(int indentWidth)
//        {
//            Initialize();

//            string indentString = "";
//            for (int i = 0; i < indentWidth; i++)
//            {
//                indentString += " ";
//            }
//            IndentString = indentString;
//        }
//        public Structured(string indentString)
//        {
//            Initialize();

//            IndentString = indentString;
//        }

//        private void Initialize()
//        {
//            IndentString = "    ";
//            TextBlockWidth = 100;
//        }
//        #endregion

//        #region private properties

//        protected int IndentLevel = 0;
//        StringBuilder sb = new StringBuilder();

//        #endregion

//        #region public properties

//        public string IndentString
//        {
//            get;
//            set;
//        }

//        public int TextBlockWidth
//        {
//            get;
//            set;
//        }

//        #endregion


//        public string Format(CQ selection)
//        {

//            AddContents(selection);

//            return sb.ToString();
//        }

//        protected void AddContents(IEnumerable<IDomObject> sequence)
//        {
//            Indent();

//            foreach (var el in sequence)
//            {
//                switch (el.NodeType)
//                {
//                    case NodeType.ELEMENT_NODE:

//                        if (((IDomElement)el).IsBlock)
//                        {
//                            IndentLevel++;
//                            AddContents(el.ChildNodes);
//                            IndentLevel--;
//                        }
//                        else
//                        {
//                            AddContents(el.ChildNodes);
//                        }
//                        break;


//                }
//            }
//        }


//        private void Indent()
//        {
//            for (int i = 0; i < IndentLevel; i++)
//            {
//                sb.Append(IndentString);
//            }

//        }

//    }
//}
