using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fabric_Metadata_Scanning
{

    public sealed class Configuration_Handler
    {
        private static Configuration_Handler instance = null;

        public string _configurationFilePath { get; set; }
        public JObject _configurationSettings { get; set; }

        private Configuration_Handler()
        {

        }

        public static Configuration_Handler Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Configuration_Handler();
                }
                return instance;
            }
        }

        public void setConfigurationsFile(string configurationFilePath)
        {
            _configurationFilePath = configurationFilePath;
            string jsonString = File.ReadAllText(_configurationFilePath);
            _configurationSettings = JObject.Parse(jsonString);
            validateConfigs();
        }

        private void validateConfigs()
        {
            int threadsCount = getConfig("shared", "threadsCount").Value<int>();
            if (threadsCount < 1 || threadsCount > 16)
            {
                string errorMessage = "The number of threads need to be between 1-16.";
                throw new ScanningException("Configurations", errorMessage);
            }

            int defaultRetryAfter = getConfig("shared", "defaultRetryAfter").Value<int>();
            if (defaultRetryAfter <= 0)
            {
                string errorMessage = "The defaultRetryAfter need to be a positive number.";
                throw new ScanningException("Configurations", errorMessage);
            }


            bool datasetExpressions = getConfig("getInfo", "datasetExpressions").Value<bool>();
            bool datasetSchema = getConfig("getInfo", "datasetSchema").Value<bool>();
            if (datasetExpressions && !datasetSchema)
            {
                string errorMessage = "datasetSchema can not set to false while datasetExpressions is set to true";
                throw new ScanningException("getInfo", errorMessage);
            }

            int chunkMaxSize = getConfig("getInfo", "chunkMaxSize").Value<int>();
            if (chunkMaxSize < 1 || chunkMaxSize > 100)
            {
                string errorMessage = "The number of threads need to be between 1-100.";
                throw new ScanningException("getInfo", errorMessage);
            }



        }

        public JObject getApiSettings(string apiName)
        {
            return (JObject)_configurationSettings[apiName];
        }

        public JToken getConfig(string apiName, string parameterName)
        {
            if (_configurationSettings.TryGetValue(apiName, out var apiSettings) &&
                ((JObject)apiSettings).TryGetValue(parameterName, out JToken parameterValue))
            {
                return parameterValue;
            }
            else
            {
                return null;
            }
        }

        public void setConfig(string apiName, string parameterName, JToken value)
        {
            _configurationSettings.TryGetValue(apiName, out JToken apiSettings);
            ((JObject)apiSettings)[parameterName] = value;

            string jsonString = JsonConvert.SerializeObject(_configurationSettings, Formatting.Indented);
            File.WriteAllText(_configurationFilePath, jsonString);
        }
    }






}
