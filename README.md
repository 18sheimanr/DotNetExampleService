# STS/AI/Kafka Starter Project - Learning .NET 9 with Kafka and audio streaming

This project is a comprehensive example application designed to help developers learn C#, .NET 9, and Apache Kafka integration. The solution demonstrates a modern web API with Kafka message production and consumption capabilities.

The frontend allows a user to record an audio segment (no automatic VAD) and sends it to the .NET backend over websocket.
Next, the backend transcribes it to text, produces a response via LLM, and finally streams audio back to the frontend via the same websocket, closing it when done.
Responses generated are sent to the LLMResponseGenerated Kafka topic.

## Project Structure

- **API**: RESTful service with a Kafka producer singleton service and openAIService for STS AI
- **Consumer**: Console application that consumes messages from Kafka
- **Shared**: Common models and utilities shared between projects

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Visual Studio Code](https://code.visualstudio.com/) with C# extension

## Getting Started

### 1. Start Kafka Environment

The project includes a Docker Compose file that sets up Kafka, Zookeeper, and Kafka UI.

```bash
docker-compose up -d
```

This will start:
- Zookeeper on port 2181
- Kafka on port 9092
- Kafka UI on port 8080 (accessible at http://localhost:8080)

### 2. Run the API

```bash
dotnet run --project src/api/KafkaStart.api.csproj
```

The API will be available at http://localhost:5000 with Swagger UI accessible at the root URL.

### 3. Run the frontend React App (in a separate terminal)

```bash
cd ./frontend && npm start
```

### 4. Run the Consumer (in a separate terminal) (also, optional, it doesn't do anything at the moment lol)

```bash
dotnet run --project src/consumer/KafkaStart.consumer.csproj
```

The consumer will start listening for messages on the "messages" and "LLMResponseGenerated" topic.
The former is produced from the /messages http endpoint, the latter from the STS AI processing

## Using the Application

### Sending a Single Message

Using curl:
```bash
curl -X POST http://localhost:5000/api/message \
  -H "Content-Type: application/json" \
  -d '{"content":"Hello, Kafka!"}'
```

### Sending Multiple Messages

Using curl:
```bash
curl -X POST http://localhost:5000/api/message/batch \
  -H "Content-Type: application/json" \
  -d '{"messages":[{"content":"Message 1"},{"content":"Message 2"}]}'
```

### Viewing Messages

Use the Kafka UI at http://localhost:8080 to explore topics and messages

## TODO: Next Steps for Learning

- Add authentication to the API
- Implement more complex message processing logic
- Add unit and integration tests
- Store all transcribed and LLM generated messages for full conversations instead of zero shot
