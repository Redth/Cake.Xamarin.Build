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
        }

        public void Dispose()
        {
            context.DumpLogs();
        }
    }
}
