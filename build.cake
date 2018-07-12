#tool nuget:?package=NUnit.ConsoleRunner
#addin nuget:?package=Cake.Git

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Debug");

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
		CleanDirectories("./src/**/obj");
		CleanDirectories("./src/**/bin");
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
		var buildSettings = new MSBuildSettings()
                .SetConfiguration(configuration)
                .UseToolVersion(MSBuildToolVersion.VS2017);
		
		MSBuild("./src/Jackett.sln", buildSettings);
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
		CopyFiles("./src/Jackett.Updater/bin/" + configuration + "/net452" + "/JackettUpdater.*", windowsOutput);  //builds against multiple frameworks
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

Task("Package-Files-Full-Framework-Mono")
	.IsDependentOn("Check-Packaging-Platform")
	.Does(() =>
	{
		Gzip(monoBuildFullFramework, $"./{artifactsDirName}", "Jackett", "Jackett.Binaries.Mono.tar.gz");
		Information(@"Full Framework Mono Binaries Gzip Completed");
	});

Task("Package-Full-Framework")
	.IsDependentOn("Package-Files-Full-Framework-Mono")
	.Does(() =>
	{
		Information("Full Framework Packaging Completed");
	});

Task("Kestrel-Full-Framework")
	.IsDependentOn("Package-Full-Framework")
	.Does(() =>
	{
		CleanDirectories("./src/**/obj");
		CleanDirectories("./src/**/bin");

		NuGetRestore("./src/Jackett.sln");

		var buildSettings = new MSBuildSettings()
                .SetConfiguration(configuration)
                .UseToolVersion(MSBuildToolVersion.VS2017);
		
		MSBuild("./src/Jackett.sln", buildSettings);
	});

Task("Experimental-Kestrel-Windows-Full-Framework")
	.IsDependentOn("Kestrel-Full-Framework")
	.Does(() =>
	{
		string serverProjectPath = "./src/Jackett.Server/Jackett.Server.csproj";
		string buildOutputPath = "./BuildOutput/Experimental/net461/win7-x86/Jackett";
		
		DotNetCorePublish(serverProjectPath, "net461", "win7-x86");

		CopyFiles("./src/Jackett.Service/bin/" + configuration + "/JackettService.*", buildOutputPath);
		CopyFiles("./src/Jackett.Tray/bin/" + configuration + "/JackettTray.*", buildOutputPath);
		CopyFiles("./src/Jackett.Updater/bin/" + configuration + "/net461" + "/JackettUpdater.*", buildOutputPath);  //builds against multiple frameworks

		Zip("./BuildOutput/Experimental/net461/win7-x86", $"./{artifactsDirName}/Jackett.Binaries.Windows.zip");

		//InnoSetup
		string sourceFolder = MakeAbsolute(Directory(buildOutputPath)).ToString();

		InnoSetupSettings settings = new InnoSetupSettings();
		settings.OutputDirectory = workingDir + "/" + artifactsDirName;
 		settings.Defines = new Dictionary<string, string>
			{
				{ "MyFileForVersion", sourceFolder + "/Jackett.Common.dll" },
				{ "MySourceFolder",  sourceFolder },
				{ "MyOutputFilename",  "Jackett.Installer.Windows" },
			};

		InnoSetup("./Installer.iss", settings);
	});

Task("Experimental-Kestrel-Mono-Full-Framework")
	.IsDependentOn("Kestrel-Full-Framework")
	.Does(() =>
	{
		string serverProjectPath = "./src/Jackett.Server/Jackett.Server.csproj";
		string buildOutputPath = "./BuildOutput/Experimental/net461/linux-x64/Jackett";

		DotNetCorePublish(serverProjectPath, "net461", "linux-x64");

		CopyFiles("./src/Jackett.Updater/bin/" + configuration + "/net461" + "/JackettUpdater.*", buildOutputPath);  //builds against multiple frameworks

		//There is an issue with Mono 5.8 (fixed in Mono 5.12) where its expecting to use its own patched version of System.Net.Http.dll, instead of the version supplied in folder
		//https://github.com/dotnet/corefx/issues/19914
		//https://bugzilla.xamarin.com/show_bug.cgi?id=60315
		//The workaround is to delete System.Net.Http.dll and patch the .exe.config file

		DeleteFile(buildOutputPath + "/System.Net.Http.dll");

		var configFile = File(buildOutputPath + "/JackettConsole.exe.config");
		XmlPoke(configFile, "configuration/runtime/*[name()='assemblyBinding']/*[name()='dependentAssembly']/*[name()='assemblyIdentity'][@name='System.Net.Http']/../*[name()='bindingRedirect']/@newVersion", "4.0.0.0");

		Gzip("./BuildOutput/Experimental/net461/linux-x64", $"./{artifactsDirName}", "Jackett", "Experimental.Jackett.Binaries.Mono.tar.gz");
	});
	
Task("Experimental-DotNetCore")
	.IsDependentOn("Kestrel-Full-Framework")
	.Does(() =>
	{
		string serverProjectPath = "./src/Jackett.Server/Jackett.Server.csproj";
		
		DotNetCorePublish(serverProjectPath, "netcoreapp2.1", "win-x86");
		DotNetCorePublish(serverProjectPath, "netcoreapp2.1", "linux-x64");
		DotNetCorePublish(serverProjectPath, "netcoreapp2.1", "osx-x64");

		Zip("./BuildOutput/Experimental/netcoreapp2.1/win-x86", $"./{artifactsDirName}/Experimental.netcoreapp.win-x86.zip");
		Zip("./BuildOutput/Experimental/netcoreapp2.1/osx-x64", $"./{artifactsDirName}/Experimental.netcoreapp.osx-x64.zip");
		Gzip("./BuildOutput/Experimental/netcoreapp2.1/linux-x64", $"./{artifactsDirName}", "Jackett", "Experimental.netcoreapp.linux-x64.tar.gz");
	});

Task("Experimental")
	.IsDependentOn("Experimental-Kestrel-Windows-Full-Framework")
	.IsDependentOn("Experimental-Kestrel-Mono-Full-Framework")
	//.IsDependentOn("Experimental-DotNetCore")
	.Does(() =>
	{
		Information("Experimental builds completed");
	});

Task("Appveyor-Push-Artifacts")
	.IsDependentOn("Package-Full-Framework")
	.IsDependentOn("Experimental")
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
	var cygPath = cygdriveBase + relativePath.TrimStart('.');
	return cygPath;
}

private void Gzip(string sourceFolder, string outputDirectory, string tarCdirectoryOption, string outputFileName)
{
	var cygSourcePath = RelativeWinPathToCygPath(sourceFolder);
	var tarFileName = outputFileName.Remove(outputFileName.Length - 3, 3);
	var tarArguments = @"-cvf " + cygSourcePath + "/" + tarFileName + " -C " + cygSourcePath + $" {tarCdirectoryOption} --mode ='755'";
	var gzipArguments = @"-k " + cygSourcePath + "/" + tarFileName;

	RunCygwinCommand("Tar", tarArguments);
	RunCygwinCommand("Gzip", gzipArguments);

	MoveFile($"{sourceFolder}/{tarFileName}.gz", $"{outputDirectory}/{tarFileName}.gz");
}

private void DotNetCorePublish(string projectPath, string framework, string runtime)
{
	var settings = new DotNetCorePublishSettings
		 {
			 Framework = framework,
			 Runtime = runtime,
			 OutputDirectory = $"./BuildOutput/Experimental/{framework}/{runtime}/Jackett"
		 };

		 DotNetCorePublish(projectPath, settings);
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
