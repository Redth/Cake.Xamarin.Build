using System;
using Cake.Core.IO;
using Cake.Core;
using Cake.Common.Diagnostics;
using Cake.Common;
using Cake.Common.IO;
using Cake.Common.Tools.MSBuild;
using System.Linq;

namespace Cake.Xamarin.Build
{
    public class WpSolutionBuilder : DefaultSolutionBuilder
    {
        public WpSolutionBuilder () : base ()
        {
            BuildsOn = BuildPlatforms.Windows;
            Platform = "";
            MSBuildPlatform = Cake.Common.Tools.MSBuild.MSBuildPlatform.x86;
        }

        public string WpPlatformTarget { get; set; }

        public Cake.Common.Tools.MSBuild.MSBuildPlatform MSBuildPlatform { get; set; }

        public override void RunBuild (FilePath solution)
        {
            if (!BuildsOnCurrentPlatform) {
                CakeContext.Information ("Solution is not configured to build on this platform: {0}", SolutionPath);
                return;
            }

            CakeContext.MSBuild(solution, c => {

                c.Configuration = Configuration;
                c.MSBuildPlatform = MSBuildPlatform;

                if (!string.IsNullOrEmpty(Platform))

                    c.Properties["Platform"] = new[] { Platform };

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
    }
}

