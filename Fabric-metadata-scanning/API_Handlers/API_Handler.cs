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
                HttpResponseMessage response;
                setHeaders(httpClient);
                do
                {
                    response = await httpClient.GetAsync(apiUriBuilder.Uri + $"/{scanId}");

                } while (!await verifySuccess(response));
                

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

        public async Task<bool> verifySuccess(HttpResponseMessage response)
        {

            if (!response.IsSuccessStatusCode)
            {
                if((int)response.StatusCode == 429)
                {
                    int retryAfter;
                    RetryConditionHeaderValue retryAfterObject = response.Headers.RetryAfter;

                    if (Equals(retryAfterObject, null))
                    {
                        retryAfter = (int)Configuration_Handler.Instance.getConfig("shared", "defaultRetryAfter");
                    }
                    else
                    {                  
                        int.TryParse(retryAfterObject.ToString(), out retryAfter );
                    }

                    if (retryAfter > 0)
                    {
                        Console.WriteLine($"Too many requests for {apiName} API. Retrying in {retryAfter} seconds");
                        Thread.Sleep(retryAfter*1000);
                        return false;
                    }
                    return true;
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
            return true;
        }

        public void setHeaders(HttpClient httpClient)
        {
            // Set the authorization header with the access token, to identify the specific client 
            string accessToken = Auth_Handler.Instance.accessToken;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // Used for telemetry purposes, DO NOT MODIFY.
            httpClient.DefaultRequestHeaders.Add("X-POWERBI-ADMIN-CLIENT-NAME", "FabricScanningClient");
        }
    }
}
