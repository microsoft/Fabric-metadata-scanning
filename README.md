Last modified: 05/20/2024
# Fabric Metadata Scanning
This solution is for Microsoft Fabric metadata scanning APIs, presenting a simple way to run and get the result of 4 APIs:

* [Modified Workspaces API](https://learn.microsoft.com/en-us/rest/api/power-bi/admin/workspace-info-get-modified-workspaces)
* [Workspace Info API](https://learn.microsoft.com/en-us/rest/api/power-bi/admin/workspace-info-post-workspace-info)
* [Scan Status API](https://learn.microsoft.com/en-us/rest/api/power-bi/admin/workspace-info-get-scan-status)
* [Scan Result API](https://learn.microsoft.com/en-us/rest/api/power-bi/admin/workspace-info-get-scan-result)

This is a template. Feel free to adjust and modify it for your needs.
 
## How To Autenticate
1. Go to Azure Portal.
2. Create an AAD app (Entra Id).
3. Click on Add -> Add Registration:

![registration image](https://github.com/microsoft/Fabric-metadata-scanning/blob/main/Fabric-metadata-scanning/images/add_registration.png)

4. Set the name and supported account types and click Register.
5. On the left bar, go to Overview, and copy the Application (client) ID and Directory (tenant) ID to the configuration file (under "auth").
6. Go to Fabric(Power BI) portal, click on Settings -> Admin portal -> Admin API settings.
   Enable Admin Switch: Enhance admin APIs responses with detailed metadata, choose Specific
   security groups (recommended) and add your security group to the list (for both switches).

![img](https://github.com/microsoft/Fabric-metadata-scanning/blob/main/Fabric-metadata-scanning/images/clientId.png)

### Using Service Principal(Recommended):
7. On the app's left bar click on Certificates & secrets and create new client secret. You will get a secret value, copy that value to a safe place.
8. In Azure Portal create a Key Vault (under your/new Resource Group).
9. Set access: In the Key Vault left bar under access control, add yourself as a Key Vault Administrator role assignment, and your application as Key Vault Certificate User.
10. Create a secret inside the Key Vault (Secrets -> Generate/Import -> paste the secret value you copied in step 5 and pick you secret name -> Create).
11. Create a certificate in the Key Vault (Certificates -> Generate/Import -> pick certificate name and subject -> Generate). Use the key vault, secret and certificate names in the configuration file.
12. Download the current version of the certificate to your local machine (Certificates -> [Certificate Name] -> [Version Number] -> Download in CER format).
13. Go back to your app and upload the certificate (Certificates & secrets -> Certificates -> Upload certificate).
14. [Create a security group](https://learn.microsoft.com/en-us/entra/fundamentals/how-to-manage-groups#create-a-basic-group-and-add-members) , and add your app as an member and yourself as an Owner.
15. Go to Fabric(Power BI) portal, click on Settings -> Admin portal -> Admin API settings.
    Enable Admin Switches: Service principals can access read-only admin APIs, choose Specific security groups (recommended) and add your security group to the list (for both switches).

### Using delegated Token (Specific user):
7. On the app's left bar click on API permissions.
8. Add Permission -> Power BI Service
9. This system manged only Delegated Permission currently, so click on that.
10. Search for "Tenant" and check Tenant.Read.All permissions.

![img](https://github.com/microsoft/Fabric-metadata-scanning/blob/main/Fabric-metadata-scanning/images/add_permission.png)

9. On the left bar, go to Authentication.Click on Add platform -> "Mobile and desktop applications Quickstart Docs Redirect URIs" and set http://localhost as Custom redirect URIs.

### Pre-requirement:

1. .NET Core
2. Newtonsoft.Json (nuget)
3. Microsoft.Identity.Client (nuget)

## How To Run

1. Clone or download the files.
2. Set the configurations you would like to use: The system limits are written in the example configuration file.
3. From Fabric-metadata-scanning\Fabric-metadata-scanning folder, run "dotnet run <configuration file path>" (without the quotes). If no <configuration file path> specified, the system use the example config file location.
For Example: cd Fabric-metadata-scanning; dotnet run
4. The system will ask you to autheticate with your admin credentials.
5. After each run, the configuration "modifiedSince" would be set to the current time. IMPORTANT - to enable the incremental scan(set modifiedSince parameter to the last run time automatically) on debug also, set the configuration file property 'Copy to Output Directory' value  to 'Copy if newer'.
6. Once finished, the output file path would be printed to the console.  

## Modified Workspaces API

	This API get the modified workspaces since modifySince parameter set on the configuration file.This parameter should be on iso8601 format.
	Also Possible to use "alwaysFullScan" flag which ignores the modifySince parameter.
	Saves the output in outputs/ModifiedWorkspaces/< date >.txt as a list of worspaces Ids.

## Workspace Info API

	This API use the output from Modified Workspaces API, take chunk of Ids and sends for scan those ids.
	The chunk size set on the configurationfile(getInfo section)
	Returns the scan Id to the Main function.

## Scan Status API

	This API use the output from Workspace Info API (scanId), and return the scan status.
	The system waits for this API to return "Succeeded".
	 
## Scan Result API

	Once scan succeeded, The system asked for detailed result from this API.
	Each workspace details saved on outputs/< workspaceId >/< scanId >_< date >.json
	Final result report saved on outputs/Results/< date >.json

## Configuration File
	Section for each API - "modified", "getInfo","scanStatus","scanResult".
	More sections: "shared","auth"

## Data Collection

The software may collect information about you and your use of the software and send it to Microsoft. 
Microsoft may use this information to provide services and improve our products and services. You may turn off the telemetry as described in the repository. 
There are also some features in the software that may enable you and Microsoft to collect data from users of your applications. 
If you use these features, you must comply with applicable law, including providing appropriate notices to users of your applications together with a copy of Microsoft’s privacy statement. Our privacy statement is located at https://go.microsoft.com/fwlink/?LinkID=824704. You can learn more about data collection and use in the help documentation and our privacy statement. Your use of the software operates as your consent to these practices.
