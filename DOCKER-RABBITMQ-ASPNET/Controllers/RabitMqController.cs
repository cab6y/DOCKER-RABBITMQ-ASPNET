﻿using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System;
using RabbitMQ.Client;
using System.Text;

namespace DOCKER_RABBITMQ_ASPNET.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RabitMqController : ControllerBase
    {
        //TODO: senaryo oluşturmaya öşendim rastgele deneme diye bir class açtım
        public class deneme
        {
            public string degisken { get; set; }
        }
        [HttpPost]
        public void SendNameToQueue(string name)
        {
            deneme dene = new deneme();
            dene.degisken = name;
            var factory = new ConnectionFactory() { HostName = "localhost", UserName = "cihan", Password = "sivas1923*" };//Konfigurasyondan alınabilir            
            using (IConnection connection = factory.CreateConnection())
            using (IModel channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "NameQueue",
                    durable: false, //Data saklama yöntemi (memory-fiziksel)
                    exclusive: false, //Başka bağlantıların kuyruğa ulaşmasını istersek true kullanabiliriz.
                    autoDelete: false,
                    arguments: null);//Exchange parametre tanımları.          

                // Nesneyi JSON formatına dönüştür
                string json = System.Text.Json.JsonSerializer.Serialize(dene);

                // JSON stringini byte dizisine dönüştür
                var body = Encoding.UTF8.GetBytes(json);

                channel.BasicPublish(exchange: "",
                    routingKey: "NameQueue",
                    body: body);
            }
        }
        [HttpGet]
        public List<deneme> GetMessagesFromQueue()
        {
            var messages = new List<deneme>();
            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                UserName = "cihan",
                Password = "sivas1923*"
            };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            { 
                // Kuyruk adı tanımlanmalı
                channel.QueueDeclare(queue: "NameQueue",
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                // Mesajları kuyruğun sonuna kadar iterasyonla çekiyoruz
                while (true)
                {
                    // Kuyruktaki bir mesajı alın
                    var result = channel.BasicGet(queue: "NameQueue", autoAck: true);

                    if (result == null)
                    {
                        // Kuyruk boş, döngüyü bitir
                        break;
                    }

                    // Mesajın gövdesini UTF8'e çevir ve listeye ekle
                    var message = Encoding.UTF8.GetString(result.Body.ToArray());
                    deneme dene = System.Text.Json.JsonSerializer.Deserialize<deneme>(message);
                    messages.Add(dene);
                }
            }

            return messages;
        }

        [HttpDelete]
        public IActionResult DeleteMessageFromQueue(string queueName, int index)
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                UserName = "enes",
                Password = "enes123"
            };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                // Kuyruk tanımlama
                channel.QueueDeclare(queue: queueName,
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                var messages = new List<string>();

                // Mesajları sırayla al ve geçici bir listeye yükle
                while (true)
                {
                    var result = channel.BasicGet(queue: queueName, autoAck: false);
                    if (result == null)
                        break;

                    var message = Encoding.UTF8.GetString(result.Body.ToArray());
                    messages.Add(message);

                    // Mesajları kuyruğu boşaltmak için onayla (autoAck: false)
                    channel.BasicAck(deliveryTag: result.DeliveryTag, multiple: false);
                }

                // Silinecek mesajı kontrol et
                if (index < 0 || index >= messages.Count)
                {
                    return BadRequest($"Geçersiz indeks: {index}");
                }

                // Mesajı listeden kaldır
                messages.RemoveAt(index);

                // Kuyruğu temizledikten sonra kalan mesajları tekrar kuyruğa yaz
                foreach (var msg in messages)
                {
                    var body = Encoding.UTF8.GetBytes(msg);
                    channel.BasicPublish(exchange: "",
                        routingKey: queueName,
                        basicProperties: null,
                        body: body);
                }
            }

            return Ok($"Index {index} mesajı başarıyla silindi.");
        }


    }
}
