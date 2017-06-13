using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Cake.Core;
using Cake.Core.IO;
using Cake.Core.Tooling;

namespace Cake.Xamarin.Build
{
	[DataContract]
	public class VisualStudioInfo
	{
		[DataMember (Name="instanceId")]
		public string InstanceId { get; set; }
		[DataMember(Name = "installDate")]
		public string InstallDate { get; set; }
		[DataMember(Name = "installationName")]
		public string InstallationName { get; set; }
		[DataMember(Name = "installationPath")]
		public string InstallationPath { get; set; }
		[DataMember(Name = "installationVersion")]
		public string InstallationVersion { get; set; }
		[DataMember(Name = "displayName")]
		public string DisplayName { get; set; }
		[DataMember(Name = "description")]
		public string Description { get; set; }
		[DataMember(Name = "enginePath")]
		public string EnginePath { get; set; }
		[DataMember(Name = "clientId")]
		public string ChannelId { get; set; }
		[DataMember(Name = "channelUri")]
		public string ChannelUri { get; set; }
	}

	public class VSWhereToolSettings : ToolSettings
	{

	}

	public class VSWhereTool : Tool<VSWhereToolSettings>
	{
		public VSWhereTool(IFileSystem fileSystem, ICakeEnvironment environment, IProcessRunner processRunner, IToolLocator toolLocator)
			: base (fileSystem, environment, processRunner, toolLocator)
		{
		}

		protected override IEnumerable<string> GetToolExecutableNames()
		{
			return new[] { "vswhere.exe" };
		}

		protected override string GetToolName()
		{
			return "VSWhere";
		}

		public List<VisualStudioInfo> GetVisualStudioInstalls(VSWhereToolSettings settings = null)
		{
			var results = new List<VisualStudioInfo>();

			if (settings == null)
				settings = new VSWhereToolSettings();
			
			//adb devices -l
			var builder = new ProcessArgumentBuilder();

			builder.Append("-all");
			builder.Append("-format json");


			var p = RunProcess(settings, builder, new ProcessSettings
			{
				RedirectStandardOutput = true,
			});

			p.WaitForExit();

			var json = string.Join (Environment.NewLine, p.GetStandardOutput());

			using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(json)))
			{
				var jsonSerializer = new DataContractJsonSerializer(typeof(List<VisualStudioInfo>));

				results = (List<VisualStudioInfo>)jsonSerializer.ReadObject(ms);
			}

			if (results == null)
				results = new List<VisualStudioInfo>();

			return results;
		}
	}
}
