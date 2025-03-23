using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudNinjaFunctions
{

    public class OpenPort
    {
        public int Port { get; set; }
        public string Protocol { get; set; }
        public string RuleName { get; set; }
        public string NsgName { get; set; }
    }
}
