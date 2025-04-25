using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KafkaStarter.Api.Services
{

    public interface IOpenAIService
    {
        Task<string> TranscribeSpeech(byte[] audioData);
        Task<string> ProcessWithLLM(string inputText, int maxTokens = 512);
        Task<Stream> TextToSpeech(string text);
    }

    public class OpenAIService : IOpenAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        
        public OpenAIService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = Environment.GetEnvironmentVariable("OPENAI_KEY");
                      
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("OpenAI API key is not configured. Set the OPENAI_API_KEY environment variable or configure it in appsettings.json");
            }
            
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        public async Task<string> TranscribeSpeech(byte[] audioData)
        {
            using var formData = new MultipartFormDataContent();
            var audioBinaryContent = new ByteArrayContent(audioData);
            audioBinaryContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/webm");
            formData.Add(audioBinaryContent, "file", "audio.webm");
            formData.Add(new StringContent("whisper-1"), "model");
            formData.Add(new StringContent("en"), "language");

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/audio/transcriptions", formData);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"OpenAI API error: {response.StatusCode} - {errorContent}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var transcriptionResult = JsonSerializer.Deserialize<TranscriptionResult>(jsonResponse);
            return transcriptionResult!.Text;
        }

        public async Task<string> ProcessWithLLM(string inputText, int maxTokens = 512)
        {
            var requestBody = new
            {
                model = "gpt-4o",
                messages = new[]
                {
                    new { role = "user", content = inputText }
                },
                max_tokens = maxTokens
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            if(!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"OpenAI API error: {response.StatusCode} - {errorContent}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var completionResult = JsonSerializer.Deserialize<CompletionResult>(jsonResponse);
            
            return completionResult?.Choices[0].Message.Content;
        }

        // Quickly provides a text to speech STREAM not a ready audio response
        public async Task<Stream> TextToSpeech(string text)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/audio/speech")
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    model = "tts-1",
                    input = text,
                    voice = "nova",
                    response_format = "mp3",
                    stream = true
                }), Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadAsStreamAsync();
        }
    }

    public class TranscriptionResult
    {
        [JsonPropertyName("text")]
        public required string Text { get; set; }
    }

    public class CompletionResult
    {
        [JsonPropertyName("choices")]
        public required Choice[] Choices { get; set; }
    }

    public class Choice
    {
        [JsonPropertyName("message")]
        public required Message Message { get; set; }
    }

    public class Message
    {
        [JsonPropertyName("content")]
        public required string Content { get; set; }
    }
} 