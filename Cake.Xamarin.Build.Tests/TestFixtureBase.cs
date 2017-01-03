using Cake.Core;
using Cake.Core.IO;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cake.Xamarin.Build.Tests.Fakes
{
    [TestFixture]
    public abstract class TestFixtureBase
    {
        FakeCakeContext context;

        public ICakeContext Cake { get { return context.CakeContext; } }

        [SetUp]
        public void Setup()
        {
            context = new FakeCakeContext();

            var dp = new DirectoryPath("./testdata");
            var d = context.CakeContext.FileSystem.GetDirectory(dp);

            if (d.Exists)
                d.Delete(true);

            d.Create();
        }

        [TearDown]
        public void Teardown()
        {
            context.DumpLogs();
        }
    }
}
