namespace KafkaStarter.Shared.Models
{
    public class SimpleMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Content { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public SimpleMessage(string content)
        {
            Content = content;
        }
    }
}