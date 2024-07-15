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
        public string displayName { get; set; }
        public string description { get; set; }
        public string objectId { get; set; }
        public int status { get; set; }
        public string iconUrl { get; set; }
        public DateTime publishTime { get; set; }
        public int stage { get; set; }
        public string ownerGivenName { get; set; }
        public string ownerFamilyName { get; set; }
        public string ownerEmailAddress { get; set; }
        public int resourcePackageId { get; set; }
        public bool disabled { get; set; }
        public string name { get; set; }
        public bool isPublic { get; set; }
    }
}
