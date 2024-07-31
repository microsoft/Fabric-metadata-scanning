using CsvHelper;
using System.Globalization;

namespace Fabric_metadata_scanning
{
    public class OrgStore_handler
    {
        public List<OrgStoreVisual> GetOrgStoreContent(string orgStoreFile)
        {
            using (var reader = new StreamReader(orgStoreFile))
            using (var csv = new CsvReader(reader, new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                csv.Read();
                csv.ReadHeader();
                csv.Context.RegisterClassMap<OrgStoreVisualMap>();
                var records = csv.GetRecords<OrgStoreVisual>().ToList();
                return records;
            }
        }
    }
}
