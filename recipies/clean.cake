// ---------------------------------------
// Cleaning all created paths: 
// buildPath            : Path where the build result is located.
// artifactsPath        : Path where the artifacted are located.
// testResultsPath      : Path where the tests results are located.
// sonarqubeResultsPath : Path where the sonarqube resulsts are located.
// ---------------------------------------
Task("Clean")
    .Does(() => 
    { 
        Information("Clean Task.");
        CleanDirectory(buildPath);
        CleanDirectory(artifactsPath);
        CleanDirectory(testResultsPath);
        CleanDirectory(sonarqubeResultsPath);
    });