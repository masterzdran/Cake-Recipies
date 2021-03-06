// title           : build.cake/build.ps1
// description     : This Cake Build script will deploy/rollback IIS Farm applications.
// author		    : nuno.cancelo@polarising.com
// date            : 2020/08
//==============================================================================

#addin nuget:?package=Cake.Json&version=5.2.0
#addin nuget:?package=Newtonsoft.Json&version=12.0.3
#addin nuget:?package=Microsoft.Web.Administration&version=11.1.0
#addin nuget:?package=SharpZipLib&version=1.2.0
#addin nuget:?package=Cake.Compression&version=0.2.4

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Web.Administration;
using System;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Threading;
///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////
var target = Argument("target", "Default");
var configurationFile = Argument("configurationFile","./webfarm.json");
WebFarm webFarm = default;
FilePath deployPackagePath = default;
WebFarmHost onlineHost = default;
WebFarmHost offlineHost = default;

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(ctx =>
{
   // Executed BEFORE the first task.
   Information("Running tasks...");
});

Teardown(ctx =>
{
   // Executed AFTER the last task.
   Information("Finished running tasks.");
});

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////
Task("Import-Configuration")
    .Does((context) => 
{
   var rootPath = MakeAbsolute(Directory("."));
   var configFilename = GetFiles(configurationFile).First().GetFilename();
   var configFile = rootPath.CombineWithFilePath(configFilename);

   Information($"Trying to load {configFile}");
   var fileExist = FileExists(configFile);

   if(fileExist)
   {
      Information($"File {configFile} exists");
   }
   else
   {
       Error($"File {configFile} does not exists");
       throw new FileLoadException($"File {configFile} does not exists");
   }

   try
   {
      Information($"Loading file {configFile}");
      var config = DeserializeJsonFromFile<WebFarm>(configFile);
      webFarm = config;
   }
   catch
   {
      Error($"Problem occour while loading {configFile}");
      throw new FileLoadException($"Problem occour while loading {configFile}");
   }
   
   Information($"File {configFile} was loaded successfully.");
   Information($"----------------------------------------------------------");
   Information($"ApplicationName: {webFarm.ApplicationName}");
   Information($"BackupFolderPath: {webFarm.BackupFolderPath}");
   Information($"DeployFolderPath: {webFarm.DeployFolderPath}");
   Information($"DeployPackageName: {webFarm.DeployPackageName}");
   Information($"ServerFarmName: {webFarm.ServerFarmName}");
   Information($"MinWarmTimeoutMs: {webFarm.MinWarmTimeoutMs}");
   Information($"UpHealthCheckValue: {webFarm.UpHealthCheckValue}");
   Information($"DownHealthCheckValue: {webFarm.DownHealthCheckValue}");
   Information($"ExecuteBackupStep: {webFarm.ExecuteBackupStep}");
   foreach(var item in webFarm.Warmup)
   {
      Information($"Warmup: {item}");
   }
   Information($"----------------------------------------------------------");
});


Task("Resolve-Dependencies")
    .Does((context) => 
{
   if(webFarm == null)
   {
      Error($"Invalid configuration!");
      throw new Exception($"Invalid configuration!");
   }

   // Resolve Backup path.
   Information($"Check existance of {webFarm.BackupFolderPath}.");
   var backupFolderPath = DirectoryPath.FromString(webFarm.BackupFolderPath);
   var backupFolderExist = DirectoryExists(backupFolderPath);

   if (!backupFolderExist)
   {
      CreateDirectory(backupFolderPath);
   }

   // Resolve Deployment path.
   Information($"Check existance of {webFarm.DeployFolderPath}.");
   var deployFolderPath = DirectoryPath.FromString(webFarm.DeployFolderPath);
   var deployFolderExists = DirectoryExists(deployFolderPath);

   if (!deployFolderExists)
   {
      CreateDirectory(deployFolderPath);
   }

   // Resolver Deployment package.
   Information($"Check existance of deploy package.");
   var deployPackages = GetFiles($"{webFarm.DeployFolderPath}/*");
   
   if(deployPackages.Count() == 0)
   {
      Error($"No packages found to be deployed.");
      throw new Exception($"No packages to be deployed.");
   }
   if(deployPackages.Count() > 1)
   {
      Error($"More than ONE packages found to be deployed.");
      throw new Exception($"More than ONE packages found to be deployed.");
   }

   var packageName = deployPackages.First().GetFilename();
   var package= deployPackages.First().GetFilename();
   deployPackagePath = deployPackages.First().FullPath;
   Information($"Package found: {packageName}.");

});

Task("Resolve-Farm")
   .Does((context) =>
   {
      // Identify the Online Host
      // Identify the Offline Host
      var blue  = System.IO.File.ReadAllText(webFarm.BlueHost.HealthCheckFilePath);
      var green = System.IO.File.ReadAllText(webFarm.GreenHost.HealthCheckFilePath);

      var blueIsUP =  webFarm.UpHealthCheckValue.Equals(blue.Trim().ToUpper());

      if(blueIsUP)
      {
         onlineHost = webFarm.BlueHost;
         offlineHost = webFarm.GreenHost;
      }else{
         onlineHost = webFarm.GreenHost;
         offlineHost = webFarm.BlueHost;
      }
      Information($"Online Farm Host: {onlineHost.Site}.");
      Information($"Offline Farm Host: {offlineHost.Site}.");
   }
);

Task("Backup-FarmHost")
    .Does((context) => 
{
   if(! webFarm.ExecuteBackupStep)
   {
      Information("Bypassing {onlineHost.ApplicationPath} backup.");
      return;
   }
   // Create Backup from Online Host.
   // Versioning
   var now = DateTime.UtcNow;
   var buildNbr = now.ToString("yyyyMMddHHmmss");
   var zipFilename =$"{webFarm.BackupFolderPath}/{webFarm.ApplicationName}.{buildNbr}.zip";
   
   // Compress
   Information($"Backing up {onlineHost.ApplicationPath} to {zipFilename}");
   Zip(onlineHost.ApplicationPath,zipFilename);

});

Task("Install-Package")
    .Does((context) => 
{
   if(deployPackagePath == default)
   {
      Error($"Unable to read the deployment package.");
      throw new Exception($"Unable to read the deployment package.");
   }

   Information($"Deploying {deployPackagePath} into {offlineHost.ApplicationPath}");
   ZipUncompress(deployPackagePath,offlineHost.ApplicationPath);


});

Task("Invoke-FarmSite")
    .Does((ICakeContext context) => 
{
    HttpWebRequest request = null;    
    HttpWebResponse response = null;

   
   string site = default;
   foreach(var link in webFarm.Warmup)
   {
        Stopwatch requestStopWatch = new Stopwatch();
      site = $"{offlineHost.Site}/{link}";
      Information($"Warming up {site}.");
      long elapsedMs = 0;
      do
      {
         elapsedMs = webFarm.MinWarmTimeoutMs;
         requestStopWatch.Reset();
         requestStopWatch.Start();
         request = WebRequest.Create(site) as HttpWebRequest;
         response = request.GetResponse() as HttpWebResponse;
         requestStopWatch.Stop();
         if(response.StatusCode == HttpStatusCode.OK){
            elapsedMs = requestStopWatch.ElapsedMilliseconds;
                Information($"... {elapsedMs} elapsed.");
         }
      }while(elapsedMs >= webFarm.MinWarmTimeoutMs);
   }
});


Task("Switch-FarmSite")
    .Does((context) => 
{

   HttpWebRequest  offlineRequest = null;
   HttpWebResponse offlineResponse = null;
   
  
   offlineRequest = WebRequest.Create(onlineHost.Site) as HttpWebRequest;
   offlineResponse = offlineRequest.GetResponse() as HttpWebResponse;
   if(offlineResponse.StatusCode == HttpStatusCode.OK)
   {
      Information($"Bringing up {offlineHost.ApplicationPath}");
      var bringOnline = System.IO.File.ReadAllText(offlineHost.HealthCheckFilePath).Replace(webFarm.DownHealthCheckValue,webFarm.UpHealthCheckValue);
      System.IO.File.WriteAllText(offlineHost.HealthCheckFilePath,bringOnline);

      Thread.Sleep(5000);
      
      Information($"Bringing down {onlineHost.ApplicationPath}");
      var bringOffline = System.IO.File.ReadAllText(onlineHost.HealthCheckFilePath).Replace(webFarm.UpHealthCheckValue,webFarm.DownHealthCheckValue);
      System.IO.File.WriteAllText(onlineHost.HealthCheckFilePath,bringOffline);

   }else{
      Error($"Error ocorred while switching websites.");
      throw new Exception($"Error ocorred while switching websites.");
   }


});
///////////////////////////////////////////////////////////////////////////////
Task("Help")
.Does(
   (context) => {
      Information("HELP");
      Information("-------------------------------------------------------------");
      Information("./build.ps1 --target='WORKFLOW' --configurationFile='CONFIGURATION_JSON_PATH'");
      Information("-------------------------------------------------------------");
      Information("ARGUMENTS:");
      Information("--configurationFile='CONFIGURATION_JSON_PATH' : add configuration json file. Absolute path to the file.");
      Information("--target='WORKFLOW': Workflow to be executed. Available Worflows below.");
      Information("-------------------------------------------------------------");
      Information("WORKFLOWS:");
      Information("* Deploy-Package");
      Information("* Rollback-Package");
      Information("* Resolve-Environment");
      Information("* Help");
   }
);

Task("Default")
.IsDependentOn("Help")
.Does(() => {
   Information("Hello!");
   Information("To prevent accidents, Default behaviour of the script is to do NOTHING AT ALL!");
});

// DEPLOY WORKFLOW
Task("Deploy-Package")
   .IsDependentOn("Import-Configuration")
   .IsDependentOn("Resolve-Dependencies")
   .IsDependentOn("Resolve-Farm")
   .IsDependentOn("Backup-FarmHost")
   .IsDependentOn("Install-Package")
   .IsDependentOn("Invoke-FarmSite")
   .IsDependentOn("Switch-FarmSite")
   .Does(() => {
      Information("Deploy Succeeded!");
   });

// ROLLBACK WORKFLOW
Task("Rollback-Package")
   .IsDependentOn("Import-Configuration")
   .IsDependentOn("Resolve-Dependencies")
   .IsDependentOn("Resolve-Farm")
   .IsDependentOn("Invoke-FarmSite")
   .IsDependentOn("Switch-FarmSite")
   .Does(() => {
      Information("Rollback Succeeded!");
   });  

// RESOLVE ENVIRONMENT WORKFLOW
Task("Resolve-Environment")
   .IsDependentOn("Import-Configuration")
   .IsDependentOn("Resolve-Dependencies")
   .IsDependentOn("Resolve-Farm")
   .Does(() => {
      Information("Action Succeeded!");
   });

RunTarget(target);


///////////////////////////////////////////////////////////////////////////////
// Configuration File Classes
///////////////////////////////////////////////////////////////////////////////

public sealed class WebFarmHost
{
   public string Site { get; set; }
   public string ApplicationPath { get; set; }
   public string HealthCheckFilePath { get; set; }
}

public sealed class WebFarm
{
   public WebFarm()
   {
      this.BlueHost = new WebFarmHost();
      this.GreenHost = new WebFarmHost();
   }

   public string ApplicationName { get; set; }
   public string BackupFolderPath { get; set; }
   public string DeployFolderPath { get; set; }
   public string DeployPackageName { get; set; }
   public string ServerFarmName { get; set; }
   public int MinWarmTimeoutMs { get; set; }
   public string UpHealthCheckValue { get; set; }
   public string DownHealthCheckValue { get; set; }

   public bool ExecuteBackupStep { get; set; }
   public WebFarmHost BlueHost { get; set; }
   public WebFarmHost GreenHost { get; set; }
   public string[] Warmup{get; set;}
}