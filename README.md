# Task Management Service

REST API for task management built with .NET 8, following Clean Architecture and the Transactional Outbox pattern for reliable event delivery.

## Projects

| Project                         | Role                                                                           |
| ------------------------------- | ------------------------------------------------------------------------------ |
| `TaskManagement.Application`    | Domain entities (`TaskItem`, `OutboxMessage`), interfaces, DTOs, `TaskService` |
| `TaskManagement.Infrastructure` | EF Core, repositories, `OutboxProcessor`, RabbitMQ publisher                   |
| `TaskManagement.Api`            | ASP.NET Core REST API, OpenTelemetry                                           |
| `TaskManagement.Consumer`       | RabbitMQ consumer — logs task events                                           |
| `TaskManagement.Tests`          | xUnit unit tests: `TaskService`, `OutboxProcessor`, `TaskEventConsumer`        |

## Quick Start

```bash
docker compose up --build
```

## API Reference

| Method   | Endpoint          | Description    |
| -------- | ----------------- | -------------- |
| `POST`   | `/api/tasks`      | Create a task  |
| `GET`    | `/api/tasks`      | Get all tasks  |
| `GET`    | `/api/tasks/{id}` | Get task by ID |
| `PUT`    | `/api/tasks/{id}` | Update a task  |
| `DELETE` | `/api/tasks/{id}` | Delete a task  |

#

## Running Tests

```bash
dotnet test TaskManagement.Tests
```

## OpenTelemetry

The `X-Trace-Id` response header on every API call contains the trace ID for direct lookup.
