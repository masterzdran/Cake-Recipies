// ---------------------------------------
// Run sonarqube analisys. 
// Variables defined:
//  - solutionPath
//  - configuration
//  - artifactsPath
//  - testResultsPath
//  - XUnitReportsPath
//  - OpenCoverReportsPath
//  - JOB_URL
//  - GIT_COMMIT
// 
// Configuration needed:
//    Key = ""
//    Login = ""
//    Url = ""
// #tool nuget:?package=MSBuild.SonarQube.Runner.Tool
// #addin nuget:?package=Cake.Sonar
// ---------------------------------------
Task("SonarBegin")
    .Does((context) => 
{ 
     Information("Solution_Build Task.");
     MSBuild(solutionPath, settings =>
        {
            settings
                .SetConfiguration(configuration)
                .WithTarget("Restore")
                .WithTarget("Rebuild")
                .WithTarget("Pack")
            ;
            settings.ToolPath = settings.ToolPath ?? context.Tools.Resolve("msbuild.exe");
            settings.MaxCpuCount = 0;
            settings.NodeReuse = false;
            settings.Properties["CI"] = new string [] { "true" };
            settings.Properties["PackageOutputPath"] = new string [] { artifactsPath.FullPath };
            settings.BinaryLogger = new MSBuildBinaryLogSettings
            {
                Enabled = true,
                Imports = MSBuildBinaryLogImports.Embed,
            };
        });
});


Task("SonarBegin")
  .Does(() => {
        var VsTestReportsPath =  testResultsPath.CombineWithFilePath(GetFiles(testResultsPath+"/vstest-results-*.trx").First().GetFilename()+"");
        var VsCoverageReportsPath = testResultsPath.CombineWithFilePath(GetFiles(testResultsPath+"/vscoverage-results-*.xml").First().GetFilename()+"");
        Information(JOB_URL);
        Information(GIT_COMMIT);
     SonarBegin(new SonarBeginSettings{
        Url = "",
        Key = "",
        Login = "",
        Verbose = true,
        XUnitReportsPath = XUnitReportsPath+"",
        OpenCoverReportsPath = OpenCoverReportsPath+"",
        VsCoverageReportsPath = VsCoverageReportsPath+"",
        VsTestReportsPath = VsTestReportsPath+"",
        ArgumentCustomization = args => args
                     .Append("/d:sonar.links.ci='"+CI_JOB_URL+"'")
                     .Append("/d:sonar.links.scm='"+GIT_COMMIT+"'")
     });
  });


Task("SonarEnd")
  .Does(() => {
     SonarEnd(new SonarEndSettings{
        Login = ""
     });
  });

Task("SonarQube")
  .IsDependentOn("SonarBegin")
  .IsDependentOn("SonarBuild")
  .IsDependentOn("SonarEnd");