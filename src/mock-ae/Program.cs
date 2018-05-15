using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.IoC;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Tests.mock-ae")]

namespace Mattersight.mock.ba.ae
{
    public class Program
    {
        private static readonly ManualResetEvent ShutdownComplete = new ManualResetEvent(false);
        private static readonly CancellationTokenSource Ctx = new CancellationTokenSource();
        private static ILogger<Program> _logger;
        private static Task _workerTask;

        public static void Main()
        {
            AssemblyLoadContext.Default.Unloading += ShutdownHandler;
            var serviceProvider = new Services().BuildServiceProvider();
            _logger = serviceProvider.GetService<ILogger<Program>>();
            _workerTask = serviceProvider.GetService<Ae>().Start(Ctx.Token);
            ShutdownComplete.WaitOne();
        }

        /// <summary>
        /// This method is called via the assmebly unload event, which is triggerd when Docker shuts down a container
        /// </summary>
        private static void ShutdownHandler(AssemblyLoadContext context)
        {
            //https://stackoverflow.com/questions/40742192/how-to-do-gracefully-shutdown-on-dotnet-with-docker
            try
            {
                _logger.LogInformation("ShutdownHandler running.");
                Ctx.Cancel();
                _workerTask.Wait();
                NLog.LogManager.Shutdown();
            }
            catch { /* Ignore */ }

            ShutdownComplete.Set();
        }
    }
}
