using System;
namespace Cake.Xamarin.Build
{
    public class SystemInfo
    {
        public string XCodeVersion { get; set; }
        public string CocoaPodsVersion { get; set; }
        //public string VisualStudioVersion { get; set; }
        public string XamarinAndroidVersion { get; set; }
        public string XamariniOSVersion { get; set; }
        public Cake.Core.PlatformFamily OperatingSystem { get; set; }
		public string OperatingSystemName { get; set; }
        public string OperatingSystemVersion { get; set; }
    }
}
