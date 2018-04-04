using Cake.Core;
using Cake.Core.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Cake.Xamarin.Build.Tests.Fakes
{
    public abstract class TestFixtureBase : IDisposable
    {
        FakeCakeContext context;

        public ICakeContext Cake { get { return context.CakeContext; } }

        public TestFixtureBase()
        {
            context = new FakeCakeContext();

            var dp = new DirectoryPath("./testdata");
            var d = context.CakeContext.FileSystem.GetDirectory(dp);

            if (d.Exists)
                d.Delete(true);

            d.Create();
        }

        public void Dispose()
        {
            context.DumpLogs();
        }
    }
}
