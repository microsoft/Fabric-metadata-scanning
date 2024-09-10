using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fabric_metadata_scanning
{


    public class Visuals_Handler
    {
        private const string c_reportWithOrgCV = "reportsWithOrgCV";
        private const string c_reportsWithAppSourceCV = "reportsWithAppSourceCV";
        private const string c_reportsWithPrivateCV = "reportsWithPrivateCV";
        private const string c_visuals = "visuals";

        private const string c_isOrgStoreVisual = "isOrgStore";
        private const string c_isAppSourceVisual = "isAppSource";
        private const string c_isCoreVisual = "isCoreVisual";
        private const string c_isPrivateVisual = "isPrivate";
        private const string c_isCertifiedVisual = "isCertified";
        private const string c_visualName = "name";
        private const string c_visualGuid = "visualGuid";
        private const string c_publisher = "publisher";
        private const string c_reports = "reports";
        private PowerBIMarketplaceApp_handler appSourceHandler = new();
        private OrgStore_handler orgStoreHandler = new();

        private Dictionary<string, bool> coreVisuals;
        private List<MarketplaceProduct> marketplaceProducts;
        private List<OrgStoreVisual> orgStoreVisuals;

        private JObject globalVisualStatistic = new();

        private void LoadCoreVisuals()
        {
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
        }

        private void LoadAppSourceVisuals(string marketplaceApiUrl)
        {
            marketplaceProducts = appSourceHandler.GetMarketplaceAppsAsync(marketplaceApiUrl).Result;    
        }

        private void LoadOrgVisuals(string orgStoreFile)
        {
            orgStoreVisuals = orgStoreHandler.GetOrgStoreContent(orgStoreFile);
        }

        public void Initialize(string marketplaceApiUrl, string orgStoreFile)
        {
            LoadCoreVisuals();
            LoadAppSourceVisuals(marketplaceApiUrl);
            LoadOrgVisuals(orgStoreFile);
            globalVisualStatistic[c_reportWithOrgCV] = new JArray();
            globalVisualStatistic[c_reportsWithAppSourceCV] = new JArray();
            globalVisualStatistic[c_reportsWithPrivateCV] = new JArray();
            globalVisualStatistic[c_visuals] = new JArray();
        }

        public JObject processVisuals(JArray reportsArray)
        {

            List<string> reportsWithOrgCV = new();
            List<string> reportsWithAppSourceCV = new();
            List<string> reportsWithPrivateCV = new();

            Dictionary<string, string> reportsWithOrgCVDict = new();
            Dictionary<string, string> reportsWithAppSourceCVDict = new();
            Dictionary<string, string> reportsWithPrivateCVDict = new();


            Dictionary<string, Dictionary<string, string>> visualsToReportsDict = new(); 
            Dictionary<string, JObject> visualsDict = new();
            JObject visualsStatistic = new JObject();

            foreach (var report in reportsArray)
            {
                var reportId = report["id"]?.Value<string>();
                reportId ??= "00000000000000000000000000000000000000000";
                var reportName = report["name"]?.Value<string>();
                reportName ??= "Empty report name";
                var reportSections = report["sections"];
                if (reportSections != null)
                {
                    if (reportSections is JArray sectionArray && sectionArray.Count > 0)
                    {
                        foreach (var section in sectionArray)
                        {
                            var sectionVisuals = section[c_visuals];
                            if (sectionVisuals != null && sectionVisuals is JArray visualArray && visualArray.Count > 0)
                            {
                                foreach (var visual in visualArray)
                                {
                                    var visualId = visual[c_visualGuid];
                                    if (visualId != null)
                                    {
                                        var visualGUID = visualId.Value<string>();
                                        if (visualsDict.ContainsKey(visualGUID))
                                        {
                                            var visualObj = (JObject)visual;
                                            visualObj.Merge(visualsDict[visualGUID]);
                                            updateReportsCounters(reportsWithOrgCVDict, reportsWithAppSourceCVDict, reportsWithPrivateCVDict, reportId, reportName, visual, visualsToReportsDict);
                                            continue;
                                        }
                                        visualsDict[visualGUID] = (JObject)visual;
                                        if (coreVisuals.ContainsKey(visualGUID))
                                        {
                                            // Core visual, no more information
                                            visual[c_visualName] = visualId.Value<string>();
                                            visual[c_publisher] = "Microsoft";
                                            visual[c_isCoreVisual] = true;
                                            continue;
                                        }

                                        visual[c_isOrgStoreVisual] = false;
                                        visual[c_isAppSourceVisual] = false;
                                        visual[c_isPrivateVisual] = false;
                                        visual[c_isCertifiedVisual] = false;

                                        var orgStoreVisual = orgStoreVisuals.FirstOrDefault(v => v.guid == visualGUID);
                                        if (orgStoreVisual != null)
                                        {
                                            visual[c_isOrgStoreVisual] = true;
                                            if (orgStoreVisual.source == "AppSource")
                                            {
                                                visual[c_isAppSourceVisual] = true;
                                            }
                                            else
                                            {
                                                visual["originalVisualGUID"] = visualGUID.Replace("_OrgStore", "");
                                                visual[c_isPrivateVisual] = true;

                                            }
                                            visual[c_visualName] = orgStoreVisual.name;
                                        }

                                        var marketplaceProduct = marketplaceProducts.FirstOrDefault(p => p.PowerBIVisualId == visualGUID);

                                        if (marketplaceProduct != null)
                                        {
                                            visual[c_publisher] = marketplaceProduct.PublisherDisplayName;
                                            if (orgStoreVisual == null)
                                            {
                                                visual[c_visualName] = marketplaceProduct.DisplayName;
                                                visual[c_isCertifiedVisual] = (null != marketplaceProduct.EnrichedData.Tags) && marketplaceProduct.EnrichedData.Tags.Contains(MarketplaceProduct.c_powerBICertfied);
                                            }
                                            else
                                            {
                                            }
                                            visual["appSourceLink"] = "https://appsource.microsoft.com/" + getLocale() + "/product/PowerBIVisuals/" + marketplaceProduct.LegacyId;
                                            visual[c_isAppSourceVisual] = true;
                                        }
                                        else
                                        {
                                            if (orgStoreVisual == null)
                                            {
                                                visual[c_visualName] = visualId.Value<string>();
                                            }
                                            visual[c_isPrivateVisual] = true;
                                        }
                                        updateReportsCounters(reportsWithOrgCVDict, reportsWithAppSourceCVDict, reportsWithPrivateCVDict, reportId, reportName, visual, visualsToReportsDict);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            visualsStatistic[c_reportWithOrgCV] = JArray.FromObject(reportsWithOrgCVDict.Select(kvp => new
            {
                Id = kvp.Key,
                Name = kvp.Value
            }).ToList());
            visualsStatistic[c_reportsWithAppSourceCV] = JArray.FromObject(reportsWithAppSourceCVDict.Select(kvp => new
            {
                Id = kvp.Key,
                Name = kvp.Value
            }).ToList());
            visualsStatistic[c_reportsWithPrivateCV] = JArray.FromObject(reportsWithPrivateCVDict.Select(kvp => new
            {
                Id = kvp.Key,
                Name = kvp.Value
            }).ToList());
            visualsStatistic[c_visuals] = JArray.FromObject(visualsDict.Select(kvp =>
            {
                var cpVis = new JObject(kvp.Value);
                if (cpVis[c_isCoreVisual] == null || !cpVis[c_isCoreVisual].Value<bool>())
                {
                    var reports = visualsToReportsDict[kvp.Key].Select(kvp => new
                    {
                        Id = kvp.Key,
                        Name = kvp.Value
                    }).ToList();
                    cpVis[c_reports] = JArray.FromObject(reports);
                }
                return cpVis;
            }
            ).ToList());

            mergeVisualsStatistic(visualsStatistic);

            return visualsStatistic;
        }

        private void mergeVisualsStatistic(JObject visualsStatistic)
        {
            if (globalVisualStatistic[c_reportWithOrgCV] != null && globalVisualStatistic[c_reportWithOrgCV] is JArray gReportsWithOrgCV 
                && visualsStatistic[c_reportWithOrgCV] != null && visualsStatistic[c_reportWithOrgCV] is JArray reportsWithOrgCV)
            {
                globalVisualStatistic[c_reportWithOrgCV] = ConcatenateJArrays(gReportsWithOrgCV, reportsWithOrgCV);
            }
            if (globalVisualStatistic[c_reportsWithAppSourceCV] != null && globalVisualStatistic[c_reportsWithAppSourceCV] is JArray gReportsWithAppSourceCV 
                && visualsStatistic[c_reportsWithAppSourceCV] != null && visualsStatistic[c_reportsWithAppSourceCV] is JArray reportsWithAppSourceCV)
            {
                globalVisualStatistic[c_reportsWithAppSourceCV] = ConcatenateJArrays(gReportsWithAppSourceCV, reportsWithAppSourceCV);
            }
            if (globalVisualStatistic[c_reportsWithPrivateCV] != null && globalVisualStatistic[c_reportsWithPrivateCV] is JArray gRreportsWithPrivateCV 
                && visualsStatistic[c_reportsWithPrivateCV] != null && visualsStatistic[c_reportsWithPrivateCV] is JArray reportsWithPrivateCV)
            {
                globalVisualStatistic[c_reportsWithPrivateCV] = ConcatenateJArrays(gRreportsWithPrivateCV, reportsWithPrivateCV);
            }

            if (globalVisualStatistic[c_visuals] != null && globalVisualStatistic[c_visuals] is JArray gVisuals && visualsStatistic[c_visuals] != null && visualsStatistic[c_visuals] is JArray visuals)
            {
                Dictionary<string, JToken> visualsDict = new();
                foreach (var visual in gVisuals)
                {
                    var visualGuid = visual[c_visualGuid];
                    if (visualGuid != null && visualGuid.Value<string>() != null)
                    {
                        visualsDict[visualGuid.Value<string>()] = visual;
                    }
                }
                foreach (var visual in visuals)
                {
                    var visualGuid = visual[c_visualGuid];
                    if (visualGuid != null && visualGuid.Value<string>() != null)
                    {
                        if (!visualsDict.ContainsKey(visualGuid.Value<string>()))
                        {
                            var cpVis = new JObject((JObject)visual);
                            cpVis.Remove(c_reports); 
                            visualsDict[visualGuid.Value<string>()] = cpVis;
                            gVisuals.Add(cpVis);
                        }
                    }
                }
            }
        }

        public JObject globalStatistic()
        {
            return globalVisualStatistic;
        }

        private void updateReportsCounters(Dictionary<string, string> reportsWithOrgCV, Dictionary<string, string> reportsWithAppSourceCV, Dictionary<string, string> reportsWithPrivateCV, string reportId, string reportName, JToken? visual, Dictionary<string, Dictionary<string, string>> visualsToReportsDict)
        {
            if ((visual[c_isOrgStoreVisual] != null && visual[c_isOrgStoreVisual].Type == JTokenType.Boolean) ? (bool)visual[c_isOrgStoreVisual] : false)
            {
                if (!reportsWithOrgCV.ContainsKey(reportId))
                {
                    reportsWithOrgCV[reportId] = reportName;
                }
            }
            if ((visual[c_isAppSourceVisual] != null && visual[c_isAppSourceVisual].Type == JTokenType.Boolean) ? (bool)visual[c_isAppSourceVisual] : false)
            {
                if (!reportsWithAppSourceCV.ContainsKey(reportId))
                {
                    reportsWithAppSourceCV[reportId] = reportName;
                }
            }
            if ((visual[c_isPrivateVisual] != null && visual[c_isPrivateVisual].Type == JTokenType.Boolean) ? (bool)visual[c_isPrivateVisual] : false)
            {
                if (!reportsWithPrivateCV.ContainsKey(reportId))
                {
                    reportsWithPrivateCV[reportId] = reportName;
                }
            }

            var visualGuid = visual[c_visualGuid].Value<string>();
            if (!visualsToReportsDict.ContainsKey(visualGuid))
            {
                visualsToReportsDict[visualGuid] = new();
            }
            visualsToReportsDict[visualGuid][reportId] = reportName;
        }
        private string getLocale()
        {
            CultureInfo culture = CultureInfo.CurrentCulture;
            return culture.Name;
        }

        private static JArray ConcatenateJArrays(JArray array1, JArray array2)
        {
            JArray concatenatedArray = new JArray(array1);
            foreach (var item in array2)
            {
                concatenatedArray.Add(item);
            }

            return concatenatedArray;
        }

        internal bool hasCustomVisuals(JObject visualsStatistic)
        {
            if (visualsStatistic[c_reportWithOrgCV] != null && visualsStatistic[c_reportWithOrgCV] is JArray reportsWithOrgCV)
            {
                return reportsWithOrgCV.Count() > 0;
            }
            if (visualsStatistic[c_reportsWithAppSourceCV] != null && visualsStatistic[c_reportsWithAppSourceCV] is JArray reportsWithAppSourceCV)
            {
                return reportsWithAppSourceCV.Count() > 0;
            }
            if (visualsStatistic[c_reportsWithPrivateCV] != null && visualsStatistic[c_reportsWithPrivateCV] is JArray reportsWithPrivateCV)
            {
                return reportsWithPrivateCV.Count() > 0;
            }

            return false;
        }
    }

    public class Report
    {
        public string Name { get; set; }
        public string Id { get; set; }
    }

}
