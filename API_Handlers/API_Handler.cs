using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace Fabric_Metadata_Scanning
{
    public abstract class API_Handler
    {
        public string apiName { get; set; }
        public UriBuilder apiUriBuilder { get; set; }
        public JObject parameters { get; set; }

        public API_Handler(string apiName)
        {
            this.apiName = apiName;
            parameters = new JObject();
            apiUriBuilder = new UriBuilder($"https://api.powerbi.com/v1.0/myorg/admin/workspaces/{apiName}");
        }
        
        public abstract Task<object> run(string? scanId);

        public async Task<HttpResponseMessage> sendGetRequest(string? scanId)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                // Set the authorization header with the token
                string accessToken = Auth_Handler.Instance.accessToken;
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                // Detect usage - DO NOT MODIFY
                httpClient.DefaultRequestHeaders.Add("X-POWERBI-ADMIN-CLIENT-NAME", "FabricScanningClient");
                
                // Send the GET request
                HttpResponseMessage response = await httpClient.GetAsync(apiUriBuilder.Uri + $"/{scanId}");

                int retryAfter = await verifySuccess(response);

                if (retryAfter > 0)
                {
                    Console.WriteLine($"To many requests for {apiName} API. Retring in {retryAfter / 1000} seconds");
                    Thread.Sleep(retryAfter);
                    return await sendGetRequest(scanId);
                }
                
                return response;
            }
        }

        public void setRequestParameters()
        {
            StringBuilder parametersString = new StringBuilder();
            foreach (JProperty apiProperty in parameters.Properties())
            {
                JToken token = apiProperty.Value;
                parametersString.Append($"{apiProperty.Name}={token.Value<object>()}&");
            }

            apiUriBuilder.Query = parametersString.ToString();
        }

        public async Task<int> verifySuccess(HttpResponseMessage response)
        {

            if (!response.IsSuccessStatusCode)
            {
                if((int)response.StatusCode == 429)
                {
                    RetryConditionHeaderValue retryAfterObject = response.Headers.RetryAfter;
                    if (Equals(retryAfterObject, null))
                    {
                        return (int)Configuration_Handler.Instance.getConfig("shared", "defaultRetryAfter");
                    }
                    else
                    {
                        int.TryParse(retryAfterObject.ToString(), out int retryAfter);
                        return retryAfter;
                    }
                }
                var jsonString = await response.Content.ReadAsStringAsync();
                dynamic errorObject = JObject.Parse(jsonString);
                if (errorObject?.error.details != null)
                {
                    throw new ScanningException(apiName, errorObject.error.details.message);
                }
                else
                {
                    throw new ScanningException(apiName, errorObject?.error.message);
                }
            }
            return 0;
        }
    }
}
