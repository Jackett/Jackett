using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.StringScanner.ExtensionMethods;
using CsQuery.StringScanner.Implementation;

// TODO this should be fully commented; however it's not part of the main public API

#pragma warning disable 1591
#pragma warning disable 1570


namespace CsQuery.StringScanner.Patterns
{
    
    /// <summary>
    /// Matches anything that is bounded by accepted bounding characters
    /// </summary>
    public class Bounded: ExpectPattern
    {

        private string _BoundStart = "";
        private string _BoundEnd = "";
        private char _BoundStartChar = (char)0;
        private char _BoundEndChar = (char)0;

        private bool hasStartBound;
        protected bool boundAny = true;
        private bool quoting;
        private char quoteChar;
        private bool matched;
        // This is a one-character type with different open/close - must track nested entites
        private int nestedCount;

        
        public bool HonorInnerQuotes { get; set; }
        public string BoundStart
        {
            get
            {
                return _BoundStart;
            }
            set
            {
                boundAny = false;
                if (value.Length == 0)
                {
                    boundAny = true;
                }
                else
                {
                    BoundStartChar = value[0];
                }
                _BoundStart = value;
            }
        }
        public string BoundEnd
        {
            get
            {
                return _BoundEnd;
            }
            set
            {
                boundAny = false;
                _BoundEnd = value;
                _BoundEndChar = value[0];
            }
        }        
        protected char BoundStartChar
        {
            get
            {
                return _BoundStartChar;
            }
            set
            {
                _BoundStartChar = value;
                BoundEnd = CharacterData.MatchingBound(value).ToString();
            }
        }
        protected char BoundEndChar
        {
            get
            {
                return _BoundEndChar;
            }
            set
            {
                _BoundEndChar = value;
            }
        }
  
        public override void Initialize(int startIndex, char[] sourceText)
        {
            base.Initialize(startIndex, sourceText);
            hasStartBound = (BoundStartChar != (char)0) || boundAny;
            nestedCount = 0;
            matched = false;
            quoting = false;
        }
        public override bool Validate()
        {
            int index=StartIndex;
            while (index < Source.Length && Expect(ref index, Source[index]))
            {
                ;
            }
                      
            EndIndex = index;

            // should not have passed the end

            if (EndIndex > Length || EndIndex == StartIndex || !matched)
            {
                Result = "";
                return false;
            }
            // HonorQuotes parm is false no matter what because we don't want to process escape characters for this method -only for
            // the actual "Quoted" method
            Result = GetOuput(StartIndex + BoundStart.Length, EndIndex - BoundEnd.Length, false);
            return true;

        }
        protected bool Expect(ref int index, char current)
        {
            info.Target = current;
            
            if (!quoting) {
                // only try to match bounds when not inside quotes
                if (hasStartBound)
                {
                    // this doesn't matter if there's no  tart bound
                    if (index == StartIndex)
                    {
                        if (boundAny && info.Bound)
                        {
                            BoundStart = current.ToString();
                            BoundEnd = CharacterData.Closer(current).ToString();

                            // will not increment if return false (after short-circuit) - causing validation failure
                            // when index==Start
                            return info.Bound && index++ < Length;
                        }
                        else if (MatchSubstring(index, BoundStart))
                        {
                            index += BoundStart.Length;
                            return true;
                        }
                    }
                    else if (current == BoundStartChar)
                    {
                        if (MatchSubstring(index, BoundStart))
                        {
                            index += BoundStart.Length;
                            nestedCount++;
                            return true;
                        }
                    }
                }

                if (current == BoundEndChar )
                {
                    if (boundAny)
                    {
                        if (nestedCount==0) {
                            matched= true;
                            index++;
                            return false;
                        } else {
                            nestedCount--;
                        }
                    } 
                    else if (MatchSubstring(index, BoundEnd))
                    {
                        if (nestedCount==0) {
                            index += BoundEnd.Length;
                            matched = true;
                            return false;
                        } else {
                            nestedCount--;
                        }
                    }
                }
            }
            
            
            // Now the regular part
            if (HonorInnerQuotes)
            {
                if (!quoting)
                {
                    if (info.Quote)
                    {
                        quoting = true;
                        quoteChar = current;
                    }
                }
                else
                {
                    bool isEscaped = Source[index - 1] == '\\';
                    if (current == quoteChar && !isEscaped)
                    {
                        quoting = false;
                    }
                }
            }
            index++;
            return true;
            
        }


    }
}
