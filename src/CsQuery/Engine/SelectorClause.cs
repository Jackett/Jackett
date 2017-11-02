using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.HtmlParser;
using CsQuery.ExtensionMethods.Internal;

namespace CsQuery.Engine
{
    /// <summary>
    /// A CSS selector parsed into it's component parts
    /// </summary>
    public class SelectorClause
    {

        #region constructors

        /// <summary>
        /// Default constructor.
        /// </summary>

        public SelectorClause()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes this object to its default state
        /// </summary>

        protected void Initialize()
        {
            SelectorType = 0;
            AttributeSelectorType = AttributeSelectorType.Equals;
            CombinatorType = CombinatorType.Root;
            TraversalType = TraversalType.All;
            AttributeValueStringComparison = StringComparison.CurrentCulture;
            AttributeValue = "";
        }

        #endregion

        #region private properties

        private string _Tag;
        private string _AttributeName;
        private string _AttributeValue;
        private StringComparer _AttributeValueStringComparer;

        private bool IsCaseInsensitiveAttributeValue
        {
            get
            {
                return HtmlData.IsCaseInsensitiveValues(AttributeNameTokenID);
            }
        }



        #endregion

        #region public properties

        /// <summary>
        /// The type of the selector clause.
        /// </summary>

        public SelectorType SelectorType {get;set;}

        /// <summary>
        /// The CombinatorType for this selector clause; this determines what set of elements it is applied to.
        /// </summary>

        public CombinatorType CombinatorType { get; set; }

        /// <summary>
        /// The TraversalType for this clause; this determines the depth of children to test for certain selector types.
        /// </summary>

        public TraversalType TraversalType { get; set; }

        /// <summary>
        /// The AttributeSelectorType determines how values are matched for attribute selectors.
        /// </summary>

        public AttributeSelectorType AttributeSelectorType { get; set; }

        /// <summary>
        /// When this is a pseudoselector, the implementation.
        /// </summary>
        ///
        /// <value>
        /// The pseudo selector.
        /// </value>

        public IPseudoSelector PseudoSelector { get; set; }

        /// <summary>
        /// Selection tag name
        /// </summary>
        
        public string Tag
        {
            get
            {
                return _Tag;
            }
            set
            {
                _Tag = value == null ?
                    value:
                    value.ToUpper();
            }
        }
    
        /// <summary>
        /// This is really "parameters" and is used differently by different selectors. It's the criteria for attribute selectors;
        /// the node type for -of-type selectors, the equation for nth-child. For nth-of-type, its "type|equation"
        /// </summary>
        public string Criteria {get;set;}

        /// <summary>
        /// Gets or sets zero-based index of the position.
        /// </summary>
        /// <summary>
        /// For Position selectors, the position. Negative numbers start from the end.
        /// </summary>

        public int PositionIndex { get; set; }

        /// <summary>
        /// For Child selectors, the depth of the child.
        /// </summary>

        public int ChildDepth { get; set; }

        /// <summary>
        /// For attribute selectors, gets or sets the name of the attribute to match 
        /// </summary>

        public string AttributeName
        {
            get
            {
                return _AttributeName;
            }
            set
            {
                if (value == null)
                {
                    _AttributeName = null;
                    AttributeNameTokenID = 0;
                    AttributeValueStringComparison = StringComparison.CurrentCulture;
                }
                else
                {
                    _AttributeName = value.ToLower();
                    AttributeNameTokenID = HtmlData.Tokenize(value);
                    AttributeValueStringComparison = IsCaseInsensitiveAttributeValue ?
                        StringComparison.CurrentCultureIgnoreCase :
                        StringComparison.CurrentCulture;
                }
            }
        }
        /// <summary>
        /// For AttributeValue selectors, the value to match
        /// </summary>

        public string AttributeValue
        {
            get
            {
                return _AttributeValue;
            }
            set
            {
                _AttributeValue = value ?? "";
            }
        }

        /// <summary>
        /// Gets or sets the identifier of the attribute name token.
        /// </summary>

        public ushort AttributeNameTokenID
        {
            get; private set;
        }

        /// <summary>
        /// Returns a string comparer based on the case-sensitivity characteristics of the attribute being tested
        /// </summary>

        public StringComparison AttributeValueStringComparison
        {
            get; private set;
        }

        /// <summary>
        /// Returns a string comparer based on the case-sensitivity characteristics of the attribute being tested
        /// </summary>

        public StringComparer AttributeValueStringComparer
        {
            get
            {
                if (_AttributeValueStringComparer == null)
                {
                    _AttributeValueStringComparer = AttributeValueStringComparison.ComparerFor();
                }
                return _AttributeValueStringComparer;
            }

        }


        /// <summary>
        /// For Class selectors, the class name to match
        /// </summary>

        public string Class { get; set; }

        /// <summary>
        /// For ID selectors, the ID to match
        /// </summary>

        public string ID { get; set; }

        /// <summary>
        /// The HTML to create, for HTML "selectors"
        /// </summary>

        public string Html { get; set; }

        /// <summary>
        /// The list of elements that should be matched, for elements selectors.
        /// </summary>

        public IEnumerable<IDomObject> SelectElements { get; set; }

        /// <summary>
        /// Gets a value indicating whether this object is a selector that is based on the element's
        /// position in the DOM, versus the element's position in the result set.
        /// </summary>

        public bool IsDomPositionPseudoSelector
        {
            get
            {
                if (SelectorType != SelectorType.PseudoClass)
                {
                    return false;
                }
                return !IsResultListPosition;
            }
        }

        /// <summary>
        /// Indicates that a position type selector refers to the result list, not the DOM position.
        /// </summary>

        public bool IsResultListPosition
        {
            get
            {
                if (SelectorType != SelectorType.PseudoClass)
                {
                    return false;
                } else {
                    return PseudoSelector is IPseudoSelectorFilter;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this selector accepts parameters.
        /// </summary>

        public bool IsFunction
        {
            get
            {
                return PseudoSelector.MaximumParameterCount != 0;
                
            }
        }

        /// <summary>
        /// Gets a value indicating whether this Selector is new (unconfigured).
        /// </summary>

        public bool IsNew
        {
            get
            {
                return SelectorType == 0
                    && ChildDepth == 0
                    && TraversalType == TraversalType.All
                    && CombinatorType == CombinatorType.Root;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this object is completely configured.
        /// </summary>

        public bool IsComplete
        {
            get
            {
                return SelectorType != 0;
            }
        }

        /// <summary>
        /// When true do not attempt to use the index to obtain a result from this selector. Used for
        /// automatically generated filters.
        /// </summary>

        public bool NoIndex { get; set; }

        #endregion

        #region public methods

        /// <summary>
        /// Clears this object to its blank/initial state.
        /// </summary>

        public void Clear()
        {

            AttributeName = null;
            AttributeSelectorType = 0;
            AttributeValue = null;
            ChildDepth = 0;
            Class = null;
            Criteria = null;
            Html = null;
            ID = null;
            NoIndex = false;
            PositionIndex = 0;
            SelectElements = null;
            Tag = null;

            Initialize();
        }

        /// <summary>
        /// Makes a deep copy of this Selector.
        /// </summary>
        ///
        /// <returns>
        /// A copy of this object.
        /// </returns>

        public SelectorClause Clone()
        {
            SelectorClause clone = new SelectorClause();

            clone.SelectorType = SelectorType;
            clone.TraversalType = TraversalType;
            clone.CombinatorType = CombinatorType;
         
            clone.AttributeName = AttributeName;
            clone.AttributeSelectorType = AttributeSelectorType;
            clone.AttributeValue = AttributeValue;
            clone.ChildDepth = ChildDepth;
            clone.Class = Class;
            clone.Criteria = Criteria;
            clone.Html = Html;
            clone.ID = ID;
            clone.NoIndex = NoIndex;
            clone.PositionIndex = PositionIndex;
            clone.SelectElements = SelectElements;
            clone.Tag = Tag;
            clone.PseudoSelector = PseudoSelector;

            return clone;
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        ///
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object" />.
        /// </returns>

        public override int GetHashCode()
        {
            return GetHash(SelectorType) + GetHash(TraversalType) + GetHash(CombinatorType) +
                GetHash(AttributeName) + GetHash(AttributeSelectorType) +
                GetHash(AttributeValue) + GetHash(Class) + GetHash(Criteria) + GetHash(Html) +
                GetHash(ID) + GetHash(NoIndex) + GetHash(PositionIndex) + GetHash(SelectElements) +
                GetHash(Tag);
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object" /> is equal to the current
        /// <see cref="T:System.Object" />.
        /// </summary>
        ///
        /// <param name="obj">
        /// The <see cref="T:System.Object" /> to compare with the current <see cref="T:System.Object" />.
        /// </param>
        ///
        /// <returns>
        /// true if the specified <see cref="T:System.Object" /> is equal to the current
        /// <see cref="T:System.Object" />; otherwise, false.
        /// </returns>

        public override bool Equals(object obj)
        {
            SelectorClause other = obj as SelectorClause;
            return other != null &&
                other.SelectorType == SelectorType &&
                other.TraversalType == TraversalType &&
                other.CombinatorType == CombinatorType &&
                other.AttributeName == AttributeName &&
                other.AttributeSelectorType == AttributeSelectorType &&
                other.AttributeValue == AttributeValue &&
                other.ChildDepth == ChildDepth &&
                other.Class == Class &&
                other.Criteria == Criteria &&
                other.Html == Html &&
                other.ID == ID &&
                other.NoIndex == NoIndex &&
                other.PositionIndex == PositionIndex &&
                other.SelectElements == SelectElements &&
                other.Tag == Tag;
        }

        /// <summary>
        /// Gets a hash.
        /// </summary>
        ///
        /// <param name="obj">
        /// The <see cref="T:System.Object" /> to compare with the current <see cref="T:System.Object" />.
        /// </param>
        ///
        /// <returns>
        /// The hash.
        /// </returns>

        private int GetHash(object obj) {

            return obj == null ? 0 : obj.GetHashCode();
        }

        /// <summary>
        /// Returns a string representation of the parsed selector. This may not exactly match the input
        /// selector as it is regenerated.
        /// </summary>
        ///
        /// <returns>
        /// A CSS selector string.
        /// </returns>

        public override string ToString()
        {
            string output = "";
            switch (TraversalType)
            {
                case TraversalType.Child:
                    output += " > ";
                    break;
                case TraversalType.Descendent:
                    output += " ";
                    break;
                case TraversalType.Adjacent:
                    output += " + ";
                    break;
                case TraversalType.Sibling :
                    output += " ~ ";
                    break;
            }

            if (SelectorType.HasFlag(SelectorType.Elements))
            {
                output += "<ElementList[" + SelectElements.Count() + "]> ";
            }
            if (SelectorType.HasFlag(SelectorType.HTML))
            {
                output += "<HTML[" + Html.Length + "]> ";
            }
            if (SelectorType.HasFlag(SelectorType.Tag))
            {
                output += Tag;
            }
            if (SelectorType.HasFlag(SelectorType.ID))
            {
                output += "#" + ID;
            }
            
            if (SelectorType.HasFlag(SelectorType.AttributeValue) 
                //|| SelectorType.HasFlag(SelectorType.AttributeExists)
                )
            {
                output += "[" + AttributeName;
                if (!String.IsNullOrEmpty(AttributeValue))
                {
                    output += "." + AttributeSelectorType.ToString() + ".'" + AttributeValue + "'";
                }
                output += "]";
            }
            if (SelectorType.HasFlag(SelectorType.Class))
            {
                output += "." + Class;
            }
            if (SelectorType.HasFlag(SelectorType.All))
            {
                output += "*";
            }
            if (SelectorType.HasFlag(SelectorType.PseudoClass))
            {
                output += ":" + PseudoSelector.Name;
                if (PseudoSelector.Arguments != null && PseudoSelector.Arguments.Length > 0)
                {
                    output += "("+String.Join(",",PseudoSelector.Arguments)+")";
                }
                
              
              
            }

            return output;
        }

        #endregion

        
    }
}
