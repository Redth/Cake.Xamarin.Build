using System;
using Cake.Xamarin.Build.Tests.Fakes;
using Xunit;

namespace Cake.Xamarin.Build.Tests
{
    public class VersionsTests : TestFixtureBase
    {
        [Fact]
        public void DotNetCore_Versions()
        {
            var dotnet = Cake.GetDotNetCoreVersions();

            Assert.True(dotnet.RuntimeVersions.Count > 0);
            Assert.True(dotnet.SdkVersions.Count > 0);
        }
    }
}
