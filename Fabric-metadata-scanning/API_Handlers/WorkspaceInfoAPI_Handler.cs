﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace Fabric_Metadata_Scanning
{
    public class WorkspaceInfoAPI_Handler : API_Handler
    {
        private int chunkMaxSize;
        private int nextIndexToCheck;
        private readonly object lockObject = new object();
        private string[] worspacesIds;

        public WorkspaceInfoAPI_Handler(string worspacesFilePath) : base("getInfo")
        {
            worspacesIds = File.ReadAllLines(worspacesFilePath);
            chunkMaxSize = Configuration_Handler.Instance.getConfig(apiName, "chunkMaxSize").Value<int>();
            nextIndexToCheck = 0;

            parameters.Add("datasetExpressions", Configuration_Handler.Instance.getConfig(apiName, "datasetExpressions").Value<bool>());
            parameters.Add("datasetSchema", Configuration_Handler.Instance.getConfig(apiName, "datasetSchema").Value<bool>());
            parameters.Add("datasourceDetails", Configuration_Handler.Instance.getConfig(apiName, "datasourceDetails").Value<bool>());
            parameters.Add("getArtifactUsers", Configuration_Handler.Instance.getConfig(apiName, "getArtifactUsers").Value<bool>());
            parameters.Add("lineage", Configuration_Handler.Instance.getConfig(apiName, "lineage").Value<bool>());
            parameters.Add("getTridentArtifacts", Configuration_Handler.Instance.getConfig(apiName, "getTridentArtifacts").Value<bool>());
            parameters.Add("reportObjects", Configuration_Handler.Instance.getConfig(apiName, "reportObjects").Value<bool>());

            setRequestParameters();
        }

        public override async Task<object> run(string scanId = null)
        {
            int start;
            int length;
            string[] workspacesToScan;
            lock (lockObject)
            {
                start = nextIndexToCheck;
                length =  Math.Min(start + chunkMaxSize, worspacesIds.Length)-start;
                nextIndexToCheck = start + length ;
                workspacesToScan = worspacesIds.Skip(start).Take(length).ToArray();
            }

            if (workspacesToScan.Length == 0)
            {
                return "Done";
            }

            using (HttpClient httpClient = new HttpClient())
            {
                var requestBody = new
                {
                    workspaces = workspacesToScan
                };

                string requestJsonString = JsonConvert.SerializeObject(requestBody);
                HttpContent content = new StringContent(requestJsonString, Encoding.UTF8, "application/json");

                setHeaders(httpClient);
                
                HttpResponseMessage response;
                try
                {
                    do
                    {
                        response = await httpClient.PostAsync(apiUriBuilder.Uri, content);
                    } while (!await verifySuccess(response));

                }
                catch (Exception ex)
                {
                    throw new ScanningException(apiName, "Can't send request");
                    
                }
                
                if (response.Content != null)
                {
                    var scanDetailsString = await response.Content.ReadAsStringAsync();

                    JObject scanDetails = JObject.Parse(scanDetailsString);
                    JToken token = scanDetails["id"];

                    scanId = token.Value<string>();

                    return scanId;

                }
                return null;
            }
        }
    }
}
