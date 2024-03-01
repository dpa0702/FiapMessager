using Core;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Produtor.Controllers
{
    [ApiController]
    [Route("/Pedido")]
    public class PedidoController : ControllerBase
    {
        [HttpPost]
        public IActionResult Post(Pedido pedido)
        {
            var factory = new ConnectionFactory() { HostName = "localhost", UserName = "guest", Password = "guest" };
            using var connection = factory.CreateConnection();
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(
                    queue: "fila",
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                string message = JsonSerializer
                    .Serialize(pedido);
                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(
                    exchange: "",
                    routingKey: "fila",
                    basicProperties: null,
                    body: body);
            }

            return Ok();
        }
    }
}
