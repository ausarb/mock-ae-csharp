using System;
using System.Text;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Serialization;
using Mattersight.mock.ba.ae.StreamProcessing.RabbitMQ;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using Xbehave;

namespace Mattersight.mock.ba.ae.Tests.ProcessingStreams.RabbitMQ.QueueProducer
{
    public class Producing
    {
        [Scenario]
        public void When_OnNext_method_called(QueueProducer<object> sut, QueueConfiguration queueConfiguration, byte[] serializedMessage, object message)
        {
            message = new object();
            serializedMessage = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString());
            var serializer = Mock.Of<ISerializer<object, byte[]>>(x => x.Serialize(message) == Task.FromResult(serializedMessage));

            var channelProperties = Mock.Of<IBasicProperties>();
            var channel = Mock.Of<IModel>(x => x.CreateBasicProperties() == channelProperties);

            "Given a producing stream".x(() =>
            {
                queueConfiguration = new QueueConfiguration { Name = "The name of the queue goes here" };

                var connection = Mock.Of<IConnection>(x => x.CreateModel() == channel);
                sut = new QueueProducer<object>(Mock.Of<ILogger<QueueProducer<object>>>(), connection, queueConfiguration, serializer);
            });

            "When give a message to the OnNext method".x(async () =>
            {
                await sut.OnNext(message);
            });

            "It should use the serializer to serialize the message".x(() =>
            {
                Mock.Get(serializer).Verify(x => x.Serialize(message), Times.Once);
            });

            "It should perform a BasicPublish with the seriazliedMessage".x(() =>
            {
                Mock.Get(channel).Verify(x => x.BasicPublish("", queueConfiguration.Name, false, channelProperties, serializedMessage), Times.Once);
            });
        }
    }
}
