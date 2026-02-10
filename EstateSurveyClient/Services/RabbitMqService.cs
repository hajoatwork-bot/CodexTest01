using System.Text;
using EstateSurvey.Contracts;
using Google.Protobuf;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EstateSurveyClient.Services;

public sealed class RabbitMqService : IDisposable
{
    private const string ExchangeName = "survey.events";
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMqService(string hostName)
    {
        var factory = new ConnectionFactory { HostName = hostName };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true);
    }

    public void PublishTask(SurveyTask task, string routingKey)
    {
        var body = task.ToByteArray();
        _channel.BasicPublish(ExchangeName, routingKey, basicProperties: null, body);
    }

    public string StartListening(string queueName, string routingKey, Action<SurveyTask> onMessage)
    {
        _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queueName, ExchangeName, routingKey);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (_, args) =>
        {
            var task = SurveyTask.Parser.ParseFrom(args.Body.ToArray());
            onMessage(task);
            _channel.BasicAck(args.DeliveryTag, multiple: false);
        };

        return _channel.BasicConsume(queueName, autoAck: false, consumer);
    }

    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }
}
