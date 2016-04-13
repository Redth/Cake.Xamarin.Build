
using Cake.Core;

namespace Cake.Xamarin.Build
{
    public class BuildSpec
    {
        public BuildSpec ()
        {
            Libs = new ISolutionBuilder [] {};
            Samples = new ISolutionBuilder [] {};
            NuGets = new NuGetInfo [] {};
            NuGetSources = new NuGetSource[] {};
            GitRepoDependencies = new GitRepository[] {};
        }

        public void Init (ICakeContext cakeContext)
        {          
            foreach (var l in Libs)
                l.Init(cakeContext, this);

            foreach (var s in Samples)
                s.Init(cakeContext, this);
        }

        public ISolutionBuilder[] Libs { get; set; }
        public ISolutionBuilder[] Samples { get; set; }
        public NuGetInfo[] NuGets { get; set; }
        public NuGetSource[] NuGetSources { get; set; }
        public GitRepository[] GitRepoDependencies { get;set; }
    }
}
