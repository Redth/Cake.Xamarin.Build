using Cake.Core.IO;
using Cake.Core;
using Cake.Common.Diagnostics;
using Cake.Common;

namespace Cake.Xamarin.Build
{
    public class IOSSolutionBuilder : DefaultSolutionBuilder
    {
        public IOSSolutionBuilder () : base ()
        {
            Platform = "iPhone";
        }

        public override void RunBuild (FilePath solution)
        { 
            if (!BuildsOnCurrentPlatform) {
                CakeContext.Information ("Solution is not configured to build on this platform: {0}", SolutionPath);
                return;
            }

            //if (CakeContext.IsRunningOnUnix ()) { 
            //    CakeContext.MDToolBuild (solution, c => {
            //        c.Configuration = Configuration;
            //    }); 
            //} else { 
            base.RunBuild (solution); 
            //} 
        } 
    }
}
