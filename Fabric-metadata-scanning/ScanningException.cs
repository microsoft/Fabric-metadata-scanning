
namespace Fabric_Metadata_Scanning
{
    public class ScanningException : Exception
    {
        public string ReadMeLink = "https://github.com/microsoft/Fabric-metadata-scanning/blob/main/README.md";

        public ScanningException(string apiName, string errorMessage) :
            base($"{apiName} has an error: {errorMessage}. Please check its properties.")

        { 
            HelpLink = "https://learn.microsoft.com/en-us/rest/api/power-bi/admin/workspace-info-get-modified-workspaces";
        }
    }
}
