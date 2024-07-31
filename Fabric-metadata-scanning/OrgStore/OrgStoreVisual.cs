using CsvHelper.Configuration;
using Microsoft.PowerBI.Api.Models;

namespace Fabric_metadata_scanning
{
    public class OrgStoreVisual
    {
        public string guid { get; set; }
        public string disabled { get; set; }
        public string name { get; set; }
        public string source { get; set; }
        public string changed { get; set; }
    }
    public class OrgStoreVisualMap : ClassMap<OrgStoreVisual>
    {
        public OrgStoreVisualMap()
        {
            Map(m => m.guid).Index(0); // First column
            Map(m => m.name).Index(1); // Second column
            Map(m => m.source).Index(2); // Empty header column
            Map(m => m.changed).Index(3); // Fourth column
            Map(m => m.disabled).Index(4); // Fourth column
        }
    }

}
