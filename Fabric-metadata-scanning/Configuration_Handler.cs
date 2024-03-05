using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fabric_Metadata_Scanning
{
    public sealed class Configuration_Handler
    {
        private static Configuration_Handler instance = null;
        private static object lockObject = new object();

        public string _configurationFilePath { get; set; }
        public JObject _configurationSettings { get; set; }
        public DateTime scanStartTime { get; set; }

        private Configuration_Handler(){}

        public static Configuration_Handler Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        if (instance == null)
                        {
                            instance = new Configuration_Handler();
                        }
                    }
                }
                
                return instance;
            }
        }

        public void setConfigurationsFile(string[] args)
        {
            
            if(args.Length == 0)
            {
                // Use default configuration file.
                _configurationFilePath = "configurationsFile.json";
            }
            else
            {
                _configurationFilePath = args[0];
            }
            try
            {
                string jsonString = File.ReadAllText(_configurationFilePath);
                _configurationSettings = JObject.Parse(jsonString);
                scanStartTime = DateTime.Now;
            }
            catch
            {
                throw new ScanningException("Configurations",$"Expected a json configuration file in {_configurationFilePath}");
            }
            validateConfigs();
        }

        private void validateConfigs()
        {
            string errorMessage;

            var threadsCount = getConfig("shared", "threadsCount").Value<int>();
            if (threadsCount < 1 || threadsCount > 16)
            {
                errorMessage = "The number of threads need to be between 1-16.";
                throw new ScanningException("Configurations", errorMessage);
            }

            int defaultRetryAfter = getConfig("shared", "defaultRetryAfter").Value<int>();
            if (defaultRetryAfter <= 0)
            {
                errorMessage = "The defaultRetryAfter need to be a positive number.";
                throw new ScanningException("Configurations", errorMessage);
            }

            bool datasetExpressions = getConfig("getInfo", "datasetExpressions").Value<bool>();
            bool datasetSchema = getConfig("getInfo", "datasetSchema").Value<bool>();
            if (datasetExpressions && !datasetSchema)
            {
                errorMessage = "datasetSchema can not set to false while datasetExpressions is set to true";
                throw new ScanningException("getInfo", errorMessage);
            }

            int chunkMaxSize = getConfig("getInfo", "chunkMaxSize").Value<int>();
            if (chunkMaxSize < 1 || chunkMaxSize > 100)
            {
                errorMessage = "The number of threads need to be between 1-100.";
                throw new ScanningException("getInfo", errorMessage);
            }

            var authMethod = getConfig("auth", "authMethod").Value<string>();
            var optionalAuthMethods = new [] { "Service_Principal", "Deligaded_Token" };
            if (!optionalAuthMethods.Contains(authMethod))
            {
                errorMessage = "The authentication method should be Service_Principal or Deligaded_Token.";
                throw new ScanningException("auth", errorMessage);
            }
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
                throw new ScanningException(apiName, $"Can't get config of {apiName} called {parameterName}");
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
