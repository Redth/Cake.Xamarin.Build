using Cake.Core.IO;

namespace Cake.Xamarin.Build
{
    public class Component
    {
        public Component ()
        {
            OutputDirectory = null;
            ManifestDirectory = "./component/";
            BuildsOn = BuildPlatforms.Windows | BuildPlatforms.Mac | BuildPlatforms.Linux;
        }

        public DirectoryPath ManifestDirectory { get; set; }

        public BuildPlatforms BuildsOn { get; set; }

        public DirectoryPath OutputDirectory { get; set; }
    }
}
