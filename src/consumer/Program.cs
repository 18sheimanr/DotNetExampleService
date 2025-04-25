using Confluent.Kafka;
using KafkaStarter.Shared.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace KafkaStarter.Consumer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"],
                GroupId = configuration["Kafka:GroupId"],
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            Console.WriteLine("Starting Kafka consumer...");
            Console.WriteLine($"Bootstrap Servers: {consumerConfig.BootstrapServers}");
            Console.WriteLine($"Group ID: {consumerConfig.GroupId}");
            Console.WriteLine($"Topics: messages, LLMResponseGenerated");

            using var consumer = new ConsumerBuilder<string, string>(consumerConfig)
                .SetErrorHandler((_, e) => Console.WriteLine($"Error: {e.Reason}"))
                .Build();

            // Subscribe to both topics, one comes from API and the other comes from STS pipeline
            consumer.Subscribe(new[] { "messages", "LLMResponseGenerated" });

            try
            {
                while (true)
                {
                    try
                    {
                        var consumeResult = consumer.Consume();
                        
                        // Process the message
                        Console.WriteLine($"Message received from topic {consumeResult.Topic}");
                        Console.WriteLine($"Key: {consumeResult.Message.Key}");
                        
                        // Deserialize the message
                        var message = JsonSerializer.Deserialize<SimpleMessage>(consumeResult.Message.Value);
                        
                        if (message != null)
                        {
                            Console.WriteLine($"Message ID: {message.Id}");
                            Console.WriteLine($"Content: {message.Content}");
                            Console.WriteLine($"Timestamp: {message.Timestamp}");
                            
                            await Task.Delay(1000); // Simulate work
                            
                            Console.WriteLine($"Message {message.Id} processed successfully.");
                            Console.WriteLine(new string('-', 50));
                        }
                    }
                    catch (ConsumeException e)
                    {
                        Console.WriteLine($"Error consuming message: {e.Error.Reason}");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Unexpected error: {e.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // The consumer was stopped via cancellation token
            }
            finally
            {
                consumer.Close();
            }
        }
    }
}