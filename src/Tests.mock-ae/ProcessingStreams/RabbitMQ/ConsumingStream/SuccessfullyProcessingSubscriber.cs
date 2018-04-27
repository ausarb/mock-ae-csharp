using System;
using System.Collections.Generic;
using System.Threading;
using Mattersight.mock.ba.ae.ProcessingStreams.RabbitMQ;
using Mattersight.mock.ba.ae.Serialization;
using Moq;
using RabbitMQ.Client;
using Xbehave;

// ReSharper disable ImplicitlyCapturedClosure
namespace Mattersight.mock.ba.ae.Tests.ProcessingStreams.RabbitMQ.ConsumingStream
{
    public class SuccessfullyProcessingSubscriber
    {
        [Scenario]
        public void When_subscribers_event_proccesing_completes(ConsumingStream<object> sut, IBasicConsumer consumer, Mock<IModel> channel, ulong deliveryTag = 1234)
        {
            var ctx = new CancellationTokenSource();
            try
            {
                "Given a consuming stream".x(() =>
                {
                    channel = new Mock<IModel>();

                    // When channel.BasicConsume is called, the consumer is passed to it, so grab it and we can simulate receiving a message
                    channel
                        .Setup(x => x.BasicConsume(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<IBasicConsumer>()))
                        .Callback<string, bool, string, bool, bool, IDictionary<string, object>, IBasicConsumer>((foo1, foo2, foo3, foo4, foo5, foo6, theConsumer) => consumer = theConsumer)
                        .Returns("foo");

                    var connection = Mock.Of<IConnection>(x => x.CreateModel() == channel.Object);
                    var connectionFactory = Mock.Of<IConnectionFactory>(x => x.CreateConnection() == connection);

                    var deserializer = Mock.Of<IDeserializer<byte[], object>>(x => x.Deserialize(It.IsAny<byte[]>()) == new object());
                    sut = new ConsumingStream<object>(new QueueConfiguration(), connectionFactory, deserializer);
                    sut.Start(CancellationToken.None);
                });

                "with a successfully processing subscriber"
                    .x(() => sut.Subscribe(x => Console.WriteLine("noop")));

                "When a message is delivered"
                    .x(() => consumer.HandleBasicDeliver("foo", deliveryTag, false, "exchange", "routingKey", Mock.Of<IBasicProperties>(), new byte[100]));

                "It should ACK the message"
                    .x(() => channel.Verify(x => x.BasicAck(deliveryTag, It.IsAny<bool>()), Times.Once));

                "It should not NACK the message"
                    .x(() => channel.Verify(x => x.BasicNack(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never()));
            }
            finally
            {
                ctx.Cancel();
            }
        }
    }
}
