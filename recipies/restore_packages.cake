// ---------------------------------------
// Restore packages. 
// ---------------------------------------
Task("Restore_Packages")
    .Does(() => 
{ 
     Information("Restore_Packages Task.");
     NuGetRestore(solutionPath);
});