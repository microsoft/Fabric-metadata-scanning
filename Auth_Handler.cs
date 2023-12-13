using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;

namespace Fabric_Metadata_Scanning
{
    class Auth_Handler
    {
        private static Auth_Handler instance = null;
        public static Auth_Handler Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Auth_Handler();
                }
                return instance;
            }
        }
        public string apiName = "auth";
        public string accessToken { get; set; }


        public async Task<string> authenticate()
        {
            string clientId = Configuration_Handler.Instance.getConfig(apiName, "clientId").Value<string>();
            string tenantId = Configuration_Handler.Instance.getConfig(apiName, "tenantId").Value<string>();

            string[] scopes = { "https://analysis.windows.net/powerbi/api/Tenant.Read.All",
                                "https://analysis.windows.net/powerbi/api/Tenant.ReadWrite.All"
                              }; // Use the appropriate scope for Power BI

            Uri tenantAuthority = new Uri($"https://login.microsoftonline.com/{tenantId}");

            string redirectUri = "http://localhost";
            try
            {
                IPublicClientApplication app = PublicClientApplicationBuilder
                    .Create(clientId)
                    .WithAuthority(tenantAuthority)
                    .WithRedirectUri(redirectUri)
                    .Build();

                var result = await app.AcquireTokenInteractive(scopes)
                                    .WithUseEmbeddedWebView(false)
                                    .ExecuteAsync();

                Instance.accessToken = result.AccessToken;
                return accessToken;
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }
    }
}
