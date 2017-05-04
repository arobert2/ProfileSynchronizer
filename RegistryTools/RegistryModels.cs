using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace RegistryTools
{
    public class RegKeyData
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public List<RegKeyData> SubKeyData { get; set; }
        public List<RegValueData> KeyValues { get; set; }
    }
   
    public class RegValueData
    {
        public string Name { get; set; }
        public string RegType { get; set; }
        public string Value { get; set; }
    }
}
