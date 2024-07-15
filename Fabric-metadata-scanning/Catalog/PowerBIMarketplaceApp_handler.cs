using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Fabric_metadata_scanning
{
    public class PowerBIMarketplaceApp_handler
    {

        private const string c_appCacheKeyFormat = "app-{0}"; // {0} - AppKey
        private const string c_visCacheKeyFormat = "vis-{0}"; // {0} - visual AppId
        private const string c_requestIdentifierParamName = "x-ms-clientname";
        private const string c_requestIdentifierFormat = "PowerBI-{0}"; // {0} - Random GUID per request

        public const string c_serviceAppProduct = "Power BI";
        public const string c_visualProduct = "PowerBICustomVisual";
        public const string c_serviceAppProductV2 = "PowerBI";
        public const string c_visualProductV2 = "PowerBIVisuals";

        public const string c_productExternalPurchase = "ExternalPurchase";
        public async Task<List<MarketplaceProduct>> GetMarketplaceAppsAsync(string marketplaceApiUrl)
        {
            var urlBuilder = BuildMarketplaceApiUrlWithFilter(marketplaceApiUrl);

            var productList = new List<MarketplaceProduct>();
            bool hasNextLink;

            int pageIndex = 0;
            do
            {
                try
                {
                    // Attach a URL parameter "x-ms-clientname=PowerBI-<SOME_GUID>"
                    // This parameter value is logged in AppSource telemetry, and will allow for easier correlation of events.
                    urlBuilder.SetQueryParameter(c_requestIdentifierParamName, c_requestIdentifierFormat.FormatWithInvariantCulture(Guid.NewGuid()));
                    Console.WriteLine($"Marketplace Cache: Fetching page {0} from URL '{1}'", pageIndex, urlBuilder.Uri);

                    var response = await GetAsync<MarketplaceProductCollection>(urlBuilder.Uri);
                    hasNextLink = !response.NextPageLink.IsNullOrEmpty();

                    var apps = response.Items.Where(product => product.OfferType == c_serviceAppProductV2 && !(product.IsPreview ?? false) && ((product.Plans?.FirstOrDefault())?.RedirectUrl?.Contains("Redirect?action=InstallApp") ?? false));
                    productList.AddRange(apps);

                    var visuals = response.Items.Where(app => app.OfferType == c_visualProductV2);
                    productList.AddRange(visuals);

                    Console.WriteLine($"Marketplace Cache: Fetching page {0} completed successfully. Added {1} apps. hasNextLink={2}", pageIndex, apps.Count(), hasNextLink);

                    if (hasNextLink)
                    {
                        // API might return a nextLink referecing a different domain, we only use the query from it
                        var nextUri = new Uri(response.NextPageLink);
                        urlBuilder.Query = nextUri.Query.Replace("?", string.Empty);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Marketplace Cache: An error occured while fetching apps from URL '{0}' (Page {1}). Error details: {2}", urlBuilder.Uri, pageIndex, ex);
                    // After an error from AppSource, stop fetching more applications - this can lead to a partial apps list, but otherwise it will be empty.
                    hasNextLink = false;
                }
                finally
                {
                    pageIndex++;
                }
            }
            while (hasNextLink);

            return productList;
        }

        public async Task<TResult> GetAsync<TResult>(Uri uri) where TResult : class
        {
            using (HttpClient httpClient = new HttpClient())
            {
                HttpResponseMessage response;

                // Used for telemetry purposes, DO NOT MODIFY.
                httpClient.DefaultRequestHeaders.Add("X-POWERBI-ADMIN-CLIENT-NAME", "FabricScanningClient");

                response = await httpClient.GetAsync(uri);

                string jsonStr = await response.Content.ReadAsStringAsync();

                var resultItems = JsonConvert.DeserializeObject<TResult>(jsonStr);
                return resultItems;
            }

        }


        private UriBuilder BuildMarketplaceApiUrlWithFilter(string url)
        {
            string visualsProduct = "PowerBIVisuals";
            string filterParam = "$filter";
            string singleProductFilter = @"offertype eq '{0}'";

            var urlBuilder = new UriBuilder(url);

            var filterValue =
                singleProductFilter.FormatWithInvariantCulture(visualsProduct);
            urlBuilder.SetQueryParameter(filterParam, filterValue);
            return urlBuilder;
        }

    }
    public static class ExtendedText
    {

        [StringFormatMethod("format")]
        public static string FormatWithInvariantCulture([NotNull] this string format, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, format, args);
        }
    }

    public static class ExtendedUri
    {
        public static void SetQueryParameter(this UriBuilder uriBuilder, string name, string value)
        {
            NameValueCollection queryParameters = HttpUtility.ParseQueryString(uriBuilder.Query);
            queryParameters.Set(name, value);

            uriBuilder.Query = queryParameters.ToString();
        }
    }


    public static class ExtendedEnumerable
    {
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            return enumerable == null || !enumerable.Any();
        }
    }
}
