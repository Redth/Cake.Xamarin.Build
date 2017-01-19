using Cake.Core;
using Cake.Core.IO;
using Cake.Xamarin.Tests.Fakes;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cake.Xamarin.Build.Tests.Fakes
{
    public abstract class TestFixtureBase : IDisposable
    {
        FakeCakeContext context;

        public ICakeContext Cake { get { return context.CakeContext; } }

		public TestFixtureBase()
		{
			Setup();
		}

        public void Setup()
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
