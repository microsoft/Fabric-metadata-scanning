using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fabric_Metadata_Scanning
{
    public sealed class ScanResultAPI_Handler : API_Handler
    {
        private static ScanResultAPI_Handler instance = null;
        public string baseOutputFolder { get; set; }
        public string resultStatusPath { get; set; }
        public string resultTime { get; set; }

        private object lockObject = new object();
        public Dictionary<string, int> artifactsCounters { get; set; }
        public JObject sharedResult = new JObject();

        private ScanResultAPI_Handler() : base("scanResult")
        {
            baseOutputFolder = Configuration_Handler.Instance.getConfig(apiName, "baseOutputFolder").Value<string>();
            if (!Directory.Exists(baseOutputFolder))
            {
                Directory.CreateDirectory(baseOutputFolder);
            }

            DateTime currentTime = DateTime.Now;
            resultTime = currentTime.ToString("yyyy-MM-dd-HHmmss");
            resultStatusPath = Configuration_Handler.Instance.getConfig("scanResult", "resultsStatusFolder").Value<string>();
            if (!Directory.Exists(resultStatusPath))
            {
                Directory.CreateDirectory(resultStatusPath);
            }

            artifactsCounters = new Dictionary<string, int>()
            {
                {"workspaces",0 },
                {"reports",0 },
                {"dashboards",0 },
                {"datasets",0 },
                {"dataflows",0 },
                {"datamarts",0 }
            };
        }

        public static ScanResultAPI_Handler Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ScanResultAPI_Handler();
                }
                return instance;
            }
        }

        public override async Task<object> run(string? scanId)
        {
            HttpResponseMessage response = await sendGetRequest(scanId);

            string jsonResponse = await response.Content.ReadAsStringAsync();
            JObject resultObject = JObject.Parse(jsonResponse);


            if (resultObject["datasourceInstances"] != null && !sharedResult.ContainsKey("datasourceInstances"))
            {
                sharedResult["datasourceInstances"] = resultObject["datasourceInstances"];
            }

            if (resultObject["misconfiguredDatasourceInstances"] != null && !sharedResult.ContainsKey("misconfiguredDatasourceInstances"))
            {
                sharedResult["misconfiguredDatasourceInstances"] = resultObject["misconfiguredDatasourceInstances"];
            }

            JArray workspacesArray = (JArray)resultObject["workspaces"];

            // Now you can work with the "workspaces" array
            foreach (var workspace in workspacesArray)
            {
                string outputFolder = $"{baseOutputFolder}\\{workspace["id"]}";

                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }

                string outputFilePath = $"{outputFolder}\\{scanId}_{resultTime}.json";
                string workspaceJson = JsonConvert.SerializeObject(workspace, Formatting.Indented);
                using (StreamWriter stream = new StreamWriter(outputFilePath))
                {
                    try
                    {
                        stream.Write(workspaceJson);
                    }
                    catch { }
                    stream.Close();
                }
                
                lock(lockObject)
                {
                    artifactsCounters["workspaces"] += 1;
                    artifactsCounters["reports"] += ((JArray)workspace["reports"]).Count;
                    artifactsCounters["dashboards"] += ((JArray)workspace["dashboards"]).Count;
                    artifactsCounters["datasets"] += ((JArray)workspace["datasets"]).Count;
                    artifactsCounters["dataflows"] += ((JArray)workspace["dataflows"]).Count;
                    artifactsCounters["datamarts"] += ((JArray)workspace["datamarts"]).Count;
                   
                }
            }

            //finished
            if (workspacesArray.Count < Configuration_Handler.Instance.getConfig("getInfo", "chunkMaxSize").Value<int>())
            {

                lock (lockObject)
                {
                    JObject results = new JObject
                    {
                        {"status", "Succeeded"},
                        {"Artifacts amounts",JObject.FromObject(this.artifactsCounters) }
                    };

                    if (sharedResult["datasourceInstances"] != null)
                    {
                        results["datasourceInstances"] = sharedResult["datasourceInstances"];
                    }

                    if (sharedResult["misconfiguredDatasourceInstances"] != null)
                    {
                        results["misconfiguredDatasourceInstances"] = sharedResult["misconfiguredDatasourceInstances"];
                    }

                    using (StreamWriter file = File.CreateText($"{resultStatusPath}\\{resultTime}.json"))
                    {
                        JsonSerializer serializer = new JsonSerializer
                        {
                            Formatting = Formatting.Indented
                        };

                        serializer.Serialize(file, results);
                    }
                }
            }
            return true;
        }
    }
}
