using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudNinjaFunctions
{
    public class ExposedEndpoint
    {
        public string ResourceId { get; set; }
        public string ResourceName { get; set; }
        public string ResourceGroup { get; set; }
        public string IpAddress { get; set; }
        public List<OpenPort> OpenPorts { get; set; }
        public DateTime ScanTime { get; set; }
        public string NinjaReport { get; set; }
        public string Severity { get; set; }
    }
}
