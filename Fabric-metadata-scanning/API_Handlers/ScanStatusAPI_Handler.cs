using Newtonsoft.Json.Linq;

namespace Fabric_Metadata_Scanning
{
    public sealed class ScanStatusAPI_Handler : API_Handler
        {
            private static ScanStatusAPI_Handler instance = null;

            private ScanStatusAPI_Handler() : base("scanStatus") {}

            public static ScanStatusAPI_Handler Instance
            {
                get
                {
                    if (instance == null)
                    {
                        instance = new ScanStatusAPI_Handler();
                    }
                    return instance;
                }
            }

            public override async Task<object> run(string? scanId)
            {
                HttpResponseMessage response = await sendGetRequest(scanId);

                string jsonResponse = await response.Content.ReadAsStringAsync();
                JObject statusObject = JObject.Parse(jsonResponse);
                string statusValue = (string)statusObject["status"];

                return statusValue;
            }
    }
}
