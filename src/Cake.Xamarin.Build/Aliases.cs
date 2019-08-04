using Cake.Core.Annotations;
using Cake.Core;
using Cake.Core.Scripting;
using System.Collections.Generic;
using System;
using Cake.Common.Diagnostics;
using Cake.Common;
using Cake.Core.IO;
using System.Linq;
using Cake.Common.IO;
using Cake.XCode;
using System.Text;
using Cake.CocoaPods;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Cake.Xamarin.Build
{
    /// <summary>
    /// Xamarin Build Task aliases.
    /// </summary>
    [CakeNamespaceImport("Cake.Xamarin.Build")]
    [CakeAliasCategory("Xamarin Build Tasks")]
    public static class XamarinBuildTasksAliases
    {
        public class XamarinBuildTaskSettings
        {
            public XamarinBuildTaskSettings ()
            {
                LogEnvironmentVariables = true;
            }

            public bool LogEnvironmentVariables { get; set; }
        }

        /// <summary>
        /// Creates and/or sets up Task dependency chain for Xamarin related build tasks
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="buildSpec">The build spec info to setup with</param>
        /// <param name="tasks">The currently executing cake script's Tasks list</param>
        /// <param name="addTaskDelegate">The delegate used to add a new Task to the currently executing cake script</param>
        [CakeMethodAlias]
        public static void SetupXamarinBuildTasks(this ICakeContext context, BuildSpec buildSpec, IReadOnlyList<Cake.Core.CakeTask> tasks, Func<string, CakeTaskBuilder> addTaskDelegate)
        {
            SetupXamarinBuildTasks(context, buildSpec, new XamarinBuildTaskSettings(), tasks, addTaskDelegate);
        }

        /// <summary>
        /// Creates and/or sets up Task dependency chain for Xamarin related build tasks
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="buildSpec">The build spec info to setup with</param>
        /// <param name="settings">The settings to use for setting up the build tasks</param>
        /// <param name="tasks">The currently executing cake script's Tasks list</param>
        /// <param name="addTaskDelegate">The delegate used to add a new Task to the currently executing cake script</param>        [CakeMethodAlias]
        public static void SetupXamarinBuildTasks (this ICakeContext context, BuildSpec buildSpec, XamarinBuildTaskSettings settings, IReadOnlyList<Cake.Core.CakeTask> tasks, Func<string, CakeTaskBuilder> addTaskDelegate)
        {
            if (settings.LogEnvironmentVariables)
            {
                context.Information("Environment Variables:");
                foreach (var envVar in context.EnvironmentVariables())
                {
                    context.Information("\tKey: {0}\tValue: \"{1}\"", envVar.Key, envVar.Value);
                }
            }

            XamarinBuildTasks.SetupXamarinBuildTasks (context, buildSpec, tasks, addTaskDelegate);
        }

        /// <summary>
        /// Runs a make file on non-windows systems
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="directory">Directory where makefile exists</param>
        /// <param name="target">The target to run in the makefile (Default is all)</param>
        [CakeMethodAlias]
        public static void RunMake (this ICakeContext context, DirectoryPath directory, string target = "all")
        {
            if (!context.IsRunningOnUnix ())
            {
                context.Warning("{0} is not available on the current platform", "make");
                return;
            }

            context.StartProcess("make", new ProcessSettings
            {
                Arguments = target,
                WorkingDirectory = directory,
            });
        }

        /// <summary>
        /// Runs lipo -create to merge multiple input static libs into one fat file on Mac
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="directory">The working directory.</param>
        /// <param name="output">The output fat file.</param>
        /// <param name="inputs">The individual architecture static library files.</param>
        [CakeMethodAlias]
        public static void RunLipoCreate(this ICakeContext context, DirectoryPath workingDirectory, FilePath output, params FilePath[] inputs)
        {
            if (!context.IsRunningOnUnix())
            {
                context.Warning("{0} is not available on the current platform.", "lipo");
                return;
            }

            var inputString = string.Join(" ", inputs.Select(i => string.Format("\"{0}\"", i)));
            context.StartProcess("lipo", new ProcessSettings
            {
                Arguments = string.Format("-create -output \"{0}\" {1}", output, inputString),
                WorkingDirectory = workingDirectory,
            });
        }

        /// <summary>
        /// Runs libtool -static
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="directory">The working directory.</param>
        /// <param name="output">The output file.</param>
        /// <param name="inputs">The input files.</param>
        [CakeMethodAlias]
        public static void RunLibtoolStatic (this ICakeContext context, DirectoryPath directory, FilePath output, params FilePath[] inputs)
        {
            if (!context.IsRunningOnUnix())
            {
                context.Warning("{0} is not available on the current platform.", "libtool");
                return;
            }

            var inputString = string.Join(" ", inputs.Select(i => string.Format("\"{0}\"", i)));
            context.StartProcess("libtool", new ProcessSettings
            {
                Arguments = string.Format("-static -o \"{0}\" {1}", output, inputString),
                WorkingDirectory = directory,
            });
        }

        /// <summary>
        /// Builds an XCode project with xcodebuild and combines architectures into a fat library
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="xcodeProject">The xcodeproj to build</param>
        /// <param name="target">The xcode project target to build</param>
        /// <param name="libraryTitle">Optional library name</param>
        /// <param name="fatLibrary">Optional output fat library name</param>
        /// <param name="workingDirectory">Optional working directory</param>
        [CakeMethodAlias]
        public static void BuildXCodeFatLibrary(this ICakeContext context, FilePath xcodeProject, string target, string libraryTitle = null, FilePath fatLibrary = null, DirectoryPath workingDirectory = null)
        {
            if (!context.IsRunningOnUnix())
            {
                context.Warning("{0} is not available on the current platform.", "xcodebuild");
                return;
            }

            libraryTitle = libraryTitle ?? target;
            fatLibrary = fatLibrary ?? string.Format("lib{0}.a", libraryTitle);
            workingDirectory = workingDirectory ?? context.Directory("./externals/");

            var output = string.Format("lib{0}.a", libraryTitle);
            var i386 = string.Format("lib{0}-i386.a", libraryTitle);
            var x86_64 = string.Format("lib{0}-x86_64.a", libraryTitle);
            var armv7 = string.Format("lib{0}-armv7.a", libraryTitle);
            var armv7s = string.Format("lib{0}-armv7s.a", libraryTitle);
            var arm64 = string.Format("lib{0}-arm64.a", libraryTitle);

            var buildArch = new Action<string, string, FilePath>((sdk, arch, dest) => {
                if (!context.FileExists(dest))
                {
                    context.XCodeBuild(new XCodeBuildSettings
                    {
                        Project = workingDirectory.CombineWithFilePath(xcodeProject).ToString(),
                        Target = target,
                        Sdk = sdk,
                        Arch = arch,
                        Configuration = "Release",
                    });
                    var outputPath = workingDirectory.Combine("build").Combine("Release-" + sdk).CombineWithFilePath(output);
                    context.CopyFile(outputPath, dest);
                }
            });

            buildArch("iphonesimulator", "i386", workingDirectory.CombineWithFilePath(i386));
            buildArch("iphonesimulator", "x86_64", workingDirectory.CombineWithFilePath(x86_64));

            buildArch("iphoneos", "armv7", workingDirectory.CombineWithFilePath(armv7));
            buildArch("iphoneos", "armv7s", workingDirectory.CombineWithFilePath(armv7s));
            buildArch("iphoneos", "arm64", workingDirectory.CombineWithFilePath(arm64));

            RunLipoCreate(context, workingDirectory, fatLibrary, i386, x86_64, armv7, armv7s, arm64);
        }

        public static void BuildXCodeUniversalFramework (this ICakeContext context, FilePath xcodeProject, string target, string config = "Release")
        {
            var targetsSdks = new Dictionary<string, string> {
                { "armv7", "iphoneos" },
                { "armv7s", "iphoneos" },
                { "arm64", "iphoneos" },
                { "i386", "iphonesimulator" },
                { "x86_64", "iphonesimulator" },
            };

            var projectDir = xcodeProject.GetDirectory ();
            var libsToLipo = new List<FilePath> ();

            // We need to build for all the target arch's and sdk's
            foreach (var targetSdk in targetsSdks) {
                // Build the framework
                context.XCodeBuild (new XCodeBuildSettings {
                    Project = xcodeProject.MakeAbsolute (context.Environment).FullPath,
                    Target = target,
                    Sdk = targetSdk.Value,
                    Arch = targetSdk.Key,
                    Configuration = config,
                });

                // Get the path to the built static library inside the framework
                var staticLib = projectDir
                    .Combine ("build")
                    .Combine (config + "-" + targetSdk.Value)
                    .Combine (target)
                    .Combine (target + ".framework")
                    .CombineWithFilePath (target);

                var libToLipo = projectDir.CombineWithFilePath ("lib" + target + "-" + targetSdk.Key + ".a");

                // Move the built static lib to a place where we can lipo it later
                context.MoveFile (
                    staticLib, 
                    libToLipo);

                libsToLipo.Add (libToLipo);
            }

            // Make sure the universal target dir exists
            context.EnsureDirectoryExists (projectDir.Combine ("build").Combine ("universal"));

            // Copy the framework contents from iphoneos to the universal folder 
            // This gets us all the header files and such
            context.CopyDirectory (projectDir.Combine ("build").Combine (config + "-iphoneos"),
                                   projectDir.Combine ("build").Combine ("universal"));

            // Lipo the separate static libs into one universal static lib, inside the universal framework
            context.RunLipoCreate (
                projectDir,
                projectDir.Combine ("build")
                    .Combine ("universal")
                    .Combine (target)
                    .Combine (target + ".framework")
                    .CombineWithFilePath (target),
                libsToLipo.ToArray ());
        }

        /// <summary>
        /// Cleans an xcodeproj build, and removes any *.a static libraries
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="projectRoot">Optional project root path.</param>
        /// <param name="workingDirectory">Optional working directory.</param>
        [CakeMethodAlias]
        public static void CleanXCodeBuild(this ICakeContext context, DirectoryPath projectRoot = null, DirectoryPath workingDirectory = null)
        {
            if (!context.IsRunningOnUnix())
            {
                context.Warning("{0} is not available on the current platform.", "xcodebuild");
                return;
            }

            workingDirectory = workingDirectory ?? context.Directory("./externals/");
            projectRoot = projectRoot ?? workingDirectory;

            if (context.DirectoryExists(workingDirectory.Combine("build")))
                context.DeleteDirectory(workingDirectory.Combine("build"), true);

            if (context.DirectoryExists(workingDirectory.Combine(projectRoot)))
                context.DeleteDirectory(workingDirectory.Combine(projectRoot), true);

            context.DeleteFiles(System.IO.Path.Combine(workingDirectory.ToString(), "*.a"));
        }

        /// <summary>
        /// Creates a Podfile
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="podfilePath">The Podfile filename to create</param>
        /// <param name="platform">The platform to specify inside the Podfile</param>
        /// <param name="platformVersion">The platform version to specify inside the Podfile</param>
        /// <param name="pods">Key/Value pairs of Pod ID and Version to include in the Podfile</param>
        [CakeMethodAlias]
        public static void CreatePodfile(this ICakeContext context, DirectoryPath podfilePath, string platform, string platformVersion, IDictionary<string, string> pods)
        {
            if (!context.IsRunningOnUnix())
            {
                context.Warning("{0} is not available on the current platform.", "pod");
                return;
            }

            var builder = new StringBuilder();
            builder.AppendFormat("platform :{0}, '{1}'", platform, platformVersion);
            builder.AppendLine();
            foreach (var pod in pods)
            {
                builder.AppendFormat("pod '{0}', '{1}'", pod.Key, pod.Value);
                builder.AppendLine();
            }

            if (!context.DirectoryExists(podfilePath))
            {
                context.CreateDirectory(podfilePath);
            }

            System.IO.File.WriteAllText(podfilePath.CombineWithFilePath("Podfile").ToString(), builder.ToString());
        }

        /// <summary>
        /// Creates a Podfile and Installs CocoaPods
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="podfilePath">The Podfile filename to create.</param>
        /// <param name="platform">The platform to specify inside the Podfile</param>
        /// <param name="platformVersion">The platform version to specify inside the Podfile</param>
        /// <param name="pods">Key/Value pairs of Pod ID and Version to include in the Podfile</param>
        [CakeMethodAlias]
        public static void InstallCocoaPods(this ICakeContext context, DirectoryPath podfilePath, string platform, string platformVersion, IDictionary<string, string> pods)
        {
            if (!context.IsRunningOnUnix())
            {
                context.Warning("{0} is not available on the current platform.", "pod");
                return;
            }

            CreatePodfile(context, podfilePath, platform, platformVersion, pods);

            context.CocoaPodInstall(podfilePath, new CocoaPodInstallSettings
            {
                NoIntegrate = true
            });
        }

        /// <summary>
        /// Downloads a file
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="url">The url to download.</param>
        /// <param name="downloadTo">The file to download to.</param>
        /// <param name="settings">The download settings.</param>
        [CakeMethodAlias]
        public static void DownloadFile (this ICakeContext context, string url, FilePath downloadTo, DownloadFileSettings settings)
        {
            using (var http = new HttpClient())
            {
                if (settings != null && !string.IsNullOrEmpty(settings.UserAgent))
                    http.DefaultRequestHeaders.Add("User-Agent", settings.UserAgent);

                var progress = new HttpClientExtensions.DownloadFileProgressReporter (context);

                http.DownloadFileAsync(new Uri(url), downloadTo.MakeAbsolute(context.Environment).FullPath, progress).Wait();
            }
        }

        /// <summary>
        /// Packages the given NuGet (nuspec) information items
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="nugets">The nugets to pack.</param>
        [CakeMethodAlias]
        public static void PackNuGets (this ICakeContext context, params NuGetInfo[] nugets)
        {
            XamarinBuildTasks.PackNuGets(context, nugets);
        }

		/// <summary>
		/// Gets System Information, versions of tools, etc.
		/// </summary>
		/// <returns>The system info.</returns>
		/// <param name="context">Context.</param>
        [CakeMethodAlias]
        public static SystemInfo GetSystemInfo(this ICakeContext context)
        {
            var info = new SystemInfo();

            info.XCodeVersion = GetXCodeVersion(context);
            info.CocoaPodsVersion = GetCocoaPodsVersion(context);
            info.XamarinAndroidVersion = GetXamarinAndroidVersion(context);
            info.XamariniOSVersion = GetXamariniOSVersion(context);
            info.OperatingSystem = GetOperatingSystem(context);
			info.OperatingSystemName = GetOperatingSystemName(context);
            info.OperatingSystemVersion = GetOperatingSystemVersion(context);
			info.DotNetCoreVersions = context.GetDotNetCoreVersions();

			if (info.OperatingSystem == PlatformFamily.Windows)
				info.VisualStudioInstalls = GetVisualStudioInstalls(context);
			else
				info.VisualStudioInstalls = new List<VisualStudioInfo>();

            return info;
        }

		/// <summary>
		/// Gets the visual studio installs.
		/// </summary>
		/// <returns>The visual studio installs.</returns>
		/// <param name="context">Context.</param>
		[CakeMethodAlias]
		public static List<VisualStudioInfo> GetVisualStudioInstalls(this ICakeContext context)
		{
			var tool = new VSWhereTool(context.FileSystem, context.Environment, context.ProcessRunner, context.Tools);
			return tool.GetVisualStudioInstalls(null);
		}

		/// <summary>
		/// Gets the visual studio installs.
		/// </summary>
		/// <returns>The visual studio installs.</returns>
		/// <param name="context">Context.</param>
		/// <param name="settings">Settings.</param>
		[CakeMethodAlias]
		public static List<VisualStudioInfo> GetVisualStudioInstalls(this ICakeContext context, VSWhereToolSettings settings = null)
		{
			var tool = new VSWhereTool(context.FileSystem, context.Environment, context.ProcessRunner, context.Tools);
			return tool.GetVisualStudioInstalls(settings);
		}

		[CakeMethodAlias]
		public static DotNetCoreVersionInfo GetDotNetCoreVersions (this ICakeContext context, DotNetCoreVersionToolSettings settings = null)
		{
			var tool = new DotNetCoreVersionTool(context.FileSystem, context.Environment, context.ProcessRunner, context.Tools);
			return tool.GetDotNetCoreVersions(settings);
		}

		/// <summary>
		/// Logs System Information
		/// </summary>
		/// <param name="context">Context.</param>
		[CakeMethodAlias]
		public static void LogSystemInfo(this ICakeContext context)
		{
			var info = GetSystemInfo(context);

			logInfo(context, "------------------------------");
			logInfo(context, "      System Information      ");
			logInfo(context, "------------------------------");


			logInfo(context, "Operating System:      {0}", info.OperatingSystemName);
			logInfo(context, "OS Version:            {0}\r\n", info.OperatingSystemVersion);

			logInfo(context, "Xamarin.Android:       {0}", info.XamarinAndroidVersion ?? "Not Detected");
			logInfo(context, "Xamarin.iOS:           {0}\r\n", info.XamariniOSVersion ?? "Not Detected");

			if (info.OperatingSystem == PlatformFamily.OSX)
			{
				logInfo(context, "XCode Version:         {0}", info.XCodeVersion ?? "Not Detected");
				logInfo(context, "CocoaPods Version:     {0}", info.CocoaPodsVersion ?? "Not Detected");
			}

			if (info.DotNetCoreVersions != null)
			{
				if (info.DotNetCoreVersions.RuntimeVersions.Any()) {
					logInfo(context, ".NET Core Runtime Versions:");
					foreach (var ver in info.DotNetCoreVersions.RuntimeVersions)
						logInfo(context, "    {0}", ver);
				}

				if (info.DotNetCoreVersions.SdkVersions.Any())
				{
					logInfo(context, ".NET Core SDK Versions:");
					foreach (var ver in info.DotNetCoreVersions.SdkVersions)
						logInfo(context, "    {0}", ver);
				}
			}

			if (info.OperatingSystem == PlatformFamily.Windows)
			{
				if (info.VisualStudioInstalls != null && info.VisualStudioInstalls.Any())
				{
					foreach (var vs in info.VisualStudioInstalls)
					{
						logInfo(context, "{0}", vs.DisplayName);
						logInfo(context, "         {0}", vs.InstallationVersion);
						logInfo(context, "         {0}", vs.InstallationPath);
						logInfo(context, "         {0}", vs.InstallDate);
					}
				}
				else
				{
					logInfo(context, "{0} not installed", "Visual Studio");
				}
			}


			logInfo(context, "------------------------------");
		}

		static void logInfo(ICakeContext context, string format, params object[] args)
		{
			context.Log.Write(Core.Diagnostics.Verbosity.Normal, Core.Diagnostics.LogLevel.Information,format, args);
		}

		static void logInfo(ICakeContext context, string msg)
		{
			context.Log.Write(Core.Diagnostics.Verbosity.Normal, Core.Diagnostics.LogLevel.Information, msg);
		}


        static string GetXCodeVersion(this ICakeContext context)
        {
            if (context.IsRunningOnWindows())
                return null;

            var firstLine = RunExternalProcess(context, "/usr/bin/xcodebuild", "-version")?.FirstOrDefault()?.Trim ();

            return Regex.Split(firstLine ?? string.Empty, "\\s+")
                        ?.Skip(1)?.FirstOrDefault();
        }

        static string GetCocoaPodsVersion(this ICakeContext context)
        {
            if (context.IsRunningOnWindows())
                return null;

            return RunExternalProcess(context, "pod", "--version")?.FirstOrDefault()?.Trim ();
        }


        static string GetMacOSVersion(this ICakeContext context)
        {
            if (context.IsRunningOnWindows())
                return null;

            var productVersion = RunExternalProcess(context, "/usr/bin/sw_vers", "-productVersion")?.FirstOrDefault()?.Trim ();
            var buildVersion = RunExternalProcess(context, "/usr/bin/sw_vers", "-buildVersion")?.FirstOrDefault()?.Trim ();

            if (productVersion == null && buildVersion == null)
                return null;

            return productVersion ?? "?" + "." + buildVersion ?? "?";
        }

        static string GetOperatingSystemName(this ICakeContext context)
        {
			var pf = GetOperatingSystem(context);

			switch (pf)
			{
				case PlatformFamily.Linux:
					return "Linux";
				case PlatformFamily.OSX:
					return "MacOS";
				case PlatformFamily.Windows:
					return "Windows";
			}
            return "Unix";
        }


		/// <summary>
		/// Gets the type of Operating System
		/// </summary>
		/// <returns>The operating system.</returns>
		/// <param name="context">Context.</param>
		[CakeMethodAlias]
		public static PlatformFamily GetOperatingSystem(this ICakeContext context)
		{
			try
			{
				if (NativeHelpers.IsRunningOnMac())
					return PlatformFamily.OSX;
			}
			catch { }
			
			return context.Environment.Platform.Family;
		}

        static string GetOperatingSystemVersion(this ICakeContext context)
        {
            if (context.IsRunningOnWindows())
                return GetWindowsVersion(context);

			if (GetOperatingSystem(context) == PlatformFamily.OSX)
				return GetMacOSVersion(context);

            return System.Environment.OSVersion.Version.ToString();
        }


        static string GetWindowsVersion(this ICakeContext context)
        {
            if (!context.IsRunningOnWindows())
                return null;

            return System.Environment.OSVersion.Version.ToString();
        }


        static string GetXamarinAndroidVersion(this ICakeContext context)
        {
            var versionFile = "/Library/Frameworks/Xamarin.Android.framework/Versions/Current/Version";
            var revisionFile = "/Library/Frameworks/Xamarin.Android.framework/Versions/Current/Version.rev";

            if (context.IsRunningOnWindows ())
            {
                versionFile = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86), "MSBuild", "Xamarin", "Android", "Version");
                revisionFile = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86), "MSBuild", "Xamarin", "Android", "Version.rev");
            }

			string version = null;
			if (System.IO.File.Exists (versionFile))
				version = System.IO.File.ReadAllText(versionFile)?.Trim();
			string revision = null;
			if (System.IO.File.Exists (revisionFile))
				revision = System.IO.File.ReadAllText(revisionFile)?.Trim();

            if (version == null && revision == null)
                return null;
            
            return version ?? "?" + "." + revision ?? "?";
        }

        static string GetXamariniOSVersion(this ICakeContext context)
        {
            var versionFile = "/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/Version";

            if (context.IsRunningOnWindows())
                versionFile = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86), "MSBuild", "Xamarin", "Android", "Version");

			if (!System.IO.File.Exists(versionFile))
				return null;
			
            var version = System.IO.File.ReadAllText(versionFile)?.Trim();

            return version;
        }

		static IEnumerable<string> RunExternalProcess(ICakeContext context, FilePath processPath, params string[] args)
        {
            var pab = new ProcessArgumentBuilder();

            foreach (var a in args)
                pab.Append(a);

            IEnumerable<string> procOutput = new List<string>();

            context.StartProcess(processPath, new ProcessSettings
            {
                Arguments = pab,
                RedirectStandardOutput = true,
            }, out procOutput);

            return procOutput;
        }

		///// <summary>
		///// Finds information about every entry in a given zip file, including file offsets within the zip
		///// </summary>
		///// <param name="context">Context.</param>
		///// <param name="zipFile">Zip file.</param>
		//[CakeMethodAlias]
		//public static List<ZipEntryInfo> FindZipEntries(this ICakeContext context, FilePath zipFile)
		//{
		//	var zipFilename = zipFile.MakeAbsolute(context.Environment).FullPath;

		//	return PartialZip.FindZipFileRanges(zipFilename);
		//}


		///// <summary>
		///// Reads the context of an entry in a zip file as a string
		///// </summary>
		///// <param name="context">Context.</param>
		///// <param name="zipFile">Zip file.</param>
		///// <param name="zipEntryName">Entry in zip file to read.</param>
		///// <param name="readBinaryAsHex">If true, return the string as a hexadecimal representation of the raw file data.</param>
		//[CakeMethodAlias]
		//public static string ReadZipEntryText(this ICakeContext context, FilePath zipFile, string zipEntryName, bool readBinaryAsHex = false)
		//{
		//	var zipFilename = zipFile.MakeAbsolute(context.Environment).FullPath;

		//	return PartialZip.ReadEntryText(zipFilename, zipEntryName, readBinaryAsHex);
		//}


		[CakeMethodAlias]
		public static void FixAndroidAarFile(this ICakeContext context, FilePath aarFile, string artifactId, bool fixManifestPackageNames = true, bool extractProguardConfigs = true)
		{
			AndroidAarFixer.FixAarFile(aarFile.MakeAbsolute(context.Environment).FullPath, artifactId, fixManifestPackageNames, extractProguardConfigs);
		}

		[CakeMethodAlias]
		public static void RunCakeBuilds (this ICakeContext context, Dictionary<FilePath, string[]> scriptsAndTargets, DirectoryPath testResultsDir = null)
		{
			const string EV_CAKE_EXE_PATH = "CAKE_EXE_PATH";
			const string EV_CAKE_BUILD_INFO_PATH = "CAKE_BUILD_INFO_PATH";


			// Get cake.exe path, set to environment variable
			// create temp file with scripts and targets format
			// set temp file to env. variable
			// run nunit
			if (testResultsDir == null)
				testResultsDir = new DirectoryPath("./");

			// Find path to cake.exe that we can set for the runner to use for invoking the script
			//var p = System.Diagnostics.Process.GetCurrentProcess();
			//var cakeExePath = new FilePath(p.Modules[0].FileName);

			var cakeExePath = context.GetFiles("./**/Cake.exe").FirstOrDefault();

			// Get our .dll containing the tests, in this case it's this assembly
			var testFile = context.GetFiles("./**/Cake.Xamarin.Build.CakeBuilder.dll").FirstOrDefault();

			//var testFile = new FilePath (System.Reflection.Assembly.GetCallingAssembly().Location);

			// Temp file to save the build info to
			var buildInfoPath = new FilePath("./cakebuildinfo-" + Guid.NewGuid().ToString() + ".txt").MakeAbsolute(context.Environment).FullPath;
			// Write out the build scripts and targets in the format the test case source expects
			var buildInfo = string.Empty;
			foreach (var kvp in scriptsAndTargets)
				buildInfo += kvp.Key.MakeAbsolute(context.Environment) + ":" + string.Join(",", kvp.Value) + Environment.NewLine;
			System.IO.File.WriteAllText(buildInfoPath, buildInfo);

			context.Information("Cake.exe Path: {0}", cakeExePath);
			context.Information("Build Info Path: {0}", buildInfoPath);

			// Run NUnit
			Cake.Common.Tools.XUnit.XUnit2Aliases.XUnit2(context, new[] { testFile }, new Common.Tools.XUnit.XUnit2Settings
			{
				NUnitReport = true,
				EnvironmentVariables = new Dictionary<string, string> {
					{ EV_CAKE_EXE_PATH, cakeExePath.MakeAbsolute(context.Environment).FullPath },
					{ EV_CAKE_BUILD_INFO_PATH, buildInfoPath },
				},
				OutputDirectory = testResultsDir
			});

			context.DeleteFile(new FilePath(buildInfoPath));
		}
	}
}
