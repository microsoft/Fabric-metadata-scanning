
namespace Fabric_Metadata_Scanning
{
    public class ScanningException : Exception
    {
        public string ReadMeLink = "https://github.com/Microsoft/Fabric-Metadata-Scanning/blob/master/ReadMe.md";

        public ScanningException(string apiName, string errorMessage) :
            base($"{apiName} has an error: {errorMessage}. Please check its properties.")

        { 
            HelpLink = "https://learn.microsoft.com/en-us/rest/api/power-bi/admin/workspace-info-get-modified-workspaces";
        }
    }
}
