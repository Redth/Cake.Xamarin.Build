#tool nuget:?package=NUnit.ConsoleRunner
#tool "nuget:?package=ILRepack"

var sln = "./Cake.Xamarin.Build.sln";
var nuspec = "./Cake.Xamarin.Build.nuspec";
var nugetVersion = Argument ("nuget_version", EnvironmentVariable ("NUGET_VERSION") ?? "0.0.0.0");
var target = Argument ("target", "build");
var configuration = Argument("configuration", EnvironmentVariable ("CONFIGURATION") ?? "Release");

Task ("build").Does (() =>
{
	NuGetRestore (sln);
	DotNetBuild (sln, c => c.Configuration = configuration);
});

Task ("package").IsDependentOn("build").Does (() =>
{
	EnsureDirectoryExists ("./output/");

	ILRepack ("./output/Cake.Xamarin.Build.CakeBuilder.dll", 
		"./Cake.Xamarin.Build.CakeBuilder/bin/" + configuration + "/Cake.Xamarin.Build.CakeBuilder.dll",
		new FilePath[] { "./Cake.Xamarin.Build.CakeBuilder/bin/" + configuration + "/nunit.framework.dll" },
		new ILRepackSettings {
			Libs = new List<FilePath> {
				"./Cake.Xamarin.Build.CakeBuilder/bin/" + configuration,
			}
		});

	CopyFile ("./Cake.Xamarin.Build/bin/" + configuration + "/Cake.Xamarin.Build.dll", "./output/Cake.Xamarin.Build.dll");
	CopyFile ("./Cake.Xamarin.Build/bin/" + configuration + "/Cake.Xamarin.Build.xml", "./output/Cake.Xamarin.Build.xml");

	CopyFile ("./Cake.Xamarin.Build/bin/" + configuration + "/ICSharpCode.SharpZipLib.dll", "./output/ICSharpCode.SharpZipLib.dll");

	NuGetPack (nuspec, new NuGetPackSettings {
		OutputDirectory = "./output/",
		Version = nugetVersion,
	});
});

Task ("clean").Does (() =>
{
	CleanDirectories ("./**/bin");
	CleanDirectories ("./**/obj");
});

Task("test").IsDependentOn("package").Does(() =>
{
	NUnit3("./**/bin/"+ configuration + "/*.Tests.dll");
});

Task ("Default")
	.IsDependentOn ("test");

RunTarget (target);
