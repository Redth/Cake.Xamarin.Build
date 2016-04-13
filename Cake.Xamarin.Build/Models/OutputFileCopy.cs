using Cake.Core.IO;

namespace Cake.Xamarin.Build
{
    public class OutputFileCopy
    {
        public FilePath FromFile { get; set; }
        public DirectoryPath ToDirectory { get; set; }
        public FilePath NewFileName { get; set; }
    }
}
