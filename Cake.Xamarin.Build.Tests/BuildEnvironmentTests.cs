using Xunit.Sdk;
using System;
using Cake.Core.IO;
using Cake.Xamarin.Build;
using Xunit;

namespace Cake.Xamarin.Build.Tests
{
    public class BuildEnvironmentTests : Cake.Xamarin.Build.Tests.Fakes.TestFixtureBase
    {
        [Fact]
        public void GetXamarinAndroidVersion()
        {
            var version = Cake.GetXamarinAndroidVersion();

            Assert.NotNull(version);
        }

		[Fact]
		public void GetXamariniOSVersion()
		{
			var version = Cake.GetXamariniOSVersion();

			Assert.NotNull(version);
		}

		[Fact]
		public void GetOS()
		{
			var version = Cake.GetOperatingSystem();

			Assert.NotNull(version);
		}

		[Fact]
		public void GetOSVersions()
		{
			var version = Cake.GetOperatingSystemVersion ();

			Assert.NotNull(version);
		}

		[Fact]
		public void GetCocoaPodsVersion()
		{
			var version = Cake.GetCocoaPodsVersion();

			Assert.NotNull(version);
		}

		[Fact]
		public void GetXCodeVersion()
		{
			var version = Cake.GetXCodeVersion();

			if (Cake.GetOperatingSystem () == Core.PlatformFamily.OSX)
				Assert.NotNull(version);
			else
				Assert.Null(version);
		}

		[Fact]
		public void GetBuildInfo()
		{
			var b = Cake.GetBuildEnvironmentInfo();

			Assert.NotNull(b.OperatingSystemName);
			Assert.NotNull(b.OperatingSystemVersion);
			Assert.NotNull(b.XamarinAndroidVersion);
			Assert.NotNull(b.XamariniOSVersion);

			if (b.OperatingSystem == Core.PlatformFamily.OSX)
			{
				Assert.NotNull(b.CocoaPodsVersion);
				Assert.NotNull(b.XCodeVersion);
			}
		}
    }
}

