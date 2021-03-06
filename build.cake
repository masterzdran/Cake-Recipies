//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////
#tool nuget:?package=Microsoft.TestPlatform&version=15.8.0
#tool nuget:?package=ReportGenerator
#tool nuget:?package=MSBuild.SonarQube.Runner.Tool
#tool "nuget:?package=xunit.runner.console&version=2.2.0"

/////////////////////////////////////////////////////////////////////
// ADDINS
//////////////////////////////////////////////////////////////////////
#addin nuget:?package=Cake.FileHelpers
#addin nuget:?package=Cake.Incubator
#addin nuget:?package=Cake.Sonar

/////////////////////////////////////////////////////////////////////
// USING DIRECTIVES
//////////////////////////////////////////////////////////////////////
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CSharp;


//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var CI_JOB_URL = Argument("CI_JOB_URL", "0");
var CI_BUILD_ID = Argument("CI_BUILD_ID", "0");
var CI_SERVER_URL = Argument("CI_SERVER_URL", "0");
var GIT_COMMIT =Argument("GIT_COMMIT", "0");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////
var solutionRootPath = MakeAbsolute(Directory("."));
var rootPath = MakeAbsolute(Directory("."));

// Root Path dependent
var sonarqubeResultsPath = rootPath.Combine(Directory(".sonarqube"));
var caketoolsPath        = rootPath.Combine(Directory("Cake_tools"));
var buildPath            = rootPath.Combine(Directory("BuildSolution"));

// Build Path dependent
var artifactsPath          = buildPath.Combine(Directory("Artifacts"));
var testResultsPath        = buildPath.Combine(Directory("TestResults"));
var codeQualityResultsPath = buildPath.Combine(Directory("CodeQualityResults"));  

// Artifact Path dependent
var OpenCoverReportsPath = artifactsPath.CombineWithFilePath("opencover-report.xml");
var XUnitReportsPath     = artifactsPath.CombineWithFilePath("xunit-test-results.xml");

// Solution Path dependent
var projectSourceRootPath = solutionRootPath.Combine("src");
var projectTestsRootPath  = solutionRootPath.Combine("test");

// Get Solution Filename
var solutionFile = GetFiles(solutionRootPath+"/*.sln").First().GetFilename();
var solutionPathArg = Argument("solutionPath", solutionFile);
var solutionPath = solutionRootPath.CombineWithFilePath(solutionFile);


/////////////////////////////////////////////////////////////////////
// Load Step Scripts
//////////////////////////////////////////////////////////////////////
#load "./recipies/clean.cake"
#load "./recipies/restore_packages.cake"
#load "./recipies/msbuild.cake"
#load "./recipies/sonarqube.cake"
#load "./recipies/vstest_testing.cake"
#load "./recipies/process_reports.cake"

//////////////////////////////////////////////////////////////////////
// SETUP
//////////////////////////////////////////////////////////////////////
Setup(context =>
{
    var msbuildToolsPath  = VSWhereProducts("*", new VSWhereProductSettings { Requires = "Microsoft.Component.MSBuild"})
        .FirstOrDefault()
        ?.CombineWithFilePath(@".\MSBuild\15.0\Bin\MSBuild.exe")
        ;

    context.Tools.RegisterFile(msbuildToolsPath);
});

//////////////////////////////////////////////////////////////////////
// Workflow
//////////////////////////////////////////////////////////////////////
Task("Default")
    .IsDependentOn("Clean")
    .IsDependentOn("restore_packages")
    .IsDependentOn("MSBuild")
    .IsDependentOn("VSTest_Testing")
    .IsDependentOn("SonarQube")
    .IsDependentOn("Process_Reports")
    .Does(()=> { 
});

//////////////////////////////////////////////////////////////////////
// Teardown
//////////////////////////////////////////////////////////////////////
Teardown(context =>
{
    Information("Executed AFTER the last task.");
});


RunTarget(target);
