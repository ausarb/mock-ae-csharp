using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Mattersight.mock.ba.ae.ProcessingStreams.RabbitMQ
{
    public abstract class ProcessingStream : IProcessingStream
    {
        protected IModel Channel;
        protected readonly ILogger<ProcessingStream> Logger;
        protected readonly QueueConfiguration QueueConfiguration;
        private readonly IConnectionFactory _connectionFactory;

        protected ProcessingStream(ILogger<ProcessingStream> logger, QueueConfiguration queueConfiguration, IConnectionFactory connectionFactory)
        {
            Logger = logger;
            QueueConfiguration = queueConfiguration;
            _connectionFactory = connectionFactory;
        }

        public Task Start(CancellationToken token)
        {
            string friendlyUri;
            if (_connectionFactory is ConnectionFactory uriInfo)
            {
                friendlyUri = uriInfo.Endpoint.ToString();
            }
            else
            {
                friendlyUri = _connectionFactory.GetType().ToString();
            }
            friendlyUri += " - QueueName=" + QueueConfiguration.Name;
            Logger.LogInformation($"Connecting to: {friendlyUri}.");

            var initializationComplete = new ManualResetEventSlim();
            var exceptionHappened = false;
            var task = Task.Run(() =>
            {
                try
                {
                    var factory = _connectionFactory;
                    using (var connection = factory.CreateConnection())
                    using (Channel = connection.CreateModel())
                    {
                        Channel.QueueDeclare(
                            queue: QueueConfiguration.Name,
                            durable: true,
                            exclusive: false,
                            autoDelete: QueueConfiguration.AutoDelete);

                        Channel.BasicQos(prefetchSize: 0, prefetchCount: 300, global: false);
                        initializationComplete.Set();

                        token.WaitHandle.WaitOne();
                    }
                }
                //Intended to catch an exception thrown during the connection attempt.
                //Use a finally instead of catch/rethrow because it better preserves our stack trace.
                finally
                {
                    exceptionHappened = true;
                    initializationComplete.Set();
                }
            }, token);

            if (!initializationComplete.Wait(TimeSpan.FromMinutes(1)))
            {
                throw new Exception("Could not connect to RabbitMQ within 60 seconds.");
            }

            //We must wait for the task to complete or else its exception will not be visible.
            if (exceptionHappened)
            {
                var completed = task.Wait(TimeSpan.FromMinutes(1));
                throw new Exception($"Exception trying to connectto RabbitMQ.  Task.Wait={completed}.", task.Exception);
            }

            Logger.LogInformation($"Successfully connected to: {friendlyUri}.");

            return task;
        }
    }
}
