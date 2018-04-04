using System;
using System.Linq;
using System.IO;
using System.Xml.Linq;
using Cake.Core.IO;
using Cake.Xamarin.Build;
using Xunit;

namespace Cake.Xamarin.Build.Tests.Fakes
{
    public class AndroidAarTests : TestFixtureBase
    {
        [Fact]
        public void FixAar_Test()
        {
            var src = new FilePath("../../TestData/test.aar");
            var aar = new FilePath("./tmp.aar");

            var srcFullPath = src.MakeAbsolute(Cake.Environment).FullPath;
            var aarFullPath = aar.MakeAbsolute(Cake.Environment).FullPath;

            if (File.Exists(aarFullPath))
                File.Delete(aarFullPath);
            File.Copy(srcFullPath, aarFullPath);

            Cake.FixAndroidAarFile(aarFullPath, "design");

            Assert.True(File.Exists(System.IO.Path.Combine(aar.GetDirectory().MakeAbsolute(Cake.Environment).FullPath, "design.proguard.txt")));

            using (var zipArchive = new System.IO.Compression.ZipArchive(File.OpenRead(aarFullPath), System.IO.Compression.ZipArchiveMode.Read))
            {
                var entryNames = zipArchive.Entries.Select(zae => zae.FullName).ToList();

                foreach (var entryName in entryNames)
                {
                    Assert.False(entryName.EndsWith("internal_impl.jar", StringComparison.OrdinalIgnoreCase));

                    var zipEntry = zipArchive.GetEntry(entryName);

                    if (entryName.EndsWith("AndroidManifest.xml", StringComparison.OrdinalIgnoreCase))
                    {
                        // android: namespace
                        XNamespace xns = "http://schemas.android.com/apk/res/android";

                        using (var xmlReader = System.Xml.XmlReader.Create(zipEntry.Open()))
                        {
                            var xdoc = XDocument.Load(xmlReader);

                            Assert.False(xdoc.Document.Descendants()
                                         .Any(elem => elem.Attribute(xns + "name")?.Value?.StartsWith(".", StringComparison.Ordinal) ?? false));
                        }
                    }
                }
            }
        }
    }
}

