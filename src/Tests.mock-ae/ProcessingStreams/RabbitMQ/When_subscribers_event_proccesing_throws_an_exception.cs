using System;
using System.Collections.Generic;
using System.Threading;
using Mattersight.mock.ba.ae.ProcessingStreams.RabbitMQ;
using Mattersight.mock.ba.ae.Serialization;
using Moq;
using RabbitMQ.Client;
using Xunit;

namespace Mattersight.mock.ba.ae.Tests.ProcessingStreams.RabbitMQ
{
    public class When_subscribers_event_proccesing_throws_an_exception
    {
        [Fact]
        public void It_should_NACK_the_queue()
        {
            var ctx = new CancellationTokenSource();
            try
            {
                IBasicConsumer consumer = null;
                var channel = new Mock<IModel>();

                // When channel.BasicConsume is called, the consumer is passed to it, so grab it and we can simulate receiving a message
                channel
                    .Setup(x => x.BasicConsume(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<IBasicConsumer>()))
                    .Callback<string, bool, string, bool, bool, IDictionary<string, object>, IBasicConsumer>((foo1, foo2, foo3, foo4, foo5, foo6, theConsumer) => consumer = theConsumer)
                    .Returns("foo");

                var connection = Mock.Of<IConnection>(x => x.CreateModel() == channel.Object);
                var connectionFactory = Mock.Of<IConnectionFactory>(x => x.CreateConnection() == connection);

                var deserializer = Mock.Of<IDeserializer<byte[], object>>(x => x.Deserialize(It.IsAny<byte[]>()) == new object());
                var sut = new ConsumingStream<object>(new QueueConfiguration(), connectionFactory, deserializer);

                sut.Start(CancellationToken.None);
                sut.Subscribe(x => throw new NotImplementedException());

                var deliveryTag = (ulong) 1234;
                consumer.HandleBasicDeliver("foo", deliveryTag, false, "exchange", "routingKey", Mock.Of<IBasicProperties>(), new byte[100]);
                channel.Verify(x => x.BasicNack(deliveryTag, false, true), Times.Once);
                channel.Verify(x => x.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>()), Times.Never());
            }
            finally
            {
                ctx.Cancel();
            }
        }
    }
}
