using Cake.Core;
using Cake.Core.IO;
using Cake.Xamarin.Tests.Fakes;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cake.FileHelpers.Tests
{
    [TestFixture]
    public abstract class TestFixtureBase
    {
        public FakeSession Session { get; private set; }

        public ICakeContext Cake { get; private set; }

        [SetUp]
        public virtual void Setup()
        {
            Session = new FakeSession();
            Cake = Session.CakeContext;

            var dp = new DirectoryPath("./testdata");
            var d = Session.CakeContext.FileSystem.GetDirectory(dp);

            if (d.Exists)
                d.Delete(true);

            d.Create();
        }

        [TearDown]
        public virtual void Teardown()
        {
            Session.DumpLogs();
        }
    }
}
