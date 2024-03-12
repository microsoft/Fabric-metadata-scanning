using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json.Nodes;

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
        public JObject datasources = new JObject();

        private ScanResultAPI_Handler() : base("scanResult")
        {
            baseOutputFolder = Configuration_Handler.Instance.getConfig(apiName, "baseOutputFolder").Value<string>();
            if (!Directory.Exists(baseOutputFolder))
            {
                Directory.CreateDirectory(baseOutputFolder);
            }

            resultStatusPath = Configuration_Handler.Instance.getConfig("scanResult", "resultsStatusFolder").Value<string>();
            if (!Directory.Exists(resultStatusPath))
            {
                Directory.CreateDirectory(resultStatusPath);
            }

            resultTime = Configuration_Handler.Instance.scanStartTime.ToString("yyyy-MM-dd-HHmmss");

            artifactsCounters = new Dictionary<string, int>()
            {
                { "workspaces" , 0 }
            };

            datasources["datasourceInstances"] = new JArray();
            datasources["misconfiguredDatasourceInstances"] = new JArray();
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

        public override async Task<object> run(string scanId = null)
        {
            HttpResponseMessage response = await sendGetRequest(scanId);

            string jsonResponse = await response.Content.ReadAsStringAsync();
            JObject resultObject = JObject.Parse(jsonResponse);

            
            if (resultObject["datasourceInstances"] != null )
            {
                ((JArray) datasources["datasourceInstances"]).Add(resultObject["datasourceInstances"]);
            }
            
            if (resultObject["misconfiguredDatasourceInstances"] != null )
            {
                ((JArray)datasources["misconfiguredDatasourceInstances"]).Add(resultObject["misconfiguredDatasourceInstances"]);
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

                    foreach (var property in ((JObject)workspace).Properties())
                    {
                        string propertyName = property.Name;
                        var propertyValue = property.Value;

                        if (propertyValue is JArray artifactsArray && artifactsArray.Count > 0)
                        {
                            if (artifactsCounters.ContainsKey(propertyName))
                            {
                                artifactsCounters[propertyName] += artifactsArray.Count;
                            }
                            else
                            {
                                artifactsCounters[propertyName] = artifactsArray.Count;
                            }
                        }
                    }

                    artifactsCounters["workspaces"] += 1;
                }
            }

            //finished
            if (workspacesArray.Count < Configuration_Handler.Instance.getConfig("getInfo", "chunkMaxSize").Value<int>())
            {
                lock (lockObject)
                {
                    JObject artifacts = new JObject
                    {
                        {"status", "Succeeded"},
                        {"Artifacts amounts",JObject.FromObject(this.artifactsCounters) }
                    };

                    string finalResultsDileDirPath = $"{resultStatusPath}\\{resultTime}";
                    Directory.CreateDirectory(finalResultsDileDirPath);

                    using (StreamWriter file = File.CreateText($"{finalResultsDileDirPath}\\artifacts.json"))
                    {
                        JsonSerializer serializer = new JsonSerializer
                        {
                            Formatting = Formatting.Indented
                        };

                        serializer.Serialize(file, artifacts);
                    }


                    using (StreamWriter file = File.CreateText($"{finalResultsDileDirPath}\\datasources.json"))
                    {
                        JsonSerializer serializer = new JsonSerializer
                        {
                            Formatting = Formatting.Indented
                        };

                        serializer.Serialize(file, datasources);
                    }

                    Console.WriteLine($"Scanning finished, The output is displayed in {Environment.CurrentDirectory + "\\" + finalResultsDileDirPath}");
                }
            }
            return true;
        }
    }
}
