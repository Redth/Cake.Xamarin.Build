using System;
using Cake.Xamarin.Build.Tests.Fakes;
using NUnit.Framework;

namespace Cake.Xamarin.Build.Tests
{
	public class VersionsTests : TestFixtureBase
	{
		[Test]
		public void DotNetCore_Versions()
		{
			var dotnet = Cake.GetDotNetCoreVersions();

			Assert.True(dotnet.RuntimeVersions.Count > 0);
			Assert.True(dotnet.SdkVersions.Count > 0);
		}
	}
}
