# TicketFlow .NET

[![Build Status](https://img.shields.io/badge/.NET-10-blueviolet)](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Учебный проект — сервис для управления мероприятиями на базе ASP.NET Core Web API (.NET 10).

---

## 🚀 Текущий статус

- **Спринт 1**: REST CRUD API для управления мероприятиями, хранение данных в памяти ✅
- **Спринт 2**: Глобальная обработка ошибок, фильтрация, пагинация и Unit-тесты ✅
- **Спринт 3**: Асинхронное бронирование билетов, фоновая обработка и многопоточные хранилища ✅
- **Спринт 4**: Потокобезопасность, параллельная обработка заявок и защита от овербукинга (Rich Domain Model, Lock, SemaphoreSlim) ✅
- **Спринт 5**: Переход на PostgreSQL и Entity Framework Core, настройка `AppDbContext`, Fluent API-маппинг, обновление сервисов и тестов ✅
- **Спринт 6**: Миграции EF Core, репозиторный слой, интеграционные тесты с PostgreSQL через Testcontainers ✅
- **Спринт 7**: Переход на чистую архитектуру — разделение проекта на четыре сборки (Domain, Application, Infrastructure, Presentation), интерфейсы портов и composition root ✅
---

## 🏗 Структура проекта

Солюшен разделён на четыре отдельные сборки. Зависимости направлены строго внутрь и проверяются компилятором через `<ProjectReference>`.

```text
├── TicketFlow.Domain/                  # Доменный слой (без внешних зависимостей)
│   ├── Entities/                       # Доменные сущности с бизнес-логикой (Event, Booking)
│   ├── Enums/                          # Доменные перечисления (BookingStatus)
│   └── Exceptions/                     # Доменные исключения (DomainException и наследники)
├── TicketFlow.Application/             # Прикладной слой (зависит только от Domain)
│   ├── Abstractions/                   # Интерфейсы портов (IEventRepository, IBookingRepository)
│   ├── DTOs/
│   │   ├── Bookings/                   # BookingResponseDto
│   │   ├── Events/                     # CreateEventDto, UpdateEventDto, EventInfoDto, EventFiltersDto
│   │   └── Pagination/                 # PaginationParams, PaginatedResult
│   ├── Services/                       # Use cases (IEventService/EventService, IBookingService/BookingService)
│   │   └── Background/                 # Фоновая обработка заявок (BookingProcessingBackgroundService)
│   └── DependencyInjection/            # AddApplicationServices
├── TicketFlow.Infrastructure/          # Инфраструктурный слой (зависит от Application и Domain)
│   ├── Persistence/
│   │   ├── AppDbContext.cs             # DbContext приложения
│   │   ├── Configurations/             # Fluent API-конфигурации сущностей
│   │   └── Migrations/                 # EF Core-миграции схемы БД
│   ├── Repositories/                   # Реализации портов (EventRepository, BookingRepository)
│   └── DependencyInjection/            # AddInfrastructureServices, ApplyMigrations
├── TicketFlow.Presentation/            # Presentation — точка входа (зависит от Application и Infrastructure)
│   ├── Controllers/                    # Эндпоинты REST API (EventsController, BookingsController)
│   ├── Middlewares/                    # Логирование запросов, глобальный перехват ошибок
│   ├── DependencyInjection/            # AddPresentationServices
│   ├── Program.cs                      # Composition root приложения
│   └── appsettings.json                # Настройки приложения
├── TicketFlow.Tests/                   # Юнит-тесты (ссылаются на Domain и Application, порты — моки)
│   ├── Models/                         # Изолированные тесты доменных моделей (EventTests, BookingTests)
│   └── *ServiceTests.cs                # Тесты бизнес-логики и конкурентного доступа
└── TicketFlow.IntegrationTests/        # Интеграционные тесты на PostgreSQL через Testcontainers
```

Направление зависимостей:

```text
Presentation ──> Application <── Infrastructure
      │               │               │
      └──────────> Domain <───────────┘
```

## 🧱 Слои приложения

### Domain — что такое предметная область

Доменные сущности, перечисления и исключения. Слой описывает бизнес-правила в отрыве от способа их применения: `Event` сам следит за количеством мест (`TryReserveSeats`, `ReleaseSeats`), `Booking` сам управляет своим статусом (`Confirm`, `Reject`).

Domain не ссылается ни на один проект и не содержит ни одного NuGet-пакета — ни ASP.NET Core, ни EF Core. Благодаря этому доменные правила тестируются без базы данных и веб-хоста, а смена фреймворка или СУБД слоя не касается.

Нарушение бизнес-правила выражается доменным исключением: `ValidationException`, `NotFoundException`, `NoAvailableSeatsException` наследуются от общего `DomainException`. Domain при этом не знает, что где-то они превратятся в HTTP-коды.

### Application — что приложение умеет делать

Сценарии использования: создать событие, забронировать место, получить статус брони. Здесь же живут DTO — контракты входа и выхода use cases — и фоновая обработка заявок.

Ключевой элемент слоя — **интерфейсы портов** в `Abstractions/`. Application объявляет, что ему нужно от внешнего мира (`IEventRepository`, `IBookingRepository`), но не знает, кто и как это реализует. В этом суть инверсии зависимостей: интерфейс принадлежит тому, кто им пользуется, а не тому, кто его реализует.

Application ссылается только на Domain. Ссылки на Infrastructure нет — это ключевое правило, и его соблюдение проверяет компилятор, а не договорённость в команде.

### Infrastructure — как это технически реализовано

Адаптеры к внешним технологиям: `AppDbContext`, Fluent API-конфигурации, миграции и реализации репозиториев поверх EF Core и PostgreSQL. Слой реализует порты, объявленные в Application.

Здесь сосредоточены все технологические решения. Замена PostgreSQL на другую СУБД или EF Core на Dapper затрагивает только эту сборку: Application и Domain остаются нетронутыми, потому что работают с интерфейсами.

### Presentation — как этим пользоваться снаружи

HTTP-обвязка: контроллеры, middleware и composition root. Контроллеры тонкие — принять запрос, вызвать сервис Application, вернуть результат с нужным кодом ответа. Ни бизнес-логики, ни маппинга доменных сущностей в них нет.

Глобальный обработчик исключений транслирует доменные исключения в HTTP-статусы (`ValidationException` → 400, `NotFoundException` → 404, `NoAvailableSeatsException` → 409) в формате Problem Details. Это единственное место, где домен встречается с протоколом.

Composition root находится в `Program.cs` — он читает конфигурацию и собирает граф зависимостей через extension-методы слоёв:

```csharp
builder.Services.AddInfrastructureServices(connectionString);
builder.Services.AddApplicationServices();
builder.Services.AddPresentationServices();
```

Каждый слой сам знает, что регистрировать, поэтому `Program.cs` остаётся компактным и читается как оглавление приложения.

## ✨ Реализованный функционал

- [x] CRUD операции для мероприятий (`Event`)
- [x] Бизнес-логика вынесена в сервис через DI
- [x] Валидация входных данных (обязательные поля, `EndAt > StartAt`)
- [x] Единый формат ошибок через Problem Details
- [x] Логирование HTTP-запросов
- [x] Swagger UI для тестирования API
- [x] Глобальный обработчик исключений (`Middleware`) с возвратом Problem Details (RFC 7807)
- [x] Фильтрация событий по названию (регистронезависимая) и диапазону дат
- [x] Пагинация результатов (страница, размер страницы)
- [x] Покрытие бизнес-логики `EventService` юнит-тестами (успешные и неуспешные сценарии)
- [x]  Паттерн «быстрый ответ + отложенная обработка» для создания бронирований
- [x]  Фоновый процессор заявок на базе `BackgroundService` с обработкой отмены (`CancellationToken`)
- [x]  Валидация бронирований на уровне сервиса (проверка существования и удаления событий)
- [x] Переход к **Rich Domain Model**: инкапсуляция логики резервирования и возврата мест внутри сущности `Event`
- [x] Синхронизация критических секций: защита от овербукинга с помощью `SemaphoreSlim` при конкурентном создании брони
- [x] Параллельная обработка фоновых задач: использование `Task.WhenAll` и `SemaphoreSlim` для потокобезопасного конкурентного обновления хранилища
- [x] Тестирование конкурентности: написаны юнит-тесты, симулирующие одновременные параллельные запросы к сервису для проверки потокобезопасности
- [x] Хранение данных в PostgreSQL
- [x] Работа с базой данных через Entity Framework Core
- [x] `AppDbContext` с `DbSet<Event>` и `DbSet<Booking>`
- [x] Fluent API-маппинг сущностей через `IEntityTypeConfiguration<T>`
- [x] Адаптация фонового сервиса для работы со scoped-зависимостями через `IServiceScopeFactory`
- [x] Юнит-тесты сервисов изолированы от инфраструктуры: порты подменяются моками (Moq)
- [x] Управление схемой базы данных через EF Core Migrations
- [x] Автоматическое применение миграций при запуске приложения
- [x] Начальная миграция `InitialCreate` для таблиц `events` и `bookings`
- [x] Настроена связь `bookings.event_id → events.id` через внешний ключ
- [x] Реализован репозиторный слой для `Event` и `Booking`
- [x] Сервисы используют репозитории через DI и не обращаются к `AppDbContext` напрямую
- [x] Интеграционные тесты репозиториев на реальной PostgreSQL через Testcontainers
- [x] Интеграционные тесты применения миграций и проверки структуры БД
- [x] Солюшен разделён на четыре сборки: Domain, Application, Infrastructure, Presentation
- [x] Направление зависимостей контролируется компилятором через `<ProjectReference>`
- [x] Domain не содержит ни одной ссылки на сторонние фреймворки
- [x] Интерфейсы портов объявлены в Application, реализации — в Infrastructure
- [x] Регистрация зависимостей каждого слоя вынесена в extension-методы (`AddApplicationServices`, `AddInfrastructureServices`, `AddPresentationServices`)
- [x] Composition root находится в `Program.cs` веб-проекта
- [x] Контроллеры не содержат бизнес-логики и не работают с доменными сущностями напрямую
- [x] Применение миграций инкапсулировано в Infrastructure (`ApplyMigrations`)
- [x] Тестовые проекты ссылаются на конкретные слои, а не на монолитный веб-проект
- [x] Интеграционные тесты сквозного сценария бронирования и фоновой обработки на реальной PostgreSQL
---

## 🛠 Технологический стек

- **Runtime**: .NET 10 (C# 13)
- **Framework**: ASP.NET Core Web API
- **API Documentation**: Swashbuckle (Swagger UI)
- **Database**: PostgreSQL
- **ORM**: Entity Framework Core
- **EF Provider**: Npgsql.EntityFrameworkCore.PostgreSQL
- **Mocking**: Moq (подмена портов в юнит-тестах)
- **Integration Tests Database**: PostgreSQL через Testcontainers
- **Containers**: Testcontainers.PostgreSql

---

## ⚙️ Запуск проекта

### Предварительные требования

- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- PostgreSQL 16+ или Docker
- Docker Compose, если база запускается через `docker compose`
- Docker Desktop / Docker Engine для запуска интеграционных тестов через Testcontainers

### Используемые NuGet-пакеты
Версии NuGet-пакетов управляются централизованно через `Directory.Packages.props`.

Пакеты распределены по слоям — каждый проект объявляет только то, что использует.

`TicketFlow.Domain` — ни одного пакета.

`TicketFlow.Application`:

```bash
- Microsoft.Extensions.DependencyInjection.Abstractions
- Microsoft.Extensions.Hosting.Abstractions
```

`TicketFlow.Infrastructure`:

```bash
- Microsoft.EntityFrameworkCore
- Microsoft.EntityFrameworkCore.Relational
- Npgsql.EntityFrameworkCore.PostgreSQL
```

`TicketFlow.Presentation`:

```bash
- Swashbuckle.AspNetCore
- Microsoft.AspNetCore.OpenApi
- Microsoft.EntityFrameworkCore.Design
```

`Microsoft.EntityFrameworkCore.Design` остаётся в Presentation-проекте, потому что инструменты `dotnet ef` требуют его в startup-проекте.

`TicketFlow.Tests` (юнит-тесты, ссылается на Domain и Application):

```bash
- Microsoft.Extensions.DependencyInjection
- Microsoft.NET.Test.Sdk
- Moq
- xunit
- xunit.runner.visualstudio
```

`TicketFlow.IntegrationTests` (ссылается на Domain, Application и Infrastructure):

```bash
- Microsoft.EntityFrameworkCore
- Microsoft.EntityFrameworkCore.Relational
- Npgsql.EntityFrameworkCore.PostgreSQL
- Testcontainers.PostgreSql
- Microsoft.NET.Test.Sdk
- xunit
- xunit.runner.visualstudio
```

### Настройка подключения к PostgreSQL

Приложение читает строку подключения по ключу DefaultConnection и регистрирует AppDbContext через PostgreSQL-провайдер.
Строка подключения задаётся в `TicketFlow.Presentation/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=eventapi;Username=postgres;Password=postgres"
  }
}
```

### Установка и запуск
 
1. **Клонируйте репозиторий:**
```bash
git clone https://github.com/itsanti/ticketflow.git
cd ticketflow
```

2. Запустите PostgreSQL:
```bash
docker compose up -d
```
 
3. **Соберите проект:**
```bash
dotnet build
```

4. **Запустите приложение:**
```bash
dotnet run --project ./TicketFlow.Presentation/TicketFlow.Presentation.csproj
```
При запуске приложение автоматически применит доступные EF Core-миграции через `app.Services.ApplyMigrations()`.
 
5. **Откройте Swagger UI:**
```
https://localhost:7241/swagger
```
   
### Создание и применение схемы базы данных

Схема базы данных управляется через **EF Core Migrations**.

Применение миграций инкапсулировано в Infrastructure — в `Program.cs` остаётся один вызов:

```csharp
app.Services.ApplyMigrations();
```

Внутри extension-метод создаёт scope, получает `AppDbContext` и вызывает `Database.Migrate()`. Благодаря этому веб-проект не ссылается на EF Core напрямую.

Это применяет все ожидающие миграции и создаёт таблицы:

- `events`
- `bookings`
- `__EFMigrationsHistory`

Таблица `__EFMigrationsHistory` используется EF Core для хранения истории применённых миграций.

Миграции и `AppDbContext` живут в `TicketFlow.Infrastructure`, а точка входа приложения — в `TicketFlow.Presentation`. Поэтому команды `dotnet ef` требуют двух параметров: `--project` указывает сборку с контекстом и миграциями, `--startup-project` — проект, из которого читается конфигурация и строка подключения.

Для создания новой миграции из корня решения:
```bash
dotnet ef migrations add MigrationName \
  --project ./TicketFlow.Infrastructure/TicketFlow.Infrastructure.csproj \
  --startup-project ./TicketFlow.Presentation/TicketFlow.Presentation.csproj \
  --output-dir Persistence/Migrations
```

Для применения миграций вручную:
```bash
dotnet ef database update \
  --project ./TicketFlow.Infrastructure/TicketFlow.Infrastructure.csproj \
  --startup-project ./TicketFlow.Presentation/TicketFlow.Presentation.csproj
```

В обычном сценарии ручной вызов `database update` не требуется, потому что приложение применяет миграции при запуске.

### 📡 API Endpoints
 
| Метод    | Путь              | Описание                        | Статусы           |
|----------|-------------------|---------------------------------|-------------------|
| `GET`    | `/events`         | Список событий с фильтрацией и пагинацией | 200 |
| `GET`    | `/events/{id}`    | Получить событие по ID          | 200, 404          |
| `POST`   | `/events`         | Создать новое событие           | 201, 400          |
| `PUT`    | `/events/{id}`    | Обновить событие целиком        | 200, 400, 404     |
| `DELETE` | `/events/{id}`    | Удалить событие                 | 204, 404          |
| `POST`   | `/events/{id}/book` | Забронировать билет на мероприятие (Отложенная обработка) | 202, 404, 409          |
| `GET`    | `/bookings/{id}`    | Получить текущий статус и информацию о бронировании       | 200, 404          |
 
 Параметры запроса (Query): `title` (строка), `from` (дата), `to` (дата), `page` (int), `pageSize` (int).
 
### Пример запроса (POST /events)
 
```json
{
  "title": "Tech Conference 2026",
  "description": "Ежегодная конференция по современным технологиям",
  "startAt": "2026-04-15T10:00:00",
  "endAt": "2026-04-17T18:00:00",
  "totalSeats": 100
}
```

### Пример ответа (201 Created)
```json
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

### Пример запроса (GET /events/{id})

После создания событие можно получить через GET `/events/{id}`.
 
### Пример ответа (200 OK)
 
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "Tech Conference 2026",
  "description": "Ежегодная конференция по современным технологиям",
  "startAt": "2026-04-15T10:00:00",
  "endAt": "2026-04-17T18:00:00",
  "totalSeats": 100,
  "availableSeats": 100
}
```

### Пример запроса с фильтрацией и пагинацией (GET /events)

URL запроса: `GET /events?title=Tech&page=1&pageSize=10`

### Пример ответа (200 OK)
 
```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "title": "Tech Conference 2026",
      "description": "Ежегодная конференция по современным технологиям",
      "startAt": "2026-04-15T10:00:00Z",
      "endAt": "2026-04-17T18:00:00Z",
      "totalSeats": 100,
      "availableSeats": 100
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 10
}
```

---

## ⚠️ Обработка ошибок

Все ошибки в приложении обрабатываются централизованно. В случае исключения API возвращает ответ в формате Problem Details.

Пример ответа при ошибке (404 Not Found):
```
{
  "status": 404,
  "title": "Not found",
  "detail": "Event with ID ... not found."
}
```

---

## 🧪 Тестирование

В проекте используется два уровня тестирования: unit-тесты и интеграционные тесты. Тестовые проекты ссылаются напрямую на слои, а не на веб-проект: `TicketFlow.Tests` — на `Domain` и `Application`, `TicketFlow.IntegrationTests` — дополнительно на `Infrastructure`.

Для запуска всех тестов:
```bash
dotnet test
```

Для запуска только интеграционных тестов:
```bash
dotnet test ./TicketFlow.IntegrationTests/TicketFlow.IntegrationTests.csproj
```

Для запуска интеграционных тестов должен быть доступен Docker.

### Unit-тесты

Проект `TicketFlow.Tests` проверяет бизнес-логику доменных моделей, сервисов и фоновой обработки.

Юнит-тесты не зависят от инфраструктуры: проект ссылается только на `Domain` и `Application`, а порты `IEventRepository` и `IBookingRepository` подменяются моками через Moq. Состояние хранится в памяти теста, база данных не используется вообще.

Общее окружение собирается в `TestEnvironment`: он настраивает моки портов, регистрирует их синглтонами и вызывает `AddApplicationServices()`, поэтому тесты работают с теми же сервисами, что и приложение:

```csharp
using var env = TestHelpers.Create();
using var scope = env.CreateScope();

env.SeedEvent(TestHelpers.CreateTestEvent(totalSeats: 5));

var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
```

Основные наборы unit-тестов:

- `EventServiceTests` — проверка бизнес-логики управления событиями: создание, обновление, удаление, получение по ID, фильтрация, пагинация и валидация дат.
- `BookingServiceTests` — проверка сценариев бронирования, включая создание заявки, проверку отсутствующих событий, sold out-сценарии и защиту от овербукинга.
- `BookingProcessingBackgroundServiceTests` — проверка фоновой обработки заявок: перевод Pending в Confirmed или Rejected, заполнение ProcessedAt, обработка отмены через CancellationToken.
- `EventTests` и `BookingTests` — изолированные тесты доменных моделей.

### Интеграционные тесты

Проект `TicketFlow.IntegrationTests` проверяет слой доступа к данным на реальной PostgreSQL через `Testcontainers.PostgreSql`.
Интеграционные тесты:
1. Автоматически поднимают PostgreSQL-контейнер.
2. Сбрасывают тестовую базу перед тестами.
3. Применяют EF Core-миграции через `Database.MigrateAsync()`.
4. Проверяют создание таблиц `events`, `bookings` и `__EFMigrationsHistory`.
5. Проверяют внешний ключ `bookings.event_id → events.id`.
6. Покрывают методы `EventRepository`.
7. Покрывают методы `BookingRepository`.
8. Проверяют работу фильтрации, пагинации, добавления, обновления, удаления и выборки данных на реальной PostgreSQL.
9. Покрывают сквозной сценарий бронирования: `BookingServiceTests` вызывает `IBookingService` поверх настоящих репозиториев и проверяет, что бронь сохранена, а место у события зарезервировано одним `SaveChangesAsync`.
10. Покрывают фоновую обработку: `BookingProcessingBackgroundServiceTests` запускает воркер против реальной базы и проверяет, что статус `Confirmed` действительно сохраняется.

Окружение для сервисных тестов собирает `PostgreSqlTestFixture.CreateServiceProvider()` — он вызывает `AddInfrastructureServices(connectionString)` и `AddApplicationServices()`, то есть повторяет composition root приложения.

### Как писать новые тесты

**Выбор уровня.** Правило простое: если проверяется решение, принимаемое кодом, — это unit-тест; если проверяется, что решение доехало до базы, — интеграционный.

| Что проверяем | Куда писать |
|---|---|
| Бизнес-правило сущности (`TryReserveSeats`, `Confirm`) | `TicketFlow.Tests/Models/` |
| Логика use case: валидация, выброс доменных исключений, маппинг в DTO | `TicketFlow.Tests` |
| Взаимодействие с портом (сколько раз вызван `SaveChangesAsync`) | `TicketFlow.Tests`, через `Verify` |
| Трансляция LINQ в SQL: фильтры, сортировка, пагинация | `TicketFlow.IntegrationTests` |
| Сохранение изменений, миграции, внешние ключи, каскады | `TicketFlow.IntegrationTests` |
| Сценарий, затрагивающий несколько сущностей за одно сохранение | `TicketFlow.IntegrationTests` |

**Именование.** `Method_ShouldExpectedResult_WhenCondition`, например `CreateBookingAsync_ShouldThrowNoAvailableSeatsException_WhenEventIsSoldOut`. Часть `_When...` опускается, если условие очевидно из названия теста.

**Unit-тест.** Всё окружение даёт `TestEnvironment`: `SeedEvent` / `SeedBooking` для arrange, `FindEvent` / `FindBooking` / `AllBookings` для assert, `CreateScope()` — когда важно, что сервис scoped. Обращаться к `EventRepository` / `BookingRepository` напрямую нужно только для `Verify`, то есть когда проверяется факт вызова, а не результат.

Чего в юнит-тестах делать не стоит:

- добавлять в `TicketFlow.Tests` ссылку на `Infrastructure` — тогда тест перестанет быть юнит-тестом, а моки портов потеряют смысл;
- проверять поведение хранилища. Логика фильтрации в моке `GetPagedAsync` повторяет `EventRepository` лишь приблизительно и не заменяет SQL — новые правила выборки проверяются интеграционным тестом;
- полагаться на то, что `SaveChangesAsync` что-то меняет: в моках это пустышка, объекты в списках и так изменяются по ссылке.

**Интеграционный тест.** Класс помечается `[Collection("PostgreSql collection")]` — коллекция отключает параллельный запуск, потому что база одна на всех. Каждый тест начинается с `await _fixture.ResetDatabaseAsync()`: база пересоздаётся и миграции применяются заново, поэтому тесты не зависят от порядка запуска.

Дальше два варианта. Для проверки репозитория или схемы — `_fixture.CreateContext()` и работа напрямую с `AppDbContext`. Для сценария уровня Application — `_fixture.CreateServiceProvider()`, scope и получение сервиса через DI. Результат всегда проверяется из **нового** контекста с `AsNoTracking()`, иначе можно прочитать объект из кэша change tracker'а и не заметить, что запись в базу не дошла.

Даты в тестах — только UTC (`DateTime.UtcNow`): колонки имеют тип `timestamp with time zone`, и Npgsql отвергнет значение с `Kind = Unspecified`.

**Про время выполнения.** Юнит-тесты не обращаются ни к базе, ни к диску. Интеграционные поднимают Docker-контейнер и пересоздают схему на каждый тест, а тест фоновой обработки дополнительно ждёт цикл воркера — это самая медленная часть набора. Поэтому в интеграционный проект стоит выносить только то, что действительно требует настоящей базы.

---

## 🗃️ Репозиторный слой

Доступ к базе данных инкапсулирован в репозиториях, разнесённых по двум слоям:

- интерфейсы портов — `IEventRepository`, `IBookingRepository` — объявлены в `TicketFlow.Application/Abstractions/`;
- реализации-адаптеры — `EventRepository`, `BookingRepository` — находятся в `TicketFlow.Infrastructure/Repositories/` и работают через `AppDbContext`.

Сервисы не обращаются к `AppDbContext` напрямую и не знают о конкретных реализациях — они получают интерфейсы через DI, а связывание происходит в composition root.

Репозитории отвечают только за доступ к данным:

- поиск сущностей по ID;
- добавление сущностей;
- удаление сущностей;
- выборку списка событий с фильтрацией и пагинацией;
- выборку pending-бронирований;
- сохранение изменений через `SaveChangesAsync()`.

Бизнес-логика остаётся в сервисах и доменных моделях.


## 📅 Документация подсистемы бронирования

### 🎟 Модель данных события (Event)
Сущность `Event` использует концепцию Rich Domain Model и самостоятельно управляет количеством билетов, предотвращая овербукинг на уровне бизнес-логики:
* `Id` (`Guid`) — уникальный идентификатор события.
* `Title`, `Description` — базовая информация о мероприятии.
* `StartAt`, `EndAt` (`DateTime`) — временные рамки проведения.
* `TotalSeats` (`int`) — общее (максимальное) количество мест на мероприятии. Задается при создании и должно быть больше нуля.
* `AvailableSeats` (`int`) — текущее количество свободных мест. Уменьшается при успешном создании заявки и восстанавливается, если фоновый сервис отклоняет бронь.

### 📦 Модель данных бронирования (Booking)
Сущность `Booking` описывает заявку на бронирование места на конкретное мероприятие и содержит поля:
* `Id` (`Guid`) — уникальный идентификатор брони.
* `EventId` (`Guid`) — идентификатор связанного события.
* `Status` (`BookingStatus`) — текущее состояние заявки. Принимает значения:
  * `Pending` — бронь создана и ожидает обработки фоновым сервисом.
  * `Confirmed` — бронирование успешно подтверждено.
  * `Rejected` — бронирование отклонено.
* `CreatedAt` (`DateTime`) — дата и время инициализации бронирования.
* `ProcessedAt` (`DateTime?`) — дата и время обработки заявки внешней системой (заполняется фоновым сервисом).

### ⚙️ Логика фоновой обработки (Background Processing)
Для реализации паттерна «быстрый ответ + отложенная обработка» запущен фоновый хостинг-сервис `BookingProcessingBackgroundService`:

1. Раз в 5 секунд сервис создаёт scope через `IServiceScopeFactory`, получает `IBookingRepository` через DI и извлекает идентификаторы бронирований со статусом `Pending`.
2. Для каждой найденной заявки создаётся отдельный scope. Внутри scope используются scoped-репозитории `IBookingRepository` и `IEventRepository`, которые работают через свой экземпляр `AppDbContext`.
3. После искусственной задержки в 2 секунды бронь переводится в `Confirmed`, либо в `Rejected`, если связанное событие не найдено или произошла ошибка.
4. Изменения сохраняются через `SaveChangesAsync()` репозитория.

> ⏳ **Важное примечание по таймингам:** Из-за интервала опроса хранилища (5 сек) и времени выполнения внешней интеграции (2 сек), суммарное ожидание смены статуса с `Pending` на финальный (`Confirmed`/`Rejected`) после выполнения POST-запроса может занимать **от 2 до 7 секунд**. Для демонстрационных целей текущего спринта такие задержки являются ожидаемыми и нормальными.


## 🔒 Потокобезопасность и многопоточность

Для защиты от овербукинга при конкурентном создании бронирований используется `static SemaphoreSlim` в `BookingService`.

`BookingService` зарегистрирован как scoped-сервис, поэтому обычный instance-семафор защищал бы только один экземпляр сервиса. `static SemaphoreSlim` синхронизирует критическую секцию между разными экземплярами `BookingService` внутри одного процесса приложения.

Критическая секция включает:

1. загрузку события из базы данных;
2. проверку доступных мест через `TryReserveSeats()`;
3. уменьшение `AvailableSeats`;
4. создание новой брони;
5. сохранение изменений через `SaveChangesAsync()`.

Так как `AppDbContext` отслеживает и изменённое событие, и новую бронь, один вызов `SaveChangesAsync()` сохраняет оба изменения.

`BookingProcessingBackgroundService` не хранит общий `DbContext` и не использует общий in-memory store. Для работы со scoped-зависимостями он использует `IServiceScopeFactory`: сначала создаёт scope для получения списка `Pending`-бронирований через `IBookingRepository`, затем отдельный scope для обработки каждой брони через `IBookingRepository` и `IEventRepository`.


### 🔄 Пример сквозного сценария использования

**Шаг 1: Создание бронирования**
Клиент отправляет запрос на бронирование места на существующее событие:
`POST /events/27fffa2f-fe74-42ea-8baa-4e7efa57e541/book`

**Сценарий А: Места есть (202 Accepted)**
Сервис мгновенно выполняет проверку мест, резервирует одно место и возвращает статус `202 Accepted`.
В заголовках ответа (`Headers`) передается ссылка на проверку статуса, а в теле — объект со статусом `Pending`:
* **Заголовок Location:** `https://localhost:7241/bookings/14770068-9649-4b33-816c-9481019d2611` 
* **Тело ответа:**
```json
{
  "id": "14770068-9649-4b33-816c-9481019d2611",
  "eventId": "27fffa2f-fe74-42ea-8baa-4e7efa57e541",
  "status": "Pending",
  "createdAt": "2026-05-18T20:37:48Z",
  "processedAt": null
}
```
**Сценарий Б: Мест нет (409 Conflict)**
Если лимит билетов исчерпан (`AvailableSeats == 0`), API мгновенно прерывает операцию и возвращает ошибку в формате Problem Details:
```
{
  "status": 409,
  "title": "Conflict",
  "detail": "Cannot create booking. No available seats for event with ID 27fffa2f-fe74-42ea-8baa-4e7efa57e541."
}
```

**Шаг 2: Проверка статуса (Сразу после создания)**
При переходе по адресу из заголовка Location (`GET /bookings/14770068-9649-4b33-816c-9481019d2611`)  в первые секунды клиент видит статус ожидания:
```json
{
  "id": "14770068-9649-4b33-816c-9481019d2611",
  "eventId": "27fffa2f-fe74-42ea-8baa-4e7efa57e541",
  "status": "Pending",
  "createdAt": "2026-05-18T20:37:48Z",
  "processedAt": null
}
```
**Шаг 3: Проверка статуса (Спустя несколько секунд)**
После того как фоновый сервис обработает заявку, повторный запрос к эндпоинту `GET /bookings/{id}` вернет обновленный объект с финальным статусом (в зависимости от параметров события это будет либо успешный `Confirmed`, либо отклоненный `Rejected` в случае нарушения бизнес-правил):

```json
{
  "id": "14770068-9649-4b33-816c-9481019d2611",
  "eventId": "27fffa2f-fe74-42ea-8baa-4e7efa57e541",
  "status": "Confirmed",
  "createdAt": "2026-05-18T20:37:48Z",
  "processedAt": "2026-05-18T20:37:50Z"
}
```

---

### 💥 Сценарий защиты от овербукинга (Concurrency Scenario)

Представим ситуацию:
1. На мероприятие осталось ровно **5 мест**.
2. **20 пользователей** одновременно нажимают кнопку «Забронировать».
3. Благодаря использованию конструкции `SemaphoreSlim` в `BookingService`, запросы выстраиваются в строгую очередь на уровне процессорных потоков.
4. Первые **5 потоков** (если доступно 5 мест) успешно вызывают `TryReserveSeats()`, уменьшают счетчик до 0 и получают ответ `202 Accepted`. Их брони уходят в статус `Pending`.
5. Остальные **15 запросов** мгновенно получают отказ на уровне бизнес-логики модели, и API возвращает им `409 Conflict`.
6. Фоновый сервис параллельно переводит эти 5 успешных броней в статус `Confirmed`, создавая отдельный scope и отдельный `AppDbContext` для обработки каждой брони.
7. Если в процессе работы фонового сервиса с одной из этих 5 броней произойдет непредвиденная ошибка (исключение), воркер переведет бронь в статус `Rejected` и автоматически вызовет `eventItem.ReleaseSeats()`, возвращая место обратно в продажу для других пользователей.

---

## 📝 Лицензия

Распространяется под лицензией MIT.

---

Автор: Александр Куров [@itsanti](https://github.com/itsanti)

Курс: [Продвинутая разработка на C# и .NET ](https://practicum.yandex.ru/middle-csharp) (Яндекс Практикум)

