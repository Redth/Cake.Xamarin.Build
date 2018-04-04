using Cake.Core.IO;

namespace Cake.Xamarin.Build
{
    public class NuGetInfo
    {
        public NuGetInfo ()
        {
            OutputDirectory = null;
            Version = null;
            RequireLicenseAcceptance = false;
            BuildsOn = BuildPlatforms.Windows | BuildPlatforms.Mac | BuildPlatforms.Linux;
        }

        public FilePath NuSpec { get;set; }
        public DirectoryPath OutputDirectory { get; set; }
        public string Version { get;set; }
        public bool RequireLicenseAcceptance { get; set; }
        public BuildPlatforms BuildsOn { get; set; }
    }
}
