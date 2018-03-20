using System;
using System.Collections.Generic;

namespace Jackett.Common.Models
{
    // A Dictionary allowing the same key multiple times
    public class KeyValuePairList : List<KeyValuePair<string, selectorBlock>>, IDictionary<string, selectorBlock>
    {
        public selectorBlock this[string key]
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                base.Add(new KeyValuePair<string, selectorBlock>(key, value));
            }
        }

        public ICollection<string> Keys
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public ICollection<selectorBlock> Values
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void Add(string key, selectorBlock value)
        {
            base.Add(new KeyValuePair<string, selectorBlock>(key, value));
        }

        public bool ContainsKey(string key)
        {
            throw new NotImplementedException();
        }

        public bool Remove(string key)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(string key, out selectorBlock value)
        {
            throw new NotImplementedException();
        }
    }

    // Cardigann yaml classes
    public class IndexerDefinition
    {
        public string Site { get; set; }
        public List<settingsField> Settings { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string Language { get; set; }
        public string Encoding { get; set; }
        public List<string> Links { get; set; }
        public List<string> Legacylinks { get; set; }
        public List<string> Certificates { get; set; }
        public capabilitiesBlock Caps { get; set; }
        public loginBlock Login { get; set; }
        public ratioBlock Ratio { get; set; }
        public searchBlock Search { get; set; }
        public downloadBlock Download { get; set; }
        // IndexerDefinitionStats not needed/implemented
    }
    public class settingsField
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Label { get; set; }
        public string Default { get; set; }
        public Dictionary<string, string> Options { get; set; }
    }

    public class CategorymappingBlock
    {
        public string id { get; set; }
        public string cat { get; set; }
        public string desc { get; set; }
    }

    public class capabilitiesBlock
    {
        public Dictionary<string, string> Categories { get; set; }
        public List<CategorymappingBlock> Categorymappings { get; set; }
        public Dictionary<string, List<string>> Modes { get; set; }
    }

    public class captchaBlock
    {
        public string Type { get; set; }
        public string Selector { get; set; }
        public string Image { get { throw new Exception("Deprecated, please use Login.Captcha.Selector instead"); } set { throw new Exception("Deprecated, please use login/captcha/selector instead of image"); } }
        public string Input { get; set; }
    }

    public class loginBlock
    {
        public string Path { get; set; }
        public string Submitpath { get; set; }
        public List<string> Cookies { get; set; }
        public string Method { get; set; }
        public string Form { get; set; }
        public bool Selectors { get; set; } = false;
        public Dictionary<string, string> Inputs { get; set; }
        public Dictionary<string, selectorBlock> Selectorinputs { get; set; }
        public Dictionary<string, selectorBlock> Getselectorinputs { get; set; }
        public List<errorBlock> Error { get; set; }
        public pageTestBlock Test { get; set; }
        public captchaBlock Captcha { get; set; }
    }

    public class errorBlock
    {
        public string Path { get; set; }
        public string Selector { get; set; }
        public selectorBlock Message { get; set; }
    }

    public class selectorBlock
    {
        public string Selector { get; set; }
        public bool Optional { get; set; } = false;
        public string Text { get; set; }
        public string Attribute { get; set; }
        public string Remove { get; set; }
        public List<filterBlock> Filters { get; set; }
        public Dictionary<string, string> Case { get; set; }
    }

    public class filterBlock
    {
        public string Name { get; set; }
        public dynamic Args { get; set; }
    }

    public class pageTestBlock
    {
        public string Path { get; set; }
        public string Selector { get; set; }
    }

    public class ratioBlock : selectorBlock
    {
        public string Path { get; set; }
    }

    public class searchBlock
    {
        public string Path { get; set; }
        public List<searchPathBlock> Paths { get; set; }
        public Dictionary<string, List<string>> Headers { get; set; }
        public List<filterBlock> Keywordsfilters { get; set; }
        public Dictionary<string, string> Inputs { get; set; }
        public List<errorBlock> Error { get; set; }
        public List<filterBlock> Preprocessingfilters { get; set; }
        public rowsBlock Rows { get; set; }
        public KeyValuePairList Fields { get; set; }
    }

    public class rowsBlock : selectorBlock
    {
        public int After { get; set; }
        //public string Remove { get; set; } // already inherited
        public selectorBlock Dateheaders { get; set; }
    }

    public class searchPathBlock : requestBlock
    {
        public List<string> Categories { get; set; }
        public bool Inheritinputs { get; set; } = true;
    }

    public class requestBlock
    {
        public string Path { get; set; }
        public string Method { get; set; }
        public Dictionary<string, string> Inputs { get; set; }
    }

    public class downloadBlock
    {
        public string Selector { get; set; }
        public List<filterBlock> Filters { get; set; }
        public string Method { get; set; }
        public requestBlock Before { get; set; }
    }
}
