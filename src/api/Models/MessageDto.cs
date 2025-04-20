using System.ComponentModel.DataAnnotations;

namespace KafkaStarter.Api.Models
{
    public class MessageRequestDto
    {
        [Required]
        public required string Content { get; set; }
    }

    public class BatchMessageRequestDto
    {
        [Required]
        public required List<MessageRequestDto> Messages { get; set; } = new();
    }

    public class MessageResponseDto
    {
        public Guid MessageId { get; set; }
        public required string Status { get; set; } = "Produced";
        public required string Topic { get; set; } = string.Empty;
        public int Partition { get; set; }
        public long Offset { get; set; }
    }

    public class BatchMessageResponseDto
    {
        public int Count { get; set; }
        public required List<MessageResponseDto> Messages { get; set; } = new();
    }
} 
