// ---------------------------------------
// Run unit testing analisys. 
// Variables defined:
//  - solutionPath
//  - configuration
//  - testResultsPath
// #tool nuget:?package=Microsoft.TestPlatform&version=15.8.0
// #tool nuget:?package=ReportGenerator
// ---------------------------------------

Task("VSTest_Testing")
    .Does((context) => 
{ 
    Information("Unit_Testing Task.");
    var projects = ParseSolution(solutionPath)
        .Projects
        .Where(project => project.Path.FullPath.EndsWith(".Tests.csproj", StringComparison.InvariantCultureIgnoreCase))
        .Select(project => ParseProject(project.Path, configuration));

    var targetFrameworkVersions = projects.SelectMany(project => project.TargetFrameworkVersions).Distinct();    
    foreach (var targetFrameworkVersion in targetFrameworkVersions)
    {
        var targetFrameworkVersionMoniker = targetFrameworkVersion.Replace('.', '_');

        var testResultsFilePath = testResultsPath.CombineWithFilePath($"vstest-results-{targetFrameworkVersionMoniker}.trx");
        VSTest(
            projects.SelectMany(project => project.GetAssemblyFilePaths()).Where(assemblyFilePath => assemblyFilePath.Segments.Any(segment => segment.Equals(targetFrameworkVersion, StringComparison.InvariantCultureIgnoreCase))),
            new VSTestSettings
            {
                EnableCodeCoverage = true,
                Logger = $"trx;LogFileName=\"{testResultsFilePath}\"",
                InIsolation = true,
                ArgumentCustomization = args => args.Append($"/ResultsDirectory:\"{testResultsPath}\"").Append("/Blame"),
                SettingsFile = solutionRootPath.Combine(Directory("test")).CombineWithFilePath("CodeCoverage.runsettings"),
                ToolPath = context.Tools.Resolve("vstest.console.exe"),
                EnvironmentVariables = new Dictionary<string, string>
                {
                    { "COR_ENABLE_PROFILING", "1" },
                    { "COR_PROFILER", "{9317AE81-BCD8-47B7-AAA1-A28062E41C71}" },
                    { "COR_PROFILER_PATH_32", context.Tools.Resolve("IntelliTrace/Microsoft.IntelliTrace.Profiler.dll").FullPath },
                    { "COR_PROFILER_PATH_64", context.Tools.Resolve("IntelliTrace/x64/Microsoft.IntelliTrace.Profiler.dll").FullPath },
                },
            });

        var testResults = XDocument.Load(testResultsFilePath.FullPath);
        var nmsp = (XNamespace)"http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
        var runDeploymentRoot = testResults.Root
            .Element(nmsp + "TestSettings")
                .Element(nmsp + "Deployment").Attribute("runDeploymentRoot").Value;
        var codeCoverageResults = testResults.Root
            .Element(nmsp + "ResultSummary")
                .Element(nmsp + "CollectorDataEntries")
                    .Elements(nmsp + "Collector")
                        .SingleOrDefault(e => e.Attribute("collectorDisplayName").Value == "Code Coverage")
                            .Element(nmsp + "UriAttachments")
                                .Element(nmsp + "UriAttachment")
                                    .Element(nmsp + "A").Attribute("href").Value;

        var rawCoverageResultsFilePath = testResultsPath.Combine(Directory($"{runDeploymentRoot}/In/")).CombineWithFilePath(codeCoverageResults);
        var coverageResultsFilePath = testResultsPath.CombineWithFilePath($"vscoverage-results-{targetFrameworkVersionMoniker}.xml");
        StartProcess(context.Tools.Resolve("CodeCoverage.exe"), $"analyze \"/output:{coverageResultsFilePath}\" \"{rawCoverageResultsFilePath}\"");
    }
});