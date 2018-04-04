using System;
using System.Collections.Generic;
using Cake.Core;
using Cake.Core.IO;
using Cake.Core.Tooling;

namespace Cake.Xamarin.Build
{
	public class DotNetCoreVersionInfo
	{
		public List<string> RuntimeVersions { get;set; } = new List<string>();

		public List<string> SdkVersions { get; set; } = new List<string>();
	}

	public class DotNetCoreVersionToolSettings : ToolSettings
	{
	}

	public class DotNetCoreVersionTool : Tool<DotNetCoreVersionToolSettings>
	{
		public DotNetCoreVersionTool(IFileSystem fileSystem, ICakeEnvironment environment, IProcessRunner processRunner, IToolLocator toolLocator)
			: base(fileSystem, environment, processRunner, toolLocator)
		{
			Environment = environment;
			FileSystem = fileSystem;
		}

		public ICakeEnvironment Environment { get; private set; }
		public IFileSystem FileSystem { get; private set; }

		protected override IEnumerable<string> GetToolExecutableNames()
		{
			return new[] { "dotnet.exe", "dotnet" };
		}

		protected override string GetToolName()
		{
			return "dotnet";
		}

		public DotNetCoreVersionInfo GetDotNetCoreVersions(DotNetCoreVersionToolSettings settings = null)
		{
			const string DOTNET_PATH_WINDOWS = "C:\\Program Files\\dotnet";
			const string DOTNET_PATH_UNIX = "/usr/local/share/dotnet";

			var results = new DotNetCoreVersionInfo();

			if (settings == null)
				settings = new DotNetCoreVersionToolSettings();

			DirectoryPath dotnetPath;

			if (settings.ToolPath != null && FileSystem.Exist(settings.ToolPath)) {
				dotnetPath = settings.ToolPath.GetDirectory();
			} else {
				dotnetPath = new DirectoryPath(Environment.Platform.Family == PlatformFamily.Windows ? DOTNET_PATH_WINDOWS : DOTNET_PATH_UNIX);
			}

			var sdkDirs = FileSystem.GetDirectory(dotnetPath.Combine("sdk")).GetDirectories("*", SearchScope.Current);

			foreach (var d in sdkDirs)
				results.SdkVersions.Add(d.Path.GetDirectoryName());

			var runtimeDirs = FileSystem.GetDirectory(dotnetPath.Combine("shared").Combine("Microsoft.NETCore.App")).GetDirectories("*", SearchScope.Current);
			foreach (var d in runtimeDirs)
				results.RuntimeVersions.Add(d.Path.GetDirectoryName());

			return results;
		}
	}
}
