using Fabric_metadata_scanning;
using Microsoft.PowerBI.Api.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json.Nodes;
using static System.Net.Mime.MediaTypeNames;

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
        private Dictionary<string, bool> coreVisuals;
        private JObject datasources;
        private HashSet<dynamic> datasourceInstancesSet;
        private HashSet<dynamic> misconfiguredDatasourceInstancesSet;
        private List<MarketplaceProduct> marketplaceProducts;
        private List<OrgStoreVisual> orgStoreVisuals;

        public string? catalogUri { get; private set; }
        public bool enrichVisuals { get; private set; }

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

            catalogUri = Configuration_Handler.Instance.getConfig("catalogAccess", "uri").Value<string>();
            enrichVisuals = Configuration_Handler.Instance.getConfig("catalogAccess", "enrichVisuals").Value<bool>();

            if (catalogUri != null && enrichVisuals)
            {
                var marketplaceHandler = new PowerBIMarketplaceApp_handler();
                marketplaceProducts = marketplaceHandler.GetMarketplaceAppsAsync(catalogUri).Result;
            }

            coreVisuals = new Dictionary<string, bool>
            {
                { "actionButton", true},
                { "animatedNumber", true},
                { "areaChart", true},
                { "barChart", true},
                { "basicShape", true},
                { "shape", true},
                { "card", true},
                { "cardVisual", true},
                { "multiRowCard", true},
                { "clusteredBarChart", true},
                { "clusteredColumnChart", true},
                { "columnChart", true},
                { "donutChart", true},
                { "funnel", true},
                { "gauge", true},
                { "hundredPercentStackedBarChart", true},
                { "hundredPercentStackedColumnChart", true},
                { "image", true},
                { "lineChart", true},
                { "lineStackedColumnComboChart", true},
                { "lineClusteredColumnComboChart", true},
                { "map", true},
                { "filledMap", true},
                { "azureMap", true},
                { "ribbonChart", true},
                { "shapeMap", true},
                { "treemap", true},
                { "pieChart", true},
                { "realTimeLineChart", true},
                { "scatterChart", true},
                { "stackedAreaChart", true},
                { "table", true},
                { "matrix", true},
                { "tableEx", true},
                { "pivotTable", true},
                { "accessibleTable", true},
                { "slicer", true},
                { "advancedSlicerVisual", true},
                { "pageNavigator", true},
                { "bookmarkNavigator", true},
                { "filterSlicer", true},
                { "textbox", true},
                { "aiNarratives", true},
                { "waterfallChart", true},
                { "scriptVisual", true},
                { "pythonVisual", true},
                { "kpi", true},
                { "keyDriversVisual", true},
                { "decompositionTreeVisual", true},
                { "qnaVisual", true},
                { "scorecard", true},
                { "rdlVisual", true},
                { "dataQueryVisual", true},
                { "debugVisual", true},
                { "heatMap", true},            
            };

            string orgStoreUri = Configuration_Handler.Instance.getConfig("orgVisuals", "uri").Value<string>();
            string token = Configuration_Handler.Instance.getConfig("orgVisuals", "token").Value<string>();

            //orgStoreVisuals =
            var orgStore_handler = new OrgStore_handler();
            orgStoreVisuals = orgStore_handler.GetOrgStoreContentAsync(orgStoreUri, token).Result;

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
                string outputFolderForRepWithCV = $"{baseOutputFolder}\\RepWithCV\\{workspace["id"]}";


                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }

                var workspaceHaveReportsWithAppSourceCV = false;
                var workspaceHaveReportsWithOrgCV = false;
                var workspaceHaveReportsWithPrivateCV = false;

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

                            if (enrichVisuals && propertyName.Contains("reports"))
                            {
                                foreach (var report in artifactsArray)
                                {
                                    var reportSections = report["sections"];
                                    if (reportSections != null)
                                    {
                                        if (reportSections is JArray sectionArray && sectionArray.Count > 0)
                                        {
                                            foreach (var section in sectionArray)
                                            {
                                                var sectionVisuals = section["visuals"];
                                                if (sectionVisuals != null && sectionVisuals is JArray visualArray && visualArray.Count > 0)
                                                {
                                                    foreach (var visual in visualArray)
                                                    {
                                                        var visualId = visual["visualGuid"];
                                                        if (visualId != null)
                                                        {
                                                            var visualGUID = visualId.Value<string>();
                                                            var orgStoreVisual = orgStoreVisuals.FirstOrDefault(v => v.name == visualGUID);
                                                            if (orgStoreVisual != null)
                                                            {
                                                                workspaceHaveReportsWithOrgCV = true;
                                                                visual["isOrgStore"] = true;
                                                                visual["originalVisualGUID"] = visualGUID.Replace("_OrgStore", "");
                                                                visual["name"] = orgStoreVisual.displayName;
                                                            }
                                                            else
                                                            {
                                                                visual["isOrgStore"] = false;
                                                            }
 
                                                            var marketplaceProduct = marketplaceProducts.FirstOrDefault(p => p.PowerBIVisualId == (visual["isOrgStore"].Value<bool>() ? visual["originalVisualGUID"].Value<string>() : visualGUID));

                                                            if (marketplaceProduct != null)
                                                            {
                                                                workspaceHaveReportsWithAppSourceCV = true;
                                                                if (orgStoreVisual == null)
                                                                {
                                                                    visual["name"] = marketplaceProduct.DisplayName;
                                                                }
                                                                visual["publisher"] = marketplaceProduct.PublisherDisplayName;
                                                                visual["isCoreVisual"] = false;
                                                                visual["isAppSource"] = true;
                                                                visual["IsPrivate"] = false;
                                                            }
                                                            else if (coreVisuals.ContainsKey(visualGUID))
                                                            {
                                                                visual["name"] = visualId.Value<string>();
                                                                visual["publisher"] = "Microsoft";
                                                                visual["isCoreVisual"] = true;
                                                                visual["isAppSource"] = false;
                                                                visual["IsPrivate"] = false;
                                                            }
                                                            else
                                                            {
                                                                workspaceHaveReportsWithPrivateCV = true;
                                                                if (orgStoreVisual == null)
                                                                {
                                                                    visual["name"] = visualId.Value<string>();
                                                                }
                                                                visual["isCoreVisual"] = false;
                                                                visual["isAppSource"] = false;
                                                                visual["IsPrivate"] = true;
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    artifactsCounters["workspaces"] += 1;
                }

                string outputFilePath = $"{outputFolder}\\{scanId}_{resultTime}.json";
                string outputFilePathForWsWithCV = $"{outputFolderForRepWithCV}\\{scanId}_{resultTime}.json";
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
                if (workspaceHaveReportsWithAppSourceCV && workspaceHaveReportsWithOrgCV && workspaceHaveReportsWithPrivateCV)
                {
                    if (!Directory.Exists(outputFolderForRepWithCV))
                    {
                        Directory.CreateDirectory(outputFolderForRepWithCV);
                    }

                    using (StreamWriter stream = new StreamWriter(outputFilePathForWsWithCV))
                    {
                        try
                        {
                            stream.Write(workspaceJson);
                        }
                        catch { }
                        stream.Close();
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

                    Console.WriteLine($"Scanning finished, The output is displayed in {Environment.CurrentDirectory + "\\" + finalResultsDileDirPath}");
                }
            }
            return true;
        }
    }
}
