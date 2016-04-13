using Cake.Core.IO;

namespace Cake.Xamarin.Build
{
    public class NuGetInfo
    {
        public FilePath NuSpec { get;set; }
        public string Version { get;set; }
        public bool RequireLicenseAcceptance { get; set; }
    }
}
