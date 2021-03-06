// ---------------------------------------
// Process test results. 
// Variables defined:
//  - testResultsPath
// #tool nuget:?package=ReportGenerator
//
// ---------------------------------------
Task("Process_Reports")
    .Does(() => 
{ 
     Information("Process_Reports Task.");
     var coverageResultsFilePaths = GetFiles(testResultsPath+"/vscoverage-results-*.xml");
     ReportGenerator(
        coverageResultsFilePaths, 
        testResultsPath.Combine("VSCoverReport"),
        new ReportGeneratorSettings
        {
            ArgumentCustomization = args => args.Append("-reporttypes:HTMLInline"),
        });   

});