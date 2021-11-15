using Cake.Core;
using Cake.Core.IO;
using Cake.Core.Tooling;
using Cake.Testing;
using System;

namespace Cake.Xamarin.Build.Tests.Fakes
{
    public class FakeCakeContext
    {
        readonly ICakeContext context;
        readonly FakeCakeLog log;
        readonly DirectoryPath testsDir;

        public FakeCakeContext()
        {
            testsDir = new DirectoryPath(
                System.IO.Path.GetFullPath(AppContext.BaseDirectory));

            var fileSystem = new FileSystem();
            log = new FakeCakeLog();
            var environment = new CakeEnvironment(new CakePlatform(), new CakeRuntime());
            var globber = new Globber(fileSystem, environment);

            var args = new FakeCakeArguments();
            var registry = new WindowsRegistry();

            var toolRepository = new ToolRepository(environment);
            var config = new FakeConfiguration();
            var toolResolutionStrategy = new ToolResolutionStrategy(fileSystem, environment, globber, config, log);
            IToolLocator tools = new ToolLocator(environment, toolRepository, toolResolutionStrategy);
            var processRunner = new ProcessRunner(fileSystem, environment, log, tools, config);

            var dataService = new FakeCakeDataService();
            context = new CakeContext(fileSystem, environment, globber, log, args, processRunner, registry, tools, dataService, config);
            context.Environment.WorkingDirectory = testsDir;
        }

        public DirectoryPath WorkingDirectory
        {
            get { return testsDir; }
        }

        public ICakeContext CakeContext
        {
            get { return context; }
        }

        public string GetLogs()
        {
            return string.Join(Environment.NewLine, log.Messages);
        }

        public void DumpLogs()
        {
            foreach (var m in log.Messages)
                Console.WriteLine(m);
        }
    }
}
