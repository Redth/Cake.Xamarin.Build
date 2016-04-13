using NUnit.Framework;
using System;
using Cake.Xamarin.Tests.Fakes;
using Cake.Core.IO;

namespace Cake.FileHelpers.Tests
{
    [TestFixture]
    public class FileHelperTests
    {
        FakeCakeContext context;

        [SetUp]
        public void Setup ()
        {
            context = new FakeCakeContext ();

            var dp = new DirectoryPath ("./testdata");
            var d = context.CakeContext.FileSystem.GetDirectory (dp);

            if (d.Exists)
                d.Delete (true);

            d.Create ();
        }

        [TearDown]
        public void Teardown ()
        {
            context.DumpLogs ();
        }

        // TODO: Test?
    }
}

