using Core;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Net.Http;

namespace Consumidor
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var factory = new ConnectionFactory() { HostName = "localhost", UserName = "guest", Password = "guest" };
                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();
                channel.QueueDeclare(queue: "fila",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += async (sender, eventArgs) =>
                {
                    var body = eventArgs.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var pedido = JsonSerializer.Deserialize<Pedido>(body);

                    //Enviar para banco de dados e gravar
                    await SendToBDByPostAsync(pedido);

                    Console.WriteLine(pedido?.ToString());
                };

                channel.BasicConsume(
                    queue: "fila",
                    autoAck: true,
                    consumer: consumer);
                await Task.Delay(2000, stoppingToken);
            }
        }

        public async Task SendToBDByPostAsync(Pedido pedido)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost:7195/api/Pedido");
            var content = new StringContent("{\r\n  " +
                "\"id\": " + pedido.Id.ToString() + ",\r\n  " +
                "\"usuario\": {\r\n    " +
                    "\"id\": 10,\r\n    " +
                    "\"nome\": \"string10\",\r\n    " +
                    "\"email\": \"string10\"\r\n  " +
                    "},\r\n  " +
                "\"dataCriacao\": \"2024-03-01T21:35:04.059Z" +
                "\"\r\n}", null, "application/json");
            request.Content = content;
            var response = await client.SendAsync(request);
            //response.EnsureSuccessStatusCode();
            Console.WriteLine(await response.Content.ReadAsStringAsync());
        }
    }
}
