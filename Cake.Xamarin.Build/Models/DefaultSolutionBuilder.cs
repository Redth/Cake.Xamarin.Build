using System;
using System.Collections.Generic;
using System.Linq;
using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Tools;
using Cake.Common.Tools.NuGet;
using Cake.Common.Tools.NuGet.Restore;
using Cake.Core;
using Cake.Core.IO;

namespace Cake.Xamarin.Build
{
    public class DefaultSolutionBuilder : ISolutionBuilder
    {
        public DefaultSolutionBuilder (ICakeContext cakeContext)
        {
            CakeContext = cakeContext;
            OutputDirectory = "./output";
            RestoreComponents = false;
            BuildsOn = BuildPlatforms.Mac;
            Platform = "\"Any CPU\"";
            Configuration = "Release";
            Properties = new Dictionary<string, List<string>> ();
        }

        public ICakeContext CakeContext { get; private set; }

        public string SolutionPath { get; set; }

        public BuildPlatforms BuildsOn { get; set; }

        public Dictionary<string, List<string>> Properties { get; set; }
        public IEnumerable<string> Targets { get; set; }
        public virtual string Configuration { get; set; }
        public virtual string Platform { get; set; }

        public OutputFileCopy [] OutputFiles { get; set; }
        public virtual string OutputDirectory { get; set; }
        public virtual bool RestoreComponents { get; set; }

        public Action PreBuildAction { get;set; }
        public Action PostBuildAction { get;set; }

        protected virtual bool BuildsOnCurrentPlatform
        {
            get {
                if (CakeContext.IsRunningOnUnix () &&
                ((BuildsOn & BuildPlatforms.Linux) != 0 || (BuildsOn & BuildPlatforms.Mac) != 0))
                    return true;

                if (CakeContext.IsRunningOnWindows () &&
                (BuildsOn & BuildPlatforms.Windows) != 0)
                    return true;
            
                return false;
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
                Source = CakeSpec.NuGetSources.Select (s => s.Url).ToList ()
            });

            RunBuild (SolutionPath);

            if (PostBuildAction != null)
                PostBuildAction ();
        }

        public virtual void RunBuild (FilePath solution)
        {
            CakeContext.DotNetBuild (solution, c => { 
                c.Configuration = Configuration; 
                if (!string.IsNullOrEmpty (Platform))
                    c.Properties ["Platform"] = new [] { Platform }; 
                if (Targets != null && Targets.Any ()) {
                    foreach (var t in Targets)
                        c.Targets.Add (t);
                }           
                if (Properties != null && Properties.Any ()) {
                    foreach (var kvp in Properties)
                        c.Properties.Add (kvp.Key, kvp.Value);
                }
            }); 
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
