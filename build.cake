///////////////////////////////////////////////////////////////////////////////
// TOOLS / ADDINS
///////////////////////////////////////////////////////////////////////////////

#tool paket:?package=GitVersion.CommandLine
#tool paket:?package=gitreleasemanager
#tool paket:?package=vswhere
#addin paket:?package=Cake.Figlet
#addin paket:?package=Cake.Paket

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

///////////////////////////////////////////////////////////////////////////////
// PREPARATION
///////////////////////////////////////////////////////////////////////////////

// Set build version
GitVersion(new GitVersionSettings { OutputType = GitVersionOutput.BuildServer });
GitVersion gitVersion = GitVersion(new GitVersionSettings { OutputType = GitVersionOutput.Json });

var latestInstallationPath = VSWhereProducts("*", new VSWhereProductSettings { Version = "[\"15.0\",\"16.0\"]" }).FirstOrDefault();
var msBuildPath = latestInstallationPath.CombineWithFilePath("./MSBuild/15.0/Bin/MSBuild.exe");

var local = BuildSystem.IsLocalBuild;
var isPullRequest = AppVeyor.Environment.PullRequest.IsPullRequest;
var isDevelopBranch = StringComparer.OrdinalIgnoreCase.Equals("develop", AppVeyor.Environment.Repository.Branch);
var isReleaseBranch = StringComparer.OrdinalIgnoreCase.Equals("master", AppVeyor.Environment.Repository.Branch);
var isTagged = AppVeyor.Environment.Repository.Tag.IsTag;

// Directories and Paths
var solution = "src/ControlzEx.sln";

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(ctx =>
{
    if (!IsRunningOnWindows())
    {
        throw new NotImplementedException("ControlzEx will only build on Windows because it's not possible to target WPF and Windows Forms from UNIX.");
    }

    Information("Informational   Version: {0}", gitVersion.InformationalVersion);
    Information("SemVer          Version: {0}", gitVersion.SemVer);
    Information("AssemblySemVer  Version: {0}", gitVersion.AssemblySemVer);
    Information("MajorMinorPatch Version: {0}", gitVersion.MajorMinorPatch);
    Information("NuGet           Version: {0}", gitVersion.NuGetVersion);
    Information("IsLocalBuild           : {0}", local);

    Information(Figlet("ControlzEx"));

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

Task("CleanOutput")
    //.ContinueOnError()
    .Does(() =>
{
    DeleteDirectories(GetDirectories("src/**/obj"), recursive:true);
    DeleteDirectories(GetDirectories("src/**/bin"), recursive:true);
});

Task("Restore")
    .Does(() =>
{
    PaketRestore();

    var msBuildSettings = new MSBuildSettings() { ToolPath = msBuildPath };
    MSBuild(solution, msBuildSettings.SetVerbosity(Verbosity.Normal).WithTarget("restore"));
});

Task("UpdateGlobalAssemblyInfo")
    .Does(() =>
{
	GitVersion(new GitVersionSettings { UpdateAssemblyInfo = true, UpdateAssemblyInfoFilePath = "src/GlobalAssemblyInfo.cs" });
});

Task("Build")
    .Does(() =>
{
  var msBuildSettings = new MSBuildSettings() { ToolPath = msBuildPath, ArgumentCustomization = args => args.Append("/m") };
  MSBuild(solution, msBuildSettings.SetMaxCpuCount(0)
                                   .SetVerbosity(Verbosity.Normal)
                                   //.WithRestore() only with cake 0.28.x
                                   .SetConfiguration(configuration)
                                   );
});

Task("PaketPack")
    .Does(() =>
{
    var packDestDir = "src/bin";
    PaketPack(packDestDir, new PaketPackSettings {
        Version = isReleaseBranch ? gitVersion.MajorMinorPatch : gitVersion.NuGetVersion,
        BuildConfig = configuration
        });
});

Task("Zip")
    .Does(() =>
{
    var zipDir = "src/bin/" + configuration + "/ControlzEx.Showcase";
	if (!DirectoryExists(zipDir))
	{
		Information("Could not zip any artifact! Folder doesn't exist: " + zipDir);
	}
    else
    {
        Zip(zipDir, "src/bin/ControlzEx.Showcase.v" + gitVersion.NuGetVersion + ".zip");
    }
});

Task("CreateRelease")
    .WithCriteria(() => isReleaseBranch)
    .WithCriteria(() => !isTagged)
    .Does(() =>
{
    var username = EnvironmentVariable("GITHUB_USERNAME");
    if (string.IsNullOrEmpty(username))
    {
        throw new Exception("The GITHUB_USERNAME environment variable is not defined.");
    }

    var token = EnvironmentVariable("GITHUB_TOKEN");
    if (string.IsNullOrEmpty(token))
    {
        throw new Exception("The GITHUB_TOKEN environment variable is not defined.");
    }

    GitReleaseManagerCreate(username, token, "ControlzEx", "ControlzEx", new GitReleaseManagerCreateSettings {
        Milestone         = gitVersion.MajorMinorPatch,
        Name              = gitVersion.MajorMinorPatch,
        Prerelease        = false,
        TargetCommitish   = "master",
        WorkingDirectory  = "."
    });
});

Task("Default")
    .IsDependentOn("CleanOutput")
    .IsDependentOn("Restore")
    .IsDependentOn("UpdateGlobalAssemblyInfo")
    .IsDependentOn("Build")
    .IsDependentOn("PaketPack")
    .IsDependentOn("Zip");

///////////////////////////////////////////////////////////////////////////////
// EXECUTION
///////////////////////////////////////////////////////////////////////////////

RunTarget(target);