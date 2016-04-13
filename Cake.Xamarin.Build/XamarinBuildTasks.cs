using System;
using System.Linq;
using Cake.Core;
using Cake.Core.Scripting;
using Cake.Common.IO;
using Cake.Common;
using Cake.Common.Tools.NuGet.Pack;
using Cake.Common.Tools.NuGet;
using Cake.Core.IO;

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
                        .Select(path => path.Path.CombineWithFilePath("choco.exe"))
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

        public static void SetupXamarinBuildTasks (IScriptHost scriptHost)
        {
            var cake = scriptHost.Context;

            scriptHost.Task (Names.LibrariesBase).Does (() => {
                    foreach (var l in CakeSpec.Libs) {
                        l.BuildSolution ();
                        l.CopyOutput ();
                    }
                });

            if (!scriptHost.Tasks.Any (tsk => tsk.Name == Names.Libraries))
                scriptHost.Task (Names.Libraries).IsDependentOn (Names.Externals).IsDependentOn (Names.LibrariesBase);
            
            scriptHost.Task (Names.SamplesBase).Does (() => {
                    foreach (var s in CakeSpec.Samples) {
                        s.BuildSolution ();
                        s.CopyOutput ();
                    }   
                });

            if (!scriptHost.Tasks.Any (tsk => tsk.Name == Names.Samples))
                scriptHost.Task (Names.Samples).IsDependentOn (Names.Libraries).IsDependentOn (Names.SamplesBase);

            scriptHost.Task (Names.NugetBase).Does (() => {
                var outputPath = "./output";

                if (CakeSpec.NuGets == null || !CakeSpec.NuGets.Any ())
                    return;

                // NuGet messes up path on mac, so let's add ./ in front twice
                var basePath = cake.IsRunningOnUnix () ? "././" : "./";

                if (!cake.DirectoryExists (outputPath)) {
                    cake.CreateDirectory (outputPath);
                }

                foreach (var n in CakeSpec.NuGets) {
                    var settings = new NuGetPackSettings { 
                        Verbosity = NuGetVerbosity.Detailed,
                        OutputDirectory = outputPath,       
                        BasePath = basePath
                    };

                    if (!string.IsNullOrEmpty (n.Version))
                        settings.Version = n.Version;

                    if (n.RequireLicenseAcceptance)
                        settings.RequireLicenseAcceptance = n.RequireLicenseAcceptance;

                    cake.NuGetPack (n.NuSpec, settings);
                }
            });

            if (!scriptHost.Tasks.Any (tsk => tsk.Name == Names.Nuget))
                scriptHost.Task (Names.Nuget).IsDependentOn (Names.Libraries).IsDependentOn (Names.NugetBase);
            
            scriptHost.Task (Names.ComponentBase).IsDependentOn (Names.Nuget).Does (() => {
                // Clear out existing .xam files
                if (!cake.DirectoryExists ("./output/"))
                    cake.CreateDirectory ("./output/");
                cake.DeleteFiles ("./output/*.xam");

                // Look for all the component.yaml files to build
                var componentYamls = cake.GetFiles ("./**/component.yaml");
                foreach (var yaml in componentYamls) {
                    var yamlDir = yaml.GetDirectory ();

                    cake.PackageComponent (yamlDir, new XamarinComponentSettings ());

                    cake.MoveFiles (yamlDir.FullPath.TrimEnd ('/') + "/*.xam", "./output/");
                }
            });

            if (!scriptHost.Tasks.Any (tsk => tsk.Name == Names.Component))
                scriptHost.Task (Names.Component).IsDependentOn (Names.Nuget).IsDependentOn (Names.ComponentBase);

            scriptHost.Task (Names.ExternalsBase).Does (() => {
                if (CakeSpec.GitRepoDependencies == null || !CakeSpec.GitRepoDependencies.Any ())
                    return;

                var gitPath = ResolveGitPath (cake);
                if (gitPath == null)
                    throw new System.IO.FileNotFoundException ("Could not locate git executable");
                
                foreach (var gitDep in CakeSpec.GitRepoDependencies) {
                    if (!cake.DirectoryExists (gitDep.Path))
                        cake.StartProcess (gitPath, "clone " + gitDep.Url + " " + cake.MakeAbsolute (gitDep.Path).FullPath);
                }
            });

            if (!scriptHost.Tasks.Any (tsk => tsk.Name == Names.Externals))
                scriptHost.Task (Names.Externals).IsDependentOn (Names.ExternalsBase);

            scriptHost.Task (Names.CleanBase).Does (() => {
                cake.CleanDirectories ("./**/bin");
                cake.CleanDirectories ("./**/obj");

                if (cake.DirectoryExists ("./output"))
                    cake.DeleteDirectory ("./output", true);

                if (cake.DirectoryExists ("./tosign"))
                    cake.DeleteDirectory ("./tosign", true);

                if (CakeSpec.GitRepoDependencies != null && CakeSpec.GitRepoDependencies.Any ()) {
                    foreach (var gitDep in CakeSpec.GitRepoDependencies) {
                        if (cake.DirectoryExists (gitDep.Path))
                            cake.DeleteDirectory (gitDep.Path, true);
                    }   
                }

                if (cake.DirectoryExists ("./tools"))
                    cake.DeleteDirectory ("./tools", true);
            });

            if (!scriptHost.Tasks.Any (tsk => tsk.Name == Names.Clean))
                scriptHost.Task (Names.Clean).IsDependentOn (Names.CleanBase);

            if (!scriptHost.Tasks.Any (tsk => tsk.Name == "Default"))
                scriptHost.Task ("Default").IsDependentOn (Names.Libraries);
        }
    }
}

