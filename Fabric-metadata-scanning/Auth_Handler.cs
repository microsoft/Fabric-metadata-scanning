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
            if (segments.Length != 3)
            {
                throw new InvalidOperationException($"Number of segments is incorrect: {segments.Length}, URI: {certificate.SecretId}");
            }

            string secretName = segments[1];
            string secretVersion = segments[2];

            KeyVaultSecret secret = secretClient.GetSecret(secretName, secretVersion);

            // For PEM, you'll need to extract the base64-encoded message body.
            // .NET 5.0 preview introduces the System.Security.Cryptography.PemEncoding class to make this easier.
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
            //string authMethod

            string[] scopes = { "https://analysis.windows.net/powerbi/api/Tenant.Read.All",
                                "https://analysis.windows.net/powerbi/api/Tenant.ReadWrite.All"
                              }; // Use the appropriate scope for Power BI

            string tenantAuthority = $"https://login.microsoftonline.com/{tenantId}";

            string redirectUri = "http://localhost";
            //scopes = new[] { "https://analysis.windows.net/powerbi/api/.default" };


            try
            {

                string keyVaultName = "fabric-md-sample-app-kv";
                Uri keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net");
                var client = new SecretClient(keyVaultUri, new DefaultAzureCredential());

                var secretName = "Fabric-metadata-scanning-sample-app-kv-secret";
                KeyVaultSecret keyVaultSecret = await client.GetSecretAsync(secretName);

                var credentials = new ClientSecretCredential(tenantId: tenantId, clientId: clientId, clientSecret: keyVaultSecret.Value);

                var secretClient = new SecretClient(keyVaultUri, credentials);

                string certName = "Fabric-metadata-scanning-sample-app-kv-cert";
                var certClient = new CertificateClient(keyVaultUri, credentials);

                var cert = GetCertificate(certClient, secretClient, certName);

                Console.WriteLine("Certificate loaded");
                var app2 = ConfidentialClientApplicationBuilder.Create(clientId)
                    .WithCertificate(cert)
                    .WithAuthority(tenantAuthority)
                    .Build();

                var result2 = await app2.AcquireTokenForClient(scopes)
                    .ExecuteAsync();
                Instance.accessToken = result2.AccessToken;
                int x = 0;

                return result2.AccessToken;



                //var identifer = new KeyVaultSecretIdentifier(certResp.Value.SecretId);

                //var secretClient = new SecretClient(keyVaultUrl, creds);
                //var secretResp = secretClient.GetSecret(identifer.Name, identifer.Version);

                //byte[] privateKeyBytes = Convert.FromBase64String(secretResp.Value.Value);

                //var cert = new X509Certificate2(privateKeyBytes);



                //return result.AccessToken;


                ///// Service Principal with secret////

                //var app = ConfidentialClientApplicationBuilder.Create(clientId)
                //    .WithClientSecret(clientSecret)
                //    .WithAuthority(tenantAuthority)
                //    .Build();


                //scopes = new[] { "https://analysis.windows.net/powerbi/api/.default" };
                //var result = await app.AcquireTokenForClient(scopes)
                //    .ExecuteAsync();

                //Instance.accessToken = result.AccessToken;

                /////////////// deligated ////
                //IPublicClientApplication app = PublicClientApplicationBuilder
                //    .Create(clientId)
                //    .WithAuthority(tenantAuthority)
                //    .WithRedirectUri(redirectUri)
                //    .Build();

                //var result = await app.AcquireTokenInteractive(scopes)
                //                    .WithUseEmbeddedWebView(false)
                //                    .ExecuteAsync();

                //Instance.accessToken = result.AccessToken;
                //return result.AccessToken;

            }

            catch (Exception ex)
            {
                throw new ScanningException(apiName, ex.Message);
            }
        }
    }
}
