using Fabric_Metadata_Scanning;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Fabric_metadata_scanning
{
    public class OrgStore_handler
    {
        public async Task<List<OrgStoreVisual>> GetOrgStoreContentAsync(string orgStoreUri, string token)
        {
            //string catalogUri = Configuration_Handler.Instance.getConfig("orgVisuals", "uri").Value<string>();
            //string token = Configuration_Handler.Instance.getConfig("orgVisuals", "token").Value<string>();

            using (HttpClient httpClient = new HttpClient())
            {
                HttpResponseMessage response;
                // Set the authorization header with the access token, to identify the specific client 
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Used for telemetry purposes, DO NOT MODIFY.
                httpClient.DefaultRequestHeaders.Add("X-POWERBI-ADMIN-CLIENT-NAME", "FabricScanningClient");

                response = await httpClient.GetAsync(orgStoreUri);

                string jsonStr = await response.Content.ReadAsStringAsync();

                var visuals = JsonConvert.DeserializeObject<List<OrgStoreVisual>>(jsonStr);
                return visuals;
            }
        }
    }
}
