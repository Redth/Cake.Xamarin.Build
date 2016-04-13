
namespace Cake.Xamarin.Build
{
    public static class CakeSpec
    {
        static CakeSpec ()
        {
            Libs = new ISolutionBuilder [] {};
            Samples = new ISolutionBuilder [] {};
            NuGets = new NuGetInfo [] {};
            NuGetSources = new NuGetSource[] {};
            GitRepoDependencies = new GitRepository[] {};
        }

        public static ISolutionBuilder[] Libs { get; set; }
        public static ISolutionBuilder[] Samples { get; set; }
        public static NuGetInfo[] NuGets { get; set; }
        public static NuGetSource[] NuGetSources { get; set; }
        public static GitRepository[] GitRepoDependencies { get;set; }
    }
}
