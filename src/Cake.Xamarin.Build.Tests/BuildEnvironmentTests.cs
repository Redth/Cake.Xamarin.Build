using System;
using Cake.Core.IO;
using Cake.Xamarin.Build;
using Xunit;

namespace Cake.Xamarin.Build.Tests
{
    public class BuildEnvironmentTests : Cake.Xamarin.Build.Tests.Fakes.TestFixtureBase
    {
        [Fact]
        public void GetBuildInfo()
        {
            var b = Cake.GetSystemInfo();

            Assert.NotNull(b.OperatingSystemName);
            Assert.NotNull(b.OperatingSystemVersion);

            if (b.OperatingSystem == Core.PlatformFamily.OSX)
            {
                Assert.NotNull(b.XamarinAndroidVersion);
                Assert.NotNull(b.XamariniOSVersion);

                Assert.NotNull(b.CocoaPodsVersion);
                Assert.NotNull(b.XCodeVersion);
            }
        }
    }
}

