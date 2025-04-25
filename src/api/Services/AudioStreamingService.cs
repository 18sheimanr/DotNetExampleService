using System.Net.WebSockets;
using KafkaStarter.Shared.Models;

namespace KafkaStarter.Api.Services
{
    public class AudioStreamingService(IOpenAIService openAIService, IKafkaProducerService kafkaProducer)
    {
        private readonly IOpenAIService _openAIService = openAIService;
        private readonly IKafkaProducerService _kafkaProducer = kafkaProducer;

        public async Task HandleTTSWebSocket(WebSocket webSocket)
        {
            try
            {
                byte[] audioData = await ReceiveAudioDataAsync(webSocket);
                if (audioData.Length > 0)
                {
                    await ProcessAudioAsync(webSocket, audioData);
                }
                await SafelyCloseWebSocketAsync(webSocket, WebSocketCloseStatus.NormalClosure, "Processing complete");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket error: {ex.Message}");
                await SafelyCloseWebSocketAsync(webSocket, WebSocketCloseStatus.InternalServerError, "Processing error occurred");
            }
        }

        private async Task<byte[]> ReceiveAudioDataAsync(WebSocket webSocket)
        {
            using var audioDataStream = new MemoryStream();
            var buffer = new byte[16384]; // 16KB buffer for audio chunks
            
            while (true)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                
                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    await audioDataStream.WriteAsync(buffer, 0, result.Count);
                    if (result.EndOfMessage)
                        break;
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await SafelyCloseWebSocketAsync(webSocket, WebSocketCloseStatus.NormalClosure, "Closing");
                    return Array.Empty<byte>();
                }
                else
                {
                    break;
                }
            }
            
            byte[] audioData = audioDataStream.ToArray();
            Console.WriteLine($"Received {audioData.Length} bytes of audio data");
            return audioData;
        }

        private async Task ProcessAudioAsync(WebSocket webSocket, byte[] audioData)
        {
            try
            {
                // Step 1: Convert audio to text
                string transcribedText = await _openAIService.TranscribeSpeech(audioData);
                Console.WriteLine($"Transcribed text: {transcribedText}");
                
                // Step 2: Process text with LLM
                string llmResponse = await _openAIService.ProcessWithLLM(transcribedText);
                Console.WriteLine($"LLM response: {llmResponse}");
                
                // Send LLM response to Kafka
                var message = new SimpleMessage(llmResponse);
                await _kafkaProducer.ProduceMessageAsync(message, "LLMResponseGenerated");
                
                // Step 3: Convert response to speech and stream back
                await SendResponseAudioAsync(webSocket, llmResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing audio: {ex.Message}");
                await SafelyCloseWebSocketAsync(webSocket, WebSocketCloseStatus.InternalServerError, "Processing error");
                throw;
            }
        }

        private async Task SendResponseAudioAsync(WebSocket webSocket, string text)
        {
            using var responseAudioStream = await _openAIService.TextToSpeech(text);
            
            byte[] audioBuffer = new byte[4096];
            int bytesRead;
            while ((bytesRead = await responseAudioStream.ReadAsync(audioBuffer, 0, audioBuffer.Length)) > 0)
            {
                await webSocket.SendAsync(
                    new ArraySegment<byte>(audioBuffer, 0, bytesRead),
                    WebSocketMessageType.Binary,
                    bytesRead < audioBuffer.Length,
                    CancellationToken.None);
            }
        }

        private async Task SafelyCloseWebSocketAsync(WebSocket webSocket, WebSocketCloseStatus status, string description)
        {
            if (webSocket.State == WebSocketState.Open)
            {
                await webSocket.CloseAsync(status, description, CancellationToken.None);
            }
        }
    }

    public class WebSocketCommand
    {
        public required string Action { get; set; }
        public required string Text { get; set; }
    }
} 