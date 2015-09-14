using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.AutoDL
{
    public class SavedAutoDLConfigurations
    {
        public List<SavedAutoDLConfig> Configurations { set; get; } = new List<SavedAutoDLConfig>();
    }
}
