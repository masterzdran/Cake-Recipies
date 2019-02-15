# Cake-Recipies
Library of Cake (https://cakebuild.net/) recipies for CI/CD integration.


build.cake has the build definition (tools and addins directives, usings, etc), defines an generic struture to use as a script for multiple (if not all) purposes. All relevant needed path should be defined in here, some of them are already defined related to the root of the solution and build paths.

Cake artefacts go to tools folder and build/code analisys/testing/whatever goes to  BuildSolution folder. Follow the structure.
./
├───BuildSolution
│   ├───Artifacts
│   └───TestResults
└───tools


The recipies are under "recipies" folder and should follow to following convention:

task_name.cake

each task file (task_name.cake) should:
* have a task with the same name of the file
* have a initial comment explaining what the task do, what are the needed variables and dependecies (tools addins).
* can have multiple task to achieve the goal. However it must have a task with the same name has the file that orchestrate all "inner task".

Take a look to the existing recipies for guidelines.

Contributions/sugestions/Improvements are welcome :-).


# Cake Basics
https://cakebuild.net/docs/tutorials/


# Cake Cheat Sheet

## Run cake script
./build.ps1

## Run cake script specific (eg: custom task named CAKE_TASK) task 
./build.ps1  -Target CAKE_TASK

## Run cake script in debug mode (quiet, minimal, normal, verbose, diagnostic).
./build.ps1  -Verbosity Diagnostic

## Run cake script and skip package restore
./build.ps1  -SkipToolPackageRestore

## Run cake script and pass custom arguments
./build.ps1  -JOB_URL="$(${env:JOB_URL})" -GIT_COMMIT="$(${env:GIT_URL})"

# Contributors
