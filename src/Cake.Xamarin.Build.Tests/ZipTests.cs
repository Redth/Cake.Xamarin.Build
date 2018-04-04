//using System;
//using System.Linq;
//using Cake.Core.IO;
//using Cake.Xamarin.Build;
//using Xunit;

//namespace Cake.Xamarin.Build.Tests.Fakes
//{
//    public class ZipTests : TestFixtureBase
//    {
//        [Fact]
//        public void FindZipEntries()
//        {
//            var url = "https://github.com/Redth/Cake.Xamarin.Build/archive/master.zip";

//            var destFile = new FilePath("./repo.zip");

//            Cake.DownloadFile(url, destFile, new DownloadFileSettings
//            {
//                UserAgent = "curl/7.43.0"
//            });

//            var fileInfo = new System.IO.FileInfo(destFile.MakeAbsolute(Cake.Environment).FullPath);

//            Assert.True(fileInfo.Exists);
//            Assert.True(fileInfo.Length > 1024);

//            var entries = Cake.FindZipEntries(destFile);

//            var readmeEntry = entries.FirstOrDefault(e => e.EntryName.Contains("README.md"));

//            Assert.IsNotNull(readmeEntry);

//            var text = Cake.ReadZipEntryText(destFile, readmeEntry.EntryName, false);

//            Assert.IsNotEmpty(text);
//        }
//    }
//}

