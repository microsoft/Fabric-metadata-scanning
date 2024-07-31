using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Fabric_metadata_scanning
{




    public class OrgStoreVisual
    {
        public string guid { get; set; }
        public string disabled { get; set; }
        public string name { get; set; }
        public string source { get; set; }
        public string changed { get; set; }
        public string visualizationPane { get; set; }
    }
}
