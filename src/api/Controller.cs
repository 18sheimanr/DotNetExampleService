using Confluent.Kafka;
using KafkaStarter.Shared.Models;
using KafkaStarter.Api.Models;
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
        public async Task<IActionResult> Post([FromBody] MessageRequestDto request)
        {
            if (string.IsNullOrEmpty(request.Content))
            {
                return BadRequest("Message content cannot be empty");
            }

            try
            {
                var message = new SimpleMessage(request.Content);
                
                var result = await _kafkaProducer.ProduceMessageAsync(message);

                var response = new MessageResponseDto
                {
                    MessageId = message.Id,
                    Status = "Produced",
                    Topic = result.Topic,
                    Partition = result.Partition.Value,
                    Offset = result.Offset.Value
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error producing message");
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        [HttpPost("batch")]
        public async Task<IActionResult> PostBatch([FromBody] BatchMessageRequestDto request)
        {
            if (request.Messages == null || !request.Messages.Any())
            {
                return BadRequest("Messages list cannot be empty");
            }

            if (request.Messages.Any(m => string.IsNullOrEmpty(m.Content)))
            {
                return BadRequest("Message content cannot be empty");
            }

            try
            {
                var messages = request.Messages.Select(dto => new SimpleMessage(dto.Content)).ToList();
                
                var results = await _kafkaProducer.ProduceBatchAsync(messages);
                
                var responseItems = messages.Select((message, index) => new MessageResponseDto
                {
                    MessageId = message.Id,
                    Status = "Produced",
                    Topic = results[index].Topic,
                    Partition = results[index].Partition.Value,
                    Offset = results[index].Offset.Value
                }).ToList();

                var response = new BatchMessageResponseDto
                {
                    Count = responseItems.Count,
                    Messages = responseItems
                };

                return Ok(response);
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