using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fabric_Metadata_Scanning
{
    public class ModifiedAPI_Handler : API_Handler
    {
        public string outputFolder; 

        public ModifiedAPI_Handler() : base("modified")
        {
            bool excludeInActiveWorkspaces = Configuration_Handler.Instance.getConfig(apiName, "excludeInActiveWorkspaces").Value<bool>();
            bool excludePersonalWorkspaces = Configuration_Handler.Instance.getConfig(apiName, "excludePersonalWorkspaces").Value<bool>();

            parameters.Add("excludeInActiveWorkspaces", excludeInActiveWorkspaces);
            parameters.Add("excludePersonalWorkspaces", excludePersonalWorkspaces);

            string modifiedSince = Configuration_Handler.Instance.getConfig(apiName, "modifiedSince").Value<string>();

            bool alwaysFullScan = Configuration_Handler.Instance.getConfig(apiName, "alwaysFullScan").Value<bool>();
            if (!(Equals(modifiedSince,"") || alwaysFullScan ))
            {
                modifiedSince = DateTime.Parse(modifiedSince).ToString("O");
                parameters.Add("modifiedSince",modifiedSince);
            }
            
            setRequestParameters();

            outputFolder = Configuration_Handler.Instance.getConfig(apiName, "outputFolder").Value<string>();
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }
        }

        public override async Task<object> run(string? placeHolder)
        {
            HttpResponseMessage response = await sendGetRequest("");

            //The parameter modifiedSince should be in iso8601 format
            DateTime currentTimeUtc = DateTime.UtcNow;
            string iso8601Time = currentTimeUtc.ToString("O");
            Configuration_Handler.Instance.setConfig(apiName, "modifiedSince", iso8601Time);

            return await saveOutput(response.Content);
        }

        public async Task<string> saveOutput(HttpContent content)
        {
            DateTime currentTime = DateTime.Now;
            string currentTimeString = currentTime.ToString("yyyy-MM-dd-HHmmss");
            string outputFilePath = $"{outputFolder}\\{currentTimeString}.txt";
            using (StreamWriter outputWriter = new StreamWriter(outputFilePath))
            {
                if (content != null)
                {
                    var ModifiedWorkspacesString = await content.ReadAsStringAsync();
                    var ModifiedWorkspaces = JsonConvert.DeserializeObject<object[]>(ModifiedWorkspacesString);

                    if (Equals(ModifiedWorkspaces.Length, 0))
                    {
                        throw new ScanningException(apiName, "No modified workspace since the selected date.");
                    }

                    foreach (JObject modifiedWorkspace in ModifiedWorkspaces)
                    {
                        if (modifiedWorkspace.TryGetValue("id", out JToken id))
                        {
                            outputWriter.WriteLine(id);
                        }
                    }
                }
                return outputFilePath;
            }
        }
    }
}
