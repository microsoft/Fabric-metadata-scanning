# Fabric Metadata Scanning
This solution is for Microsoft Fabric metadata scanning APIs, presenting a simple way to run and get the result of 4 APIs:

* [Modified Workspaces API](#modified-workspaces-api)
* [Workspace Info API](#workspace-info-api)
* [Scan Status API](#scan-status-api)
* [Scan Result API](#scan-result-api)

This is a template. Feel free to adjust and modify it for your needs.
 
## How To Autenticate
	1. Go to Azure Portal.
	2. Create an AAD app (Entra Id).
	3. Click on Add -> Add Registration
	   ![img](https://github.com/Microsoft/Fabric-Metadata-Scanning/blob/master/images/add_registration.png)
	4. Set the name and supported account types and click Register.
	5. On the left bar click on API permissions.
	6. Add Permission -> Power BI Service
	7. This system manged only Delegated Permission currently, so click on that.
	8. Search for "Tenant" and check both Tenant.Read.All and Tenant.ReadWrite.All permissions.
	   ![img](https://github.com/Microsoft/Fabric-Metadata-Scanning/blob/master/images/add_permission.png)
	9. On the left bar, go to Authentication.Click on Add platform -> "Mobile and desktop applications Quickstart Docs Redirect URIs" and set http://localhost as Custom redirect URIs.
	10. On the left bar, go to Overview, and copy the Application (client) ID and Directory (tenant) ID to the configuration file (under "auth").
			   ![img](https://github.com/Microsoft/Fabric-Metadata-Scanning/blob/master/images/clientId.png)

### Pre-requirement:

	1. .NET Core
	2. Newtonsoft.Json (nuget)
	3. Microsoft.Identity.Client (nuget)

## How To Run

	1. Clone or download the files.
	2. Set the configurations you would like to use: The system limits are written in the example configuration file. 
	3. Run dotnet run < configuration file path > 
	4. The system will ask you to autheticate with you admin credentials.
	5. After each run, the configuration "modifiedSince" would be set to the current time.

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
