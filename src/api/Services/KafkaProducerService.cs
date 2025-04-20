using Confluent.Kafka;
using KafkaStarter.Shared.Models;
using KafkaStarter.Api.Models;
using System.Text.Json;

namespace KafkaStarter.Api.Services
{
    public interface IKafkaProducerService
    {
        Task<DeliveryResult<string, string>> ProduceMessageAsync(SimpleMessage message, string topic = "messages");
        Task<List<DeliveryResult<string, string>>> ProduceBatchAsync(List<SimpleMessage> messages, string topic = "messages");
    }

    public class KafkaProducerService : IKafkaProducerService, IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<KafkaProducerService> _logger;
        private bool _disposed = false;

        public KafkaProducerService(IConfiguration configuration, ILogger<KafkaProducerService> logger)
        {
            _logger = logger;
            
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"]
            };
            
            _producer = new ProducerBuilder<string, string>(producerConfig).Build();
            
            _logger.LogInformation("Kafka producer service initialized with bootstrap servers: {Servers}", 
                configuration["Kafka:BootstrapServers"]);
        }

        public async Task<DeliveryResult<string, string>> ProduceMessageAsync(SimpleMessage message, string topic = "messages")
        {
            // Ensure message has ID and timestamp
            if (message.Id == Guid.Empty)
            {
                message.Id = Guid.NewGuid();
            }
            
            message.Timestamp = DateTime.UtcNow;
            
            var result = await _producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = message.Id.ToString(),
                Value = JsonSerializer.Serialize(message)
            });
            
            _logger.LogInformation(
                "Message {MessageId} delivered to topic {Topic} [Partition: {Partition}, Offset: {Offset}]",
                message.Id, result.Topic, result.Partition, result.Offset);
                
            return result;
        }

        public async Task<List<DeliveryResult<string, string>>> ProduceBatchAsync(List<SimpleMessage> messages, string topic = "messages")
        {
            var results = new List<DeliveryResult<string, string>>();
            
            foreach (var message in messages)
            {
                
                var result = await _producer.ProduceAsync(topic, new Message<string, string>
                {
                    Key = message.Id.ToString(),
                    Value = JsonSerializer.Serialize(message)
                });
                
                _logger.LogInformation(
                    "Message {MessageId} delivered to topic {Topic} [Partition: {Partition}, Offset: {Offset}]",
                    message.Id, result.Topic, result.Partition, result.Offset);
                
                results.Add(result);
            }
            
            return results;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
                
            if (disposing)
            {
                _producer?.Flush(TimeSpan.FromSeconds(10));
                _producer?.Dispose();
                _logger.LogInformation("Kafka producer service disposed");
            }
            
            _disposed = true;
        }
    }
} 



curl -X POST \
  -H "Authorization: Bearer $(gcloud auth application-default print-access-token)" \
  -H "Content-Type: application/json" \
  -d '{
    "contents": [
      {
        "role": "user",
        "parts": [
          {
            "file_data": {
              "mime_type": "audio/mp3",
              "uri": "https://cdn.shopify.com/s/files/1/0644/2434/5735/files/Curious_Minds_FINAL.mp3?v=1744860673"
            }
          },
          {
            "text": "Analyze this music track and provide a short description and relevant tags for music search and discovery."
          }
        ]
      }
    ],
    "generationConfig": {
      "response_mime_type": "application/json"
    },
    "responseSchema": {
      "type": "object",
      "properties": {
        "description": {
          "type": "string"
        },
        "tags": {
          "type": "array",
          "items": {
            "type": "string"
          }
        }
      },
      "required": ["description", "tags"]
    }
  }' \
  "https://us-central1-aiplatform.googleapis.com/v1/projects/YOUR_PROJECT_ID/locations/us-central1/publishers/google/models/gemini-2.0-flash:generateContent"