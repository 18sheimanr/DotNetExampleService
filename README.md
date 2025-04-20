# KafkaStarter - Learning .NET 9 with Kafka

This project is a comprehensive example application designed to help developers learn C#, .NET 9, and Apache Kafka integration. The solution demonstrates a modern web API with Kafka message production and consumption capabilities.

## Project Structure

- **API**: RESTful service with a Kafka producer singleton service
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
dotnet run --project src/api/KafkaStarter.api.csproj
```

The API will be available at http://localhost:5000 with Swagger UI accessible at the root URL.

### 3. Run the Consumer (in a separate terminal)

```bash
dotnet run --project src/consumer/KafkaStarter.consumer.csproj
```

The consumer will start listening for messages on the "messages" topic.

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