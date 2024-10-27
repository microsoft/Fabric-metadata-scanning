using Fabric_metadata_scanning;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace Fabric_Metadata_Scanning
{
    public sealed class ScanResultAPI_Handler : API_Handler
    {
        private static ScanResultAPI_Handler instance = null;
        private string baseOutputFolder { get; set; }
        private string resultStatusPath { get; set; }
        private string resultTime { get; set; }

        private object lockObject = new object();
        private Dictionary<string, int> artifactsCounters { get; set; }
        private Dictionary<string, bool> workspacesWithReports { get; set; }
        private JObject datasources;
        private HashSet<dynamic> datasourceInstancesSet;
        private HashSet<dynamic> misconfiguredDatasourceInstancesSet;

        private Visuals_Handler visualsHandler = new();

        public bool processVisuals { get; private set; }
        public bool onlyWorkspaceWithVisuals { get; private set; }

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

            datasources = new JObject();
            datasourceInstancesSet = new HashSet<dynamic>();
            misconfiguredDatasourceInstancesSet = new HashSet<dynamic>();

            processVisuals = Configuration_Handler.Instance.getConfig("visuals", "enrichVisuals").Value<bool>();
            onlyWorkspaceWithVisuals = Configuration_Handler.Instance.getConfig("visuals", "onlyWorkspaceWithVisuals").Value<bool>();
            if (processVisuals)
            {
                string catalogUri = Configuration_Handler.Instance.getConfig("visuals", "catalogUri").Value<string>();
                string orgStoreFile = Configuration_Handler.Instance.getConfig("visuals", "orgVisualsFileLocation").Value<string>();
                visualsHandler.Initialize(catalogUri, orgStoreFile);
            }
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
                datasourceInstancesSet.UnionWith(resultObject["datasourceInstances"]);
            }
            
            if (resultObject["misconfiguredDatasourceInstances"] != null )
            {
                misconfiguredDatasourceInstancesSet.UnionWith(resultObject["misconfiguredDatasourceInstances"]);
            }

            JArray workspacesArray = (JArray)resultObject["workspaces"];

            // Now you can work with the "workspaces" array
            foreach (var workspace in workspacesArray)
            {
                string outputFolder = $"{baseOutputFolder}\\{workspace["id"]}";
                JObject visualStatistic = new();
                bool createWSOutput = false;

                lock (lockObject)
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

                            if (processVisuals && propertyName.Contains("reports"))
                            {
                                visualStatistic = visualsHandler.processVisuals(artifactsArray);
                            }
                        }
                    }
                    artifactsCounters["workspaces"] += 1;

                    createWSOutput = !processVisuals || !onlyWorkspaceWithVisuals || visualsHandler.hasCustomVisuals(visualStatistic);

                    if (createWSOutput && !Directory.Exists(outputFolder))
                    {
                        Directory.CreateDirectory(outputFolder);
                    }
                }


                if (createWSOutput)
                {
                    string outputFilePath = $"{outputFolder}\\{scanId}_{resultTime}.json";
                    string visualStatisticFilePath = $"{outputFolder}\\visualStatistics_{resultTime}.json";
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
                    if (processVisuals)
                    {
                        string statisticJson = JsonConvert.SerializeObject(visualStatistic, Formatting.Indented);
                        using (StreamWriter stream = new StreamWriter(visualStatisticFilePath))
                        {
                            try
                            {
                                stream.Write(statisticJson);
                            }
                            catch { }
                            stream.Close();
                        }
                    }
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

                    datasources["datasourceInstances"] = JArray.FromObject(datasourceInstancesSet);
                    datasources["misconfiguredDatasourceInstances"] = JArray.FromObject(misconfiguredDatasourceInstancesSet);

                    using (StreamWriter file = File.CreateText($"{finalResultsDileDirPath}\\datasources.json"))
                    {
                        JsonSerializer serializer = new JsonSerializer
                        {
                            Formatting = Formatting.Indented
                        };

                        serializer.Serialize(file, datasources);
                    }

                    if (processVisuals)
                    {
                        using (StreamWriter file = File.CreateText($"{finalResultsDileDirPath}\\visuals.json"))
                        {
                            JsonSerializer serializer = new JsonSerializer
                            {
                                Formatting = Formatting.Indented
                            };

                            serializer.Serialize(file, visualsHandler.globalStatistic());
                        }
                    }

                    Console.WriteLine($"Scanning finished, The output is displayed in {Environment.CurrentDirectory + "\\" + finalResultsDileDirPath}");
                }
            }
            return true;
        }

    }
}
