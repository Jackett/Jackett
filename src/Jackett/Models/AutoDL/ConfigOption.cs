using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.AutoDL
{
    public class ConfigOption
    {
        public string Name { get; set; }
        public string Label { set; get; }
        public ConfigOptionType? Type { get; set; }
        public string Value { get; set; }
        public string DefaultValue { get; set; }
        public string EmptyText { get; set; }
        public string Tooltip { get; set; }
        public string PasteGroup { get; set; }
        public string PasteRegex { get; set; }
        public string MinValue { get; set; }
        public string MaxValue { get; set; }
        public bool IsDownloadVar { set; get; }
    }

    public enum ConfigOptionType { TextBox, Integer, Bool, Delta, Description  }
}
