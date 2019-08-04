using System;
using System.Linq;
using Cake.Core;
using Cake.Core.Scripting;
using Cake.Common.IO;
using Cake.Common;
using Cake.Common.Tools.NuGet.Pack;
using Cake.Common.Tools.NuGet;
using Cake.Core.IO;
using System.Collections.Generic;
using System.Globalization;
using Cake.Common.Diagnostics;

namespace Cake.Xamarin.Build
{
    public static class XamarinBuildTasks
    {
        public static FilePath ResolveGitPath (ICakeContext cake)
        {
            var possibleEnvVariables = new [] {
                "GIT_EXE"
            };

            var possibleWinPaths = new [] {
                "C:\\Program Files (x86)\\Git\\bin\\git.exe",
            };

            var possibleUnixPaths = new [] {
                "git",
            };

            foreach (var v in possibleEnvVariables) {
                var envVar = cake.Environment.GetEnvironmentVariable (v) ?? "";

                if (!string.IsNullOrWhiteSpace (envVar) && cake.FileExists (envVar))
                    return envVar;
            }

            // Check PATH paths 
            if (cake.IsRunningOnWindows ()) {
                // Last resort try path
                var envPath = cake.Environment.GetEnvironmentVariable ("path");
                if (!string.IsNullOrWhiteSpace(envPath))
                {
                    var pathFile = envPath
                        .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(path => cake.FileSystem.GetDirectory(path))
                        .Where(path => path.Exists)
                        .Select(path => path.Path.CombineWithFilePath("git.exe"))
                        .Select(cake.FileSystem.GetFile)
                        .FirstOrDefault(file => file.Exists);

                    if (pathFile != null)
                        return pathFile.Path;
                }
            }

            if (cake.IsRunningOnUnix ()) {
                foreach (var p in possibleUnixPaths) {
                    if (cake.FileExists (p))
                        return p;
                }
            }

            if (cake.IsRunningOnWindows ()) {
                foreach (var p in possibleWinPaths) {
                    if (cake.FileExists (p))
                        return p;
                }
            }

            return null;
        }

        public static class Names
        {
            public const string LibrariesBase = "libs-base";
            public const string Libraries = "libs";

            public const string SamplesBase = "samples-base";
            public const string Samples = "samples";

            public const string NugetBase = "nuget-base";
            public const string Nuget = "nuget";

            public const string ComponentBase = "component-base";
            public const string Component = "component";

            public const string ExternalsBase = "externals-base";
            public const string Externals = "externals";

            public const string CleanBase = "clean-base";
            public const string Clean = "clean";
        }
        
        public static void SetupXamarinBuildTasks (ICakeContext cake, BuildSpec buildSpec, IReadOnlyList<Cake.Core.CakeTask> tasks, Func<string, CakeTaskBuilder> addTaskDelegate)
        {
            buildSpec.Init(cake);

            var Task = addTaskDelegate;

            Task (Names.LibrariesBase).Does (() => {
                    foreach (var l in buildSpec.Libs) {
                        l.BuildSolution ();
                        l.CopyOutput ();
                    }
                });

            if (!tasks.Any (tsk => tsk.Name == Names.Libraries))
                Task (Names.Libraries).IsDependentOn (Names.Externals).IsDependentOn (Names.LibrariesBase);
            
            Task (Names.SamplesBase).Does (() => {
                    foreach (var s in buildSpec.Samples) {
                        s.BuildSolution ();
                        s.CopyOutput ();
                    }   
                });

            if (!tasks.Any (tsk => tsk.Name == Names.Samples))
                Task (Names.Samples).IsDependentOn (Names.Libraries).IsDependentOn (Names.SamplesBase);

            Task (Names.NugetBase).Does (() => {
                PackNuGets(cake, buildSpec.NuGets);
            });

            if (!tasks.Any (tsk => tsk.Name == Names.Nuget))
                Task (Names.Nuget).IsDependentOn (Names.Libraries).IsDependentOn (Names.NugetBase);
            
            Task (Names.ComponentBase).IsDependentOn (Names.Nuget).Does (() => {
                foreach (var c in buildSpec.Components)
                {
                    if (!BuildPlatformUtil.BuildsOnCurrentPlatform (cake, c.BuildsOn))
                    {
                        cake.Warning("Component is not marked to build on current platform: {0}", c.ManifestDirectory.FullPath);
                        continue;
                    }

                    var outputDir = c.OutputDirectory ?? "./output";

                    // Clear out existing .xam files
                    if (!cake.DirectoryExists(outputDir))
                        cake.CreateDirectory(outputDir);
                    // cake.DeleteFiles(outputDir.FullPath.TrimEnd ('/') + "/*.xam");

                    var componentYaml = c.ManifestDirectory.CombineWithFilePath("component.yaml");
                    if (!cake.FileExists (componentYaml))
                    {
                        cake.Warning("Component Manifest Missing: {0}", componentYaml.FullPath);
                        continue;
                    }

                    cake.PackageComponent(c.ManifestDirectory, new XamarinComponentSettings());

                    cake.MoveFiles(c.ManifestDirectory.FullPath.TrimEnd('/') + "/*.xam", outputDir);
                }
            });

            if (!tasks.Any (tsk => tsk.Name == Names.Component))
                Task (Names.Component).IsDependentOn (Names.Nuget).IsDependentOn (Names.ComponentBase);

            Task (Names.ExternalsBase).Does (() => {
                if (buildSpec.GitRepoDependencies == null || !buildSpec.GitRepoDependencies.Any ())
                    return;

                var gitPath = ResolveGitPath (cake);
                if (gitPath == null)
                    throw new System.IO.FileNotFoundException ("Could not locate git executable");
                
                foreach (var gitDep in buildSpec.GitRepoDependencies) {
                    if (!cake.DirectoryExists (gitDep.Path))
                        cake.StartProcess (gitPath, "clone " + gitDep.Url + " " + cake.MakeAbsolute (gitDep.Path).FullPath);
                }
            });

            if (!tasks.Any (tsk => tsk.Name == Names.Externals))
                Task (Names.Externals).IsDependentOn (Names.ExternalsBase);

            Task (Names.CleanBase).Does (() => {
                cake.CleanDirectories ("./**/bin");
                cake.CleanDirectories ("./**/obj");

                if (cake.DirectoryExists ("./output"))
                    cake.DeleteDirectory ("./output", true);

                if (buildSpec.GitRepoDependencies != null && buildSpec.GitRepoDependencies.Any ()) {
                    foreach (var gitDep in buildSpec.GitRepoDependencies) {
                        if (cake.DirectoryExists (gitDep.Path))
                            cake.DeleteDirectory (gitDep.Path, true);
                    }   
                }

                if (cake.DirectoryExists ("./tools"))
                    cake.DeleteDirectory ("./tools", true);
            });

            if (!tasks.Any (tsk => tsk.Name == Names.Clean))
                Task (Names.Clean).IsDependentOn (Names.CleanBase);

            if (!tasks.Any (tsk => tsk.Name == "Default"))
                Task ("Default").IsDependentOn (Names.Libraries);
        }

        internal static void PackNuGets(ICakeContext cake, NuGetInfo[] nugets)
        {
            if (nugets == null || !nugets.Any())
                return;

            // NuGet messes up path on mac, so let's add ./ in front twice
            var basePath = cake.IsRunningOnUnix() ? "././" : "./";

            foreach (var n in nugets)
            {
                if (!BuildPlatformUtil.BuildsOnCurrentPlatform(cake, n.BuildsOn))
                {
                    cake.Warning("Nuspec is not marked to build on current platform: {0}", n.NuSpec.FullPath);
                    continue;
                }

                var outputDir = n.OutputDirectory ?? "./output";

                if (!cake.DirectoryExists(outputDir))
                    cake.CreateDirectory(outputDir);

                var settings = new NuGetPackSettings
                {
                    Verbosity = NuGetVerbosity.Detailed,
                    OutputDirectory = outputDir,
                    BasePath = basePath
                };

                if (!string.IsNullOrEmpty(n.Version))
                    settings.Version = n.Version;

                if (n.RequireLicenseAcceptance)
                    settings.RequireLicenseAcceptance = n.RequireLicenseAcceptance;

                cake.NuGetPack(n.NuSpec, settings);
            }
        }
    }
}

