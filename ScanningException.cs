
namespace Fabric_Metadata_Scanning
{
    public class ScanningException : Exception
    {
        public string ReadMeLink = "https://github.com/yarinkagal/Fabric_Metadata_Scanning/blob/master/ReadMe.md";
        public string HelpLink = "https://learn.microsoft.com/en-us/rest/api/power-bi/admin/workspace-info-get-modified-workspaces";

        public ScanningException(string apiName, string errorMessage) :
            base($"{apiName} has an error: {errorMessage}. Please check it's properties.")
        { }
    }
}
