using System;
using Newtonsoft.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Sublime.RabbitMQ
{
    public class Client : IDisposable
    {
        #region Members

        ClientConfig config;
        IConnectionFactory factory;
        IConnection connection;
        IModel channel;
        QueueDeclareOk queue;
        QueueingBasicConsumer consumer;
        CancellationTokenSource source;
        CancellationToken token;
        Task dequeue;

        #endregion

        #region Events

        public delegate void MessageHandler(object sender, MessageEventArgs args);
        public event MessageHandler OnMessage;

        #endregion

        #region Constructors

        public Client(ClientConfig config)
        {
            this.config = config;
            this.factory = new ConnectionFactory()
            {
                HostName = this.config.HostName
            };

            this.connection = this.factory.CreateConnection();
            this.channel = this.connection.CreateModel();

            this.queue = this.channel.QueueDeclare(this.config.QueueName, true, false, false, null);

            if (this.config.DeclareExchange)
                this.channel.ExchangeDeclare(this.config.Exchange, "topic", true, false, null);

            this.channel.QueueBind(this.config.QueueName, this.config.Exchange, this.config.RoutingKey ?? string.Empty);

            this.source = new CancellationTokenSource();
            this.token = this.source.Token;
        }

        #endregion

        #region Methods

        public void Publish(string route, object message)
        {
            var buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
            channel.BasicPublish(this.config.Exchange, route, null, buffer);
        }

        public void Subscribe(params string[] routes)
        {
            if (this.config.BindQueues)
                foreach (var route in routes)
                    this.channel.QueueBind(this.queue, this.config.Exchange, route);

            this.consumer = new QueueingBasicConsumer(this.channel);
            this.channel.BasicConsume(this.queue, true, this.consumer);

            this.dequeue = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (this.token.IsCancellationRequested)
                        this.token.ThrowIfCancellationRequested();

                    var args = (BasicDeliverEventArgs)this.consumer.Queue.Dequeue();
                    var message = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(args.Body));

                    if (null != this.OnMessage)
                        this.OnMessage(this, new MessageEventArgs(args.RoutingKey, message));
                }
            }, this.source.Token);
        }

        public void Dispose()
        {
            if (null != this.dequeue)
            {
                this.source.Cancel();

                try
                {
                    this.dequeue.Wait();
                }
                catch (AggregateException)
                {
                }
                finally
                {
                    this.source.Dispose();
                    this.dequeue.Dispose();

                    this.dequeue = null;
                    this.source = null;
                }
            }

            this.channel.Dispose();
            this.connection.Dispose();

            this.channel = null;
            this.config = null;
            this.factory = null;
            this.config = null;
            this.queue = null;
            this.consumer = null;
        }

        #endregion
    }
}

