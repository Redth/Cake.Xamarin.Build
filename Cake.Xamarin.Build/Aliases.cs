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
        public static void SetupXamarinBuildTasks(this ICakeContext context, BuildSpec buildSpec, IReadOnlyList<Cake.Core.CakeTask> tasks, Func<string, CakeTaskBuilder<ActionTask>> addTaskDelegate)
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
        public static void SetupXamarinBuildTasks (this ICakeContext context, BuildSpec buildSpec, XamarinBuildTaskSettings settings, IReadOnlyList<Cake.Core.CakeTask> tasks, Func<string, CakeTaskBuilder<ActionTask>> addTaskDelegate)
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
    }
}

