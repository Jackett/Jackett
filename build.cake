#tool nuget:?package=NUnit.ConsoleRunner
#addin nuget:?package=Cake.Git

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var workingDir = MakeAbsolute(Directory("./"));
var artifactsDirName = "Artifacts";
var testResultsDirName = "TestResults";

var windowsBuildFullFramework = "./BuildOutput/FullFramework/Windows";
var monoBuildFullFramework = "./BuildOutput/FullFramework/Mono";

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Info")
	.Does(() =>
	{
		Information(@"Jackett Cake build script starting...");
		Information(@"Requires InnoSetup and C:\cygwin to be present for packaging (Pre-installed on AppVeyor)");
		Information(@"Working directory is: " + workingDir);
	});

Task("Clean")
	.IsDependentOn("Info")
	.Does(() =>
	{
		CleanDirectories("./src/**/obj" + configuration);
		CleanDirectories("./src/**/bin" + configuration);
		CleanDirectories("./BuildOutput");
		CleanDirectories("./" + artifactsDirName);
		CleanDirectories("./" + testResultsDirName);

		Information("Clean completed");
	});

Task("Restore-NuGet-Packages")
	.IsDependentOn("Clean")
	.Does(() =>
	{
		NuGetRestore("./src/Jackett.sln");
	});

Task("Build")
	.IsDependentOn("Restore-NuGet-Packages")
	.Does(() =>
	{
		MSBuild("./src/Jackett.sln", settings => settings.SetConfiguration(configuration));
	});

Task("Run-Unit-Tests")
	.IsDependentOn("Build")
	.Does(() =>
	{
		CreateDirectory("./" + testResultsDirName);
		var resultsFile = $"./{testResultsDirName}/JackettTestResult.xml";

		NUnit3("./src/**/bin/" + configuration + "/**/*.Test.dll", new NUnit3Settings
		{
			Results = new[] { new NUnit3Result { FileName = resultsFile } }
		});

		if(AppVeyor.IsRunningOnAppVeyor)
		{
			AppVeyor.UploadTestResults(resultsFile, AppVeyorTestResultsType.NUnit3);
		}
	});

Task("Copy-Files-Full-Framework")
	.IsDependentOn("Run-Unit-Tests")
	.Does(() =>
	{
		var windowsOutput = windowsBuildFullFramework + "/Jackett";

		CopyDirectory("./src/Jackett.Console/bin/" + configuration, windowsOutput);
		CopyFiles("./src/Jackett.Service/bin/" + configuration + "/JackettService.*", windowsOutput);
		CopyFiles("./src/Jackett.Tray/bin/" + configuration + "/JackettTray.*", windowsOutput);
		CopyFiles("./src/Jackett.Updater/bin/" + configuration + "/JackettUpdater.*", windowsOutput);
		CopyFiles("./Upstart.config", windowsOutput);
		CopyFiles("./LICENSE", windowsOutput);
		CopyFiles("./README.md", windowsOutput);

		var monoOutput = monoBuildFullFramework + "/Jackett";

		CopyDirectory(windowsBuildFullFramework, monoBuildFullFramework);
		DeleteFiles(monoOutput + "/JackettService.*");
		DeleteFiles(monoOutput + "/JackettTray.*");

		Information("Full framework file copy completed");
	});

Task("Check-Packaging-Platform")
	.IsDependentOn("Copy-Files-Full-Framework")
	.Does(() =>
	{
		if (IsRunningOnWindows())
		{
			CreateDirectory("./" + artifactsDirName);
			Information("Platform is Windows");
		}
		else
		{
			throw new Exception("Packaging is currently only implemented for a Windows environment");
		}
	});

Task("Package-Windows-Installer-Full-Framework")
	.IsDependentOn("Check-Packaging-Platform")
	.Does(() =>
	{
		InnoSetup("./Installer.iss", new InnoSetupSettings {
			OutputDirectory = workingDir + "/" + artifactsDirName
		});
	});

Task("Package-Files-Full-Framework-Windows")
	.IsDependentOn("Check-Packaging-Platform")
	.Does(() =>
	{
		Zip(windowsBuildFullFramework, $"./{artifactsDirName}/Jackett.Binaries.Windows.zip");
		Information(@"Full Framework Windows Binaries Zipping Completed");
	});

Task("Package-Files-Full-Framework-Mono")
	.IsDependentOn("Check-Packaging-Platform")
	.Does(() =>
	{
		var cygMonoBuildPath = RelativeWinPathToCygPath(monoBuildFullFramework);
		var tarFileName = "Jackett.Binaries.Mono.tar";
		var tarArguments = @"-cvf " + cygMonoBuildPath + "/" + tarFileName + " -C " + cygMonoBuildPath + " Jackett --mode ='755'";
		var gzipArguments = @"-k " + cygMonoBuildPath + "/" + tarFileName;

		RunCygwinCommand("Tar", tarArguments);
		RunCygwinCommand("Gzip", gzipArguments);

		MoveFile($"{monoBuildFullFramework}/{tarFileName}.gz", $"./{artifactsDirName}/{tarFileName}.gz");
	});

Task("Package-Full-Framework")
	.IsDependentOn("Package-Windows-Installer-Full-Framework")
	.IsDependentOn("Package-Files-Full-Framework-Windows")
	.IsDependentOn("Package-Files-Full-Framework-Mono")
	.Does(() =>
	{
		Information("Full Framwork Packaging Completed");
	});

Task("Appveyor-Push-Artifacts")
	.IsDependentOn("Package-Full-Framework")
	.Does(() =>
	{
		if (AppVeyor.IsRunningOnAppVeyor)
		{
			foreach (var file in GetFiles(workingDir + $"/{artifactsDirName}/*"))
			{
				AppVeyor.UploadArtifact(file.FullPath);
			}
		}
		else
		{
			Information(@"Skipping as not running in AppVeyor Environment");
		}
	});

Task("Release-Notes")
	.IsDependentOn("Appveyor-Push-Artifacts")
	.Does(() =>
	{
		string latestTag = GitDescribe(".", false, GitDescribeStrategy.Tags, 0);
		Information($"Latest tag is: {latestTag}" + Environment.NewLine);

		List<GitCommit> relevantCommits = new List<GitCommit>();

		var commitCollection = GitLog("./", 50);

		foreach(GitCommit commit in commitCollection)
		{
			var commitTag = GitDescribe(".", commit.Sha, false, GitDescribeStrategy.Tags, 0);

			if (commitTag == latestTag)
			{
				relevantCommits.Add(commit);
			}
			else
			{
				break;
			}
		}

		relevantCommits = relevantCommits.AsEnumerable().Reverse().Skip(1).ToList();

		if (relevantCommits.Count() > 0)
		{
			List<string> notesList = new List<string>();
				
			foreach(GitCommit commit in relevantCommits)
			{
				notesList.Add($"{commit.MessageShort} (Thank you @{commit.Author.Name})");
			}

			string buildNote = String.Join(Environment.NewLine, notesList);
			Information(buildNote);

			System.IO.File.WriteAllLines(workingDir + "\\BuildOutput\\ReleaseNotes.txt", notesList.ToArray());
		}
		else
		{
			Information($"No commit messages found to create release notes");
		}

	});


private void RunCygwinCommand(string utility, string utilityArguments)
{
	var cygwinDir = @"C:\cygwin\bin\";
	var utilityProcess = cygwinDir + utility + ".exe";

	Information("CygWin Utility: " + utility);
	Information("CygWin Directory: " + cygwinDir);
	Information("Utility Location: " + utilityProcess);
	Information("Utility Arguments: " + utilityArguments);

	IEnumerable<string> redirectedStandardOutput;
	IEnumerable<string> redirectedErrorOutput;
	var exitCodeWithArgument =
		StartProcess(
			utilityProcess,
			new ProcessSettings {
				Arguments = utilityArguments,
				WorkingDirectory = cygwinDir,
				RedirectStandardOutput = true
			},
			out redirectedStandardOutput,
			out redirectedErrorOutput
		);

	Information(utility + " output:" + Environment.NewLine + string.Join(Environment.NewLine, redirectedStandardOutput.ToArray()));

	// Throw exception if anything was written to the standard error.
	if (redirectedErrorOutput != null && redirectedErrorOutput.Any())
	{
		throw new Exception(
			string.Format(
				utility + " Errors ocurred: {0}",
				string.Join(", ", redirectedErrorOutput)));
	}

	Information(utility + " Exit code: {0}", exitCodeWithArgument);
}

private string RelativeWinPathToCygPath(string relativePath)
{
	var cygdriveBase = "/cygdrive/" + workingDir.ToString().Replace(":", "").Replace("\\", "/");
	var cygPath = cygdriveBase + relativePath.Replace(".", "");
	return cygPath;
}

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
	.IsDependentOn("Release-Notes")
	.Does(() =>
	{
		Information("Default Task Completed");
	});

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
