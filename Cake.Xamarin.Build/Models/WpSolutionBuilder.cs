using System;
using Cake.Core.IO;
using Cake.Core;
using Cake.Common.Diagnostics;
using Cake.Common;
using Cake.Common.IO;

namespace Cake.Xamarin.Build
{
    public class WpSolutionBuilder : DefaultSolutionBuilder
    {
        public WpSolutionBuilder (ICakeContext cakeContext) : base (cakeContext)
        {
            BuildsOn = BuildPlatforms.Windows;
            Platform = "";
        }

        public string WpPlatformTarget { get; set; }

        public override void RunBuild (FilePath solution)
        {
            if (!BuildsOnCurrentPlatform) {
                CakeContext.Information ("Solution is not configured to build on this platform: {0}", SolutionPath);
                return;
            }

            var buildTargets = "";
            if (Targets != null) {
                foreach (var t in Targets)
                    buildTargets += "/target:" + t + " ";
            }

            // We need to invoke MSBuild manually for now since Cake wants to set Platform=x86 if we use the x86 msbuild.exe version
            // and the amd64 msbuild.exe cannot be used to build windows phone projects
            // This should be fixable in cake 0.6.1
            var programFilesPath = CakeContext.Environment.GetSpecialPath(SpecialPath.ProgramFilesX86);
            var binPath = programFilesPath.Combine(string.Concat("MSBuild/", "14.0", "/Bin"));
            var msBuild = binPath.CombineWithFilePath("MSBuild.exe");

            if (!CakeContext.FileExists (msBuild)) {
                binPath = programFilesPath.Combine(string.Concat("MSBuild/", "12.0", "/Bin"));
                msBuild = binPath.CombineWithFilePath("MSBuild.exe");
            }

            CakeContext.StartProcess (msBuild, "/m /v:Normal /p:Configuration=Release " + buildTargets.Trim () + " \"" + CakeContext.MakeAbsolute (solution).ToString () + "\"");
        }
    }
}

