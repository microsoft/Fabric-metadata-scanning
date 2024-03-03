using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.KeyVault;
using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography.X509Certificates;
using Azure.Identity;
using static System.Formats.Asn1.AsnWriter;
using Azure.Security.KeyVault.Secrets;
using Azure.Security.KeyVault.Certificates;
using System.Net.Sockets;
using Microsoft.Rest.Azure;
using Azure.Core;
using Microsoft.Rest;
using Microsoft.PowerBI.Api;
using System.Runtime.ConstrainedExecution;
using Azure;
using System;
using Microsoft.Azure.KeyVault;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using System.Text;

namespace Fabric_Metadata_Scanning
{
    public class CertificateDownloader
    {
        private readonly HttpClient httpClient;

        public CertificateDownloader()
        {
            httpClient = new HttpClient();
        }

        public async Task<X509Certificate2> GetCertificateAsync(string url)
        {
            // Perform asynchronous operation to download certificate from the specified URL
            HttpResponseMessage response = await httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                // Read the certificate content as bytes
                byte[] certificateBytes = await response.Content.ReadAsByteArrayAsync();

                // Create X509Certificate2 instance from the downloaded bytes
                X509Certificate2 certificate = new X509Certificate2(certificateBytes);

                return certificate;
            }
            else
            {
                // Handle error scenario, e.g., certificate not found or inaccessible
                throw new Exception($"Failed to download certificate. HTTP status code: {response.StatusCode}");
            }
        }
    }

    class Auth_Handler
    {
        private static Auth_Handler instance = null;
        private static object lockObject = new object();
        private Auth_Handler() {}

        public static Auth_Handler Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        if (instance == null)
                        {
                            instance = new Auth_Handler();
                        }
                    }
                }
                return instance;
            }
        }
        public string apiName = "auth";
        public string accessToken { get; set; }

        private static X509Certificate2 GetCertificate(CertificateClient certificateClient,SecretClient secretClient,string certificateName)
        {
            KeyVaultCertificateWithPolicy certificate = certificateClient.GetCertificate(certificateName);

            // Return a certificate with only the public key if the private key is not exportable.
            if (certificate.Policy?.Exportable != true)
            {
                return new X509Certificate2(certificate.Cer);
            }

            // Parse the secret ID and version to retrieve the private key.
            string[] segments = certificate.SecretId.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            string secretName = segments[1];
            string secretVersion = segments[2];
            KeyVaultSecret secret = secretClient.GetSecret(secretName, secretVersion);

            if ("application/x-pkcs12".Equals(secret.Properties.ContentType, StringComparison.InvariantCultureIgnoreCase))
            {
                byte[] pfx = Convert.FromBase64String(secret.Value);
                return new X509Certificate2(pfx);
            }

            throw new NotSupportedException($"Only PKCS#12 is supported. Found Content-Type: {secret.Properties.ContentType}");
        }

        public async Task<string> authenticate()
        {
            string clientId = Configuration_Handler.Instance.getConfig(apiName, "clientId").Value<string>();
            string tenantId = Configuration_Handler.Instance.getConfig(apiName, "tenantId").Value<string>();
            string authMethod = Configuration_Handler.Instance.getConfig(apiName, "authMethod").Value<string>();
            string tenantAuthority = $"https://login.microsoftonline.com/{tenantId}";
            string[] scopes;

            try
            {
                if (authMethod.Equals("Service_Principal"))
                {
                    scopes = new [] { "https://analysis.windows.net/powerbi/api/.default" };
                    var keyVaultName = Configuration_Handler.Instance.getConfig(apiName, "keyVaultName").Value<string>();
                    Uri keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net");

                    var keyVaultSecretClient = new SecretClient(keyVaultUri, new DefaultAzureCredential());
                    var secretName = Configuration_Handler.Instance.getConfig(apiName, "secretName").Value<string>(); ;
                    KeyVaultSecret keyVaultSecret = await keyVaultSecretClient.GetSecretAsync(secretName);

                    var credentials = new ClientSecretCredential(tenantId: tenantId, clientId: clientId, clientSecret: keyVaultSecret.Value);
                    var appSecretClient = new SecretClient(keyVaultUri, credentials);

                    var certificateName = Configuration_Handler.Instance.getConfig(apiName, "certificateName").Value<string>(); ;
                    var certificateClient = new CertificateClient(keyVaultUri, credentials);
                    var certificate = GetCertificate(certificateClient, appSecretClient, certificateName);

                    var appBuilder = ConfidentialClientApplicationBuilder.Create(clientId)
                        .WithCertificate(certificate)
                        .WithAuthority(tenantAuthority)
                        .Build();

                    var result = await appBuilder.AcquireTokenForClient(scopes)
                        .ExecuteAsync();
                    Instance.accessToken = result.AccessToken;

                    return result.AccessToken;
                }

                else if (authMethod.Equals("Deligaded_Token"))
                {
                    scopes = new [] { "https://analysis.windows.net/powerbi/api/Tenant.Read.All" };
                    
                    var appBuilder = PublicClientApplicationBuilder
                        .Create(clientId)
                        .WithAuthority(tenantAuthority)
                        .WithRedirectUri("http://localhost")
                        .Build();

                    var result = await appBuilder.AcquireTokenInteractive(scopes)
                                        .WithUseEmbeddedWebView(false)
                                        .ExecuteAsync();

                    Instance.accessToken = result.AccessToken;

                    return result.AccessToken;
                }
                else
                {
                    throw new Exception("The authentication method should be Service_Principal or Deligaded_Token.");
                }
            }
            catch (Exception ex)
            {
                throw new ScanningException(apiName, ex.Message);
            }
        }
    }
}
