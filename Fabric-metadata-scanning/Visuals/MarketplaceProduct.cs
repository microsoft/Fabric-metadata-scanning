//----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
//----------------------------------------------------------------------------

using System.Runtime.Serialization;

namespace Fabric_metadata_scanning
{
    [DataContract]
    public class MarketplaceProductCollection
    {
        [DataMember(Name = "items")]
        public List<MarketplaceProduct> Items { get; set; }

        [DataMember(Name = "nextPageLink")]
        public string NextPageLink { get; set; }
    }

    [DataContract]
    public class MarketplaceProduct
    {
        private List<string> categoryIds;

        public const string c_powerBICertfied = "PowerBICertified";

        [DataMember(Name = "legacyId")]
        public string LegacyId { get; set; }

        [DataMember(Name = "displayName")]
        public string DisplayName { get; set; }

        [DataMember(Name = "summary")]
        public string Summary { get; set; }

        [DataMember(Name = "iconFileUris")]
        public MarketplaceProductIconFileUris IconFileUris { get; set; }

        [DataMember(Name = "enrichedData")]
        public MarketplaceProductEnrichedData EnrichedData { get; set; }

        [DataMember(Name = "publisherDisplayName")]
        public string PublisherDisplayName { get; set; }

        [DataMember(Name = "publisherId")]
        public string PublisherId { get; set; }

        [DataMember(Name = "offerType")]
        public string OfferType { get; set; }

        [DataMember(Name = "plans")]
        public List<MarketplaceProductPlan> Plans { get; set; }

        [DataMember(Name = "privacyPolicyUri")]
        public string PrivacyPolicyUri { get; set; }

        [DataMember(Name = "description")]
        public string Description { get; set; }

        [DataMember(Name = "legalTermsUri")]
        public string LegalTermsUri { get; set; }

        [DataMember(Name = "offerVersion")]
        public string OfferVersion { get; set; }

        [DataMember(Name = "powerBIVisualId")]
        public string PowerBIVisualId { get; set; }

        [DataMember(Name = "categoryIds")]
        public List<string> CategoryIds 
        { 
            get { return categoryIds; }
            set
            {
                categoryIds = value;
                categoryIds.Remove(c_powerBICertfied);
            } 
        }

        [DataMember(Name = "bigId")]
        public string BigId { get; set; }

        [DataMember(Name = "pbiServicePrincipalIds")]
        public List<string> PBIServicePrincipalIds { get; set; }

        [DataMember(Name = "bigCatLastModifiedDate")]
        public DateTime? BigCatLastModifiedDate { get; set; }

        [DataMember(Name = "downloadLink")]
        public string DownloadLink { get; set; }

        [DataMember(Name = "appFreeType")]
        public string AppFreeType { get; set; }

        [DataMember(Name = "downloadSampleLink")]
        public string DownloadSampleLink { get; set; }

        [DataMember(Name = "mixProductId")]
        public string MixProductId { get; set; }

        [DataMember(Name = "isPreview")]
        public bool? IsPreview { get; set; }

        [DataMember(Name = "pricingTypes")]
        public List<string> PricingTypes { get; set; }

        [DataMember(Name = "licenseManagementType")]
        public string LicenseManagementType { get; set; }

    }

    [DataContract]
    public class MarketplaceProductIconFileUris
    {
        [DataMember(Name = "small")]
        public string Small { get; set; }

        [DataMember(Name = "large")]
        public string Large { get; set; }
    }

    [DataContract]
    public class MarketplaceProductEnrichedData
    {
        [DataMember(Name = "tags")]
        public List<string> Tags { get; set; }

        [DataMember(Name = "popularity")]
        public MarketplaceProductPopularity Popularity { get; set; }

        [DataMember(Name = "rating")]
        public MarketplaceProductRating Rating { get; set; }
    }

    [DataContract]
    public class MarketplaceProductPopularity
    {
        [DataMember(Name = "appSourceApps")]
        public double AppSourceApps { get; set; }
    }

    [DataContract]
    public class MarketplaceProductRating
    {
        [DataMember(Name = "appSource")]
        public MarketplaceProductAppsource AppSource { get; set; }
    }

    [DataContract]
    public class MarketplaceProductAppsource
    {
        [DataMember(Name = "averageRating")]
        public double AverageRating { get; set; }

        [DataMember(Name = "totalRatings")]
        public int TotalRatings { get; set; }
    }

    [DataContract]
    public class MarketplaceProductPlan
    {
        [DataMember(Name = "redirectUrl")]
        public string RedirectUrl { get; set; }
    }

}
