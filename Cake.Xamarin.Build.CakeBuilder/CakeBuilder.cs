using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using System.IO;

namespace Cake.Xamarin.Build
{
	[TestFixture]
	[Category("CakeBuilder")]
	public class CakeBuilder
	{
		internal const string EV_CAKE_EXE_PATH = "CAKE_EXE_PATH";
		internal const string EV_CAKE_BUILD_INFO_PATH = "CAKE_BUILD_INFO_PATH";

		static string cakeExePath;
		static string cakeBuildInfoPath;
		static string workingDir;

		public CakeBuilder ()
		{
			Console.WriteLine("CakeBuilder: CTOR");

			workingDir = Directory.GetCurrentDirectory();

			cakeExePath = Environment.GetEnvironmentVariable(EV_CAKE_EXE_PATH);
			if (string.IsNullOrEmpty(cakeExePath))
				cakeExePath = Path.Combine(workingDir, "tools", "Cake", "Cake.exe");

			cakeBuildInfoPath = Environment.GetEnvironmentVariable(EV_CAKE_BUILD_INFO_PATH);
			if (string.IsNullOrEmpty(cakeBuildInfoPath))
				cakeBuildInfoPath = Path.Combine(workingDir, "cakebuildinfo.txt");

			Console.WriteLine("CakeBuilder: Cake.exe Path: {0}", cakeExePath);
			Console.WriteLine("CakeBuilder: Build Info Path: {0}", cakeBuildInfoPath);
		}

		[Test, TestCaseSource("GetBuildInfo")]
		public void Build(string script, string target)
		{
			var fullScript = Path.Combine(workingDir, script);

			Console.WriteLine("CakeBuilder: Running Test: {0} {1}", fullScript, target);

			var p = System.Diagnostics.Process.Start(cakeExePath, $"\"{fullScript}\" --target={target}");

			p.WaitForExit();

			Assert.AreEqual(0, p.ExitCode);
		}

		public object[] GetBuildInfo ()
		{
			Console.WriteLine("CakeBuilder: GetBuildInfo -> {0}", cakeBuildInfoPath);
			var results = new List<object>();

			var lines = File.ReadAllLines(cakeBuildInfoPath);
			foreach (var line in lines) {
				var parts = line.Split(new[] { ':' }, 2);

				if (parts != null && parts.Length == 2) {
					var script = parts[0].Trim();
					var targets = parts[1].Split(';', ',');

					if (targets != null && targets.Any()) {
						foreach (var target in targets)
							results.Add(new object[] { script, target.Trim() });
					}
				}
			}

			Console.WriteLine("CakeBuilder: {0}", results);
			return results.ToArray();
		}
	}
}
