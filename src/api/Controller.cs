using Confluent.Kafka;
using KafkaStarter.Shared.Models;
using KafkaStarter.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace KafkaStarter.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly ILogger<MessageController> _logger;
        private readonly IKafkaProducerService _kafkaProducer;

        public MessageController(ILogger<MessageController> logger, IKafkaProducerService kafkaProducer)
        {
            _logger = logger;
            _kafkaProducer = kafkaProducer;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SimpleMessage message)
        {
            if (string.IsNullOrEmpty(message.Content))
            {
                return BadRequest("Message content cannot be empty");
            }

            try
            {
                var result = await _kafkaProducer.ProduceMessageAsync(message);

                return Ok(new { 
                    MessageId = message.Id,
                    Status = "Produced",
                    Topic = result.Topic,
                    Partition = result.Partition.Value,
                    Offset = result.Offset.Value
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error producing message");
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        [HttpPost("batch")]
        public async Task<IActionResult> PostBatch([FromBody] List<SimpleMessage> messages)
        {
            if (messages == null || !messages.Any())
            {
                return BadRequest("Messages list cannot be empty");
            }

            if (messages.Any(m => string.IsNullOrEmpty(m.Content)))
            {
                return BadRequest("Message content cannot be empty");
            }

            try
            {
                var results = await _kafkaProducer.ProduceBatchAsync(messages);
                
                var responseItems = results.Select((result, index) => new {
                    MessageId = messages[index].Id,
                    Status = "Produced",
                    Topic = result.Topic,
                    Partition = result.Partition.Value,
                    Offset = result.Offset.Value
                }).ToList();

                return Ok(new { 
                    Count = responseItems.Count,
                    Messages = responseItems
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error producing batch messages");
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        [HttpGet("health")]
        public IActionResult Get()
        {
            return Ok(new { Status = "API is running" });
        }
    }
}