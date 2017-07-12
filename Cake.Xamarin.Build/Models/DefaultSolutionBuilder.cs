using System;
using System.Collections.Generic;
using System.Linq;
using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Tools;
using Cake.Common.Tools.MSBuild;
using Cake.Common.Tools.NuGet;
using Cake.Common.Tools.NuGet.Restore;
using Cake.Core;
using Cake.Core.IO;

namespace Cake.Xamarin.Build
{
    public class DefaultSolutionBuilder : ISolutionBuilder
    {
        public DefaultSolutionBuilder ()
        {
            OutputDirectory = "./output";
            RestoreComponents = false;
            BuildsOn = BuildPlatforms.Mac;
            Platform = "\"Any CPU\"";
            Configuration = "Release";
            Properties = new Dictionary<string, List<string>> ();
			AlwaysUseMSBuild = true;
        }

        public int? MaxCpuCount { get; set; }

        public BuildSpec BuildSpec { get; set; }

        public ICakeContext CakeContext { get; set; }

        public string SolutionPath { get; set; }

        public BuildPlatforms BuildsOn { get; set; }

        public Dictionary<string, List<string>> Properties { get; set; }
        public IEnumerable<string> Targets { get; set; }
        public virtual string Configuration { get; set; }
        public virtual string Platform { get; set; }

        public OutputFileCopy [] OutputFiles { get; set; }
        public virtual string OutputDirectory { get; set; }
        public virtual bool RestoreComponents { get; set; }
		public virtual bool AlwaysUseMSBuild { get; set; }
		public virtual Core.Diagnostics.Verbosity? Verbosity { get; set; }

        public Action PreBuildAction { get;set; }
        public Action PostBuildAction { get;set; }

        public void Init (ICakeContext cakeContext, BuildSpec buildSpec)
        {
            CakeContext = cakeContext;
            BuildSpec = buildSpec;
        }

        protected virtual bool BuildsOnCurrentPlatform
        {
            get {
                return BuildPlatformUtil.BuildsOnCurrentPlatform(CakeContext, BuildsOn);       
            }
        }

        public virtual void BuildSolution ()
        {       
            if (!BuildsOnCurrentPlatform) {
                CakeContext.Information ("Solution is not configured to build on this platform: {0}", SolutionPath);
                return;
            }

            if (PreBuildAction != null)
                PreBuildAction ();

            if (RestoreComponents)
                CakeContext.RestoreComponents (SolutionPath, new XamarinComponentRestoreSettings ());

            CakeContext.NuGetRestore (SolutionPath, new NuGetRestoreSettings { 
                Source = BuildSpec.NuGetSources.Select (s => s.Url).ToList ()
            });

            RunBuild (SolutionPath);

            if (PostBuildAction != null)
                PostBuildAction ();
        }

        public virtual void RunBuild (FilePath solution)
        {
            if (CakeContext.IsRunningOnWindows() || AlwaysUseMSBuild)
            {
                CakeContext.MSBuild(solution, c =>
                {
                    if (CakeContext.GetOperatingSystem() == PlatformFamily.OSX)
                        c.ToolPath = "/Library/Frameworks/Mono.framework/Versions/Current/Commands/msbuild";

                    if (Verbosity.HasValue)
                        c.Verbosity = Verbosity.Value;

                    if (MaxCpuCount.HasValue)
                        c.MaxCpuCount = MaxCpuCount.Value;
                    c.Configuration = Configuration;
                    if (!string.IsNullOrEmpty(Platform))
                       c.Properties["Platform"] = new[] { Platform };
                    if (Targets != null && Targets.Any())
                    {
                       foreach (var t in Targets)
                           c.Targets.Add(t);
                    }
                    if (Properties != null && Properties.Any())
                    {
                       foreach (var kvp in Properties)
                           c.Properties.Add(kvp.Key, kvp.Value);
                    }
                });
            }
            else
            {
                CakeContext.DotNetBuild(solution, c =>
                {
                    if (Verbosity.HasValue)
                        c.Verbosity = Verbosity.Value;

                    c.Configuration = Configuration;
                    if (!string.IsNullOrEmpty(Platform))
                        c.Properties["Platform"] = new[] { Platform };
                    if (Targets != null && Targets.Any())
                    {
                        foreach (var t in Targets)
                            c.Targets.Add(t);
                    }
                    if (Properties != null && Properties.Any())
                    {
                        foreach (var kvp in Properties)
                            c.Properties.Add(kvp.Key, kvp.Value);
                    }
                });
            }
        }

        public virtual void CopyOutput ()
        {
            if (OutputFiles == null)
                return;

            if (!BuildsOnCurrentPlatform)
                return;

            foreach (var fileCopy in OutputFiles) {
                FilePath targetFileName;

                var targetDir = fileCopy.ToDirectory ?? OutputDirectory;
                CakeContext.CreateDirectory (targetDir);

                if (fileCopy.NewFileName != null)
                    targetFileName = targetDir.CombineWithFilePath (fileCopy.NewFileName);
                else
                    targetFileName = targetDir.CombineWithFilePath (fileCopy.FromFile.GetFilename ());  
                
                var srcAbs = CakeContext.MakeAbsolute (fileCopy.FromFile);
                var destAbs = CakeContext.MakeAbsolute (targetFileName);

                var sourceTime = System.IO.File.GetLastAccessTime (srcAbs.ToString ());
                var destTime = System.IO.File.GetLastAccessTime (destAbs.ToString ());

                CakeContext.Information ("Target Dir: Exists? {0}, {1}", CakeContext.DirectoryExists (targetDir), targetDir);

                CakeContext.Information ("Copy From: Exists? {0}, Dir Exists? {1}, Modified: {2}, {3}", 
                    CakeContext.FileExists (srcAbs), CakeContext.DirectoryExists (srcAbs.GetDirectory ()), sourceTime, srcAbs);
                CakeContext.Information ("Copy To:   Exists? {0}, Dir Exists? {1}, Modified: {2}, {3}", 
                    CakeContext.FileExists (destAbs), CakeContext.DirectoryExists (destAbs.GetDirectory ()), destTime, destAbs);

                if (sourceTime > destTime || !CakeContext.FileExists (destAbs)) {
                    CakeContext.Information ("Copying File: {0} to {1}", srcAbs, targetDir);
                    CakeContext.CopyFileToDirectory (srcAbs, targetDir);
                }
            }
        }
    }

}
