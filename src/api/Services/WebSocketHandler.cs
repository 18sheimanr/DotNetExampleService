using System.Net.WebSockets;
using KafkaStarter.Shared.Models;

namespace KafkaStarter.Api.Services
{
    public class WebSocketHandler
    {
        private readonly IOpenAIService _openAIService;
        private readonly IKafkaProducerService _kafkaProducer;

        public WebSocketHandler(IOpenAIService openAIService, IKafkaProducerService kafkaProducer)
        {
            _openAIService = openAIService;
            _kafkaProducer = kafkaProducer;
        }

        public async Task HandleTTSWebSocket(WebSocket webSocket)
        {
            try
            {
                using var audioDataStream = new MemoryStream();
                var buffer = new byte[16384]; // 16KB buffer for audio chunks
                bool isReceivingAudio = true;

                while (isReceivingAudio)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        await audioDataStream.WriteAsync(buffer, 0, result.Count);
                        isReceivingAudio = !result.EndOfMessage;
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        return;
                    }
                    else
                    {
                        isReceivingAudio = false;
                    }

                    if (!isReceivingAudio)
                    {
                        byte[] audioData = audioDataStream.ToArray();
                        Console.WriteLine($"Received {audioData.Length} bytes of audio data");
                        
                        try
                        {
                            // Step 1: Convert audio to text
                            string transcribedText = await _openAIService.TranscribeSpeech(audioData);
                            Console.WriteLine($"Transcribed text: {transcribedText}");
                            
                            // Step 2: Process text with LLM
                            string llmResponse = await _openAIService.ProcessWithLLM(transcribedText, 256);
                            Console.WriteLine($"LLM response: {llmResponse}");
                            
                            // Send LLM response to Kafka
                            var message = new SimpleMessage(llmResponse);
                            await _kafkaProducer.ProduceMessageAsync(message, "LLMResponseGenerated");
                            
                            // Step 3: Convert response to speech and stream back
                            using var responseAudioStream = await _openAIService.TextToSpeech(llmResponse);
                            
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
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing audio: {ex.Message}");
                            if (webSocket.State == WebSocketState.Open)
                            {
                                await webSocket.CloseAsync(
                                    WebSocketCloseStatus.InternalServerError,
                                    "Processing error",
                                    CancellationToken.None);
                            }
                            return;
                        }
                    }
                }

                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Processing complete", CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket error: {ex.Message}");
                if (webSocket.State == WebSocketState.Open)
                {
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.InternalServerError,
                        "Processing error occurred",
                        CancellationToken.None);
                }
            }
        }
    }

    public class WebSocketCommand
    {
        public required string Action { get; set; }
        public required string Text { get; set; }
    }
} 