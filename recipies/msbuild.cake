// ---------------------------------------
// Solution build. 
// Variables defined:
//  - solutionPath
//  - configuration
//  - artifactsPath
// ---------------------------------------

Task("MSBuild")
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