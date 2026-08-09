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
- **Спринт 8**: JWT-аутентификация и ролевая авторизация (сущность `User`, роли `Admin`/`User`), доменные правила бронирования — запрет брони прошедшего события, лимит активных броней на пользователя, отмена брони с проверкой прав владельца ✅
---

## 🏗 Структура проекта

Солюшен разделён на четыре отдельные сборки. Зависимости направлены строго внутрь и проверяются компилятором через `<ProjectReference>`.

```text
├── TicketFlow.Domain/                  # Доменный слой (без внешних зависимостей)
│   ├── Entities/                       # Доменные сущности с бизнес-логикой (Event, Booking, User)
│   ├── Enums/                          # Доменные перечисления (BookingStatus, UserRole)
│   └── Exceptions/                     # Доменные исключения (DomainException и наследники)
├── TicketFlow.Application/             # Прикладной слой (зависит только от Domain)
│   ├── Abstractions/                   # Интерфейсы портов (IEventRepository, IBookingRepository, IUserRepository, IPasswordHasher, IJwtTokenGenerator)
│   ├── DTOs/
│   │   ├── Bookings/                   # BookingResponseDto
│   │   ├── Events/                     # CreateEventDto, UpdateEventDto, EventInfoDto, EventFiltersDto
│   │   ├── Users/                      # RegisterUserDto, LoginUserDto, AuthResponseDto
│   │   └── Pagination/                 # PaginationParams, PaginatedResult
│   ├── Services/                       # Use cases (IEventService/EventService, IBookingService/BookingService, IUserService/UserService)
│   │   └── Background/                 # Фоновая обработка заявок (BookingProcessingBackgroundService)
│   └── DependencyInjection/            # AddApplicationServices
├── TicketFlow.Infrastructure/          # Инфраструктурный слой (зависит от Application и Domain)
│   ├── Persistence/
│   │   ├── AppDbContext.cs             # DbContext приложения
│   │   ├── Configurations/             # Fluent API-конфигурации сущностей (в т.ч. UserConfiguration)
│   │   └── Migrations/                 # EF Core-миграции схемы БД
│   ├── Repositories/                   # Реализации портов (EventRepository, BookingRepository, UserRepository)
│   ├── Security/                       # PasswordHasher (SHA-256), JwtTokenGenerator, JwtOptions
│   └── DependencyInjection/            # AddInfrastructureServices, ApplyMigrations
├── TicketFlow.Presentation/            # Presentation — точка входа (зависит от Application и Infrastructure)
│   ├── Controllers/                    # Эндпоинты REST API (EventsController, BookingsController, AuthController)
│   ├── Middlewares/                    # Логирование запросов, глобальный перехват ошибок
│   ├── DependencyInjection/            # AddPresentationServices (MVC, JWT-аутентификация, Swagger)
│   ├── Program.cs                      # Composition root приложения
│   ├── appsettings.json                 # Несекретные настройки (Issuer/Audience/ExpirationMinutes)
│   └── appsettings.Development.json     # Dev-секреты (Jwt:Secret, строка подключения к локальному Postgres)
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

Доменные сущности, перечисления и исключения. Слой описывает бизнес-правила в отрыве от способа их применения: `Event` сам следит за количеством мест (`TryReserveSeats`, `ReleaseSeats`), `Booking` сам управляет своим статусом (`Confirm`, `Reject`, `Cancel`).

Сущность `User` хранит логин, хеш пароля и роль (`UserRole`: `User` / `Admin`) и, как остальные сущности, создаётся через фабричный метод `Create`, а не публичный конструктор. `Booking` связан с пользователем через `UserId` и умеет отменять себя: `Cancel()` переводит бронь в статус `Cancelled`, но запрещает повторную отмену уже отменённой или отклонённой брони.

Domain не ссылается ни на один проект и не содержит ни одного NuGet-пакета — ни ASP.NET Core, ни EF Core. Благодаря этому доменные правила тестируются без базы данных и веб-хоста, а смена фреймворка или СУБД слоя не касается.

Нарушение бизнес-правила выражается доменным исключением: `ValidationException`, `NotFoundException`, `NoAvailableSeatsException`, `EventAlreadyStartedException`, `BookingLimitExceededException`, `ForbiddenException`, `InvalidOperationDomainException` наследуются от общего `DomainException`. Domain при этом не знает, что где-то они превратятся в HTTP-коды.

### Application — что приложение умеет делать

Сценарии использования: создать событие, забронировать место, получить статус брони, зарегистрировать и авторизовать пользователя. Здесь же живут DTO — контракты входа и выхода use cases — и фоновая обработка заявок.

Ключевой элемент слоя — **интерфейсы портов** в `Abstractions/`. Application объявляет, что ему нужно от внешнего мира (`IEventRepository`, `IBookingRepository`, `IUserRepository`), но не знает, кто и как это реализует. Помимо репозиториев здесь же объявлены порты для аутентификации: `IPasswordHasher` (хеширование и проверка пароля) и `IJwtTokenGenerator` (выпуск JWT по данным пользователя). Application не знает, что хеш считается через SHA-256, а токен подписывается HMAC-SHA256 — это детали Infrastructure. В этом суть инверсии зависимостей: интерфейс принадлежит тому, кто им пользуется, а не тому, кто его реализует.

Application ссылается только на Domain. Ссылки на Infrastructure нет — это ключевое правило, и его соблюдение проверяет компилятор, а не договорённость в команде.

### Infrastructure — как это технически реализовано

Адаптеры к внешним технологиям: `AppDbContext`, Fluent API-конфигурации, миграции и реализации репозиториев поверх EF Core и PostgreSQL. Слой реализует порты, объявленные в Application.

Здесь же живёт `Security/`: `PasswordHasher` (реализация `IPasswordHasher` на `System.Security.Cryptography.SHA256`) и `JwtTokenGenerator` (реализация `IJwtTokenGenerator` на `System.IdentityModel.Tokens.Jwt`), плюс `JwtOptions` — параметры токена, привязанные к секции `Jwt` конфигурации.

Здесь сосредоточены все технологические решения. Замена PostgreSQL на другую СУБД, EF Core на Dapper или SHA-256 на BCrypt затрагивает только эту сборку: Application и Domain остаются нетронутыми, потому что работают с интерфейсами.

### Presentation — как этим пользоваться снаружи

HTTP-обвязка: контроллеры, middleware и composition root. Контроллеры тонкие — принять запрос, вызвать сервис Application, вернуть результат с нужным кодом ответа. Ни бизнес-логики, ни маппинга доменных сущностей в них нет.

Аутентификация подключена через `Microsoft.AspNetCore.Authentication.JwtBearer` — middleware проверяет подпись и срок жизни токена, а `[Authorize]` / `[Authorize(Roles = "Admin")]` на контроллерах решают, кому доступен эндпоинт. Идентификатор текущего пользователя `BookingsController` читает из claims токена (`ClaimTypes.NameIdentifier`) и передаёт в сервисы бронирования.

Глобальный обработчик исключений транслирует доменные исключения в HTTP-статусы (`ValidationException` → 400, `NotFoundException` → 404, `NoAvailableSeatsException` → 409, `EventAlreadyStartedException` → 400, `BookingLimitExceededException` → 409, `ForbiddenException` → 403) в формате Problem Details. Ответы 401/403, которые выдаёт сама авторизационная middleware ASP.NET Core (то есть без единого доменного исключения), через `GlobalExceptionHandlingMiddleware` не проходят — но выглядят так же: оба пути пишут ответ через один `IProblemDetailsService`, а заголовки ошибок для всех статус-кодов настроены в одном месте (`CustomizeProblemDetails` в `AddPresentationServices`). Это единственное место, где домен встречается с протоколом.

Composition root находится в `Program.cs` — он читает конфигурацию и собирает граф зависимостей через extension-методы слоёв:

```csharp
builder.Services.AddInfrastructureServices(connectionString, builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddPresentationServices(builder.Configuration);
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
- [x] `AppDbContext` с `DbSet<Event>`, `DbSet<Booking>` и `DbSet<User>`
- [x] Fluent API-маппинг сущностей через `IEntityTypeConfiguration<T>`
- [x] Адаптация фонового сервиса для работы со scoped-зависимостями через `IServiceScopeFactory`
- [x] Юнит-тесты сервисов изолированы от инфраструктуры: порты подменяются моками (Moq)
- [x] Управление схемой базы данных через EF Core Migrations
- [x] Автоматическое применение миграций при запуске приложения
- [x] Начальная миграция `InitialCreate` для таблиц `events` и `bookings`
- [x] Настроена связь `bookings.event_id → events.id` через внешний ключ
- [x] Реализован репозиторный слой для `Event`, `Booking` и `User`
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
- [x] Сущность `User` (логин, хеш пароля, роль) создаётся через фабричный метод `Create`
- [x] Перечисление ролей `UserRole` (`User`, `Admin`)
- [x] Бронирование связано с пользователем через `UserId`; миграция добавляет таблицу `users` и колонку с внешним ключом в `bookings`
- [x] Доменное правило: запрет бронирования уже начавшегося события (`EventAlreadyStartedException`)
- [x] Доменное правило: лимит активных броней на пользователя (`BookingLimitExceededException`)
- [x] Доменное правило: отмена брони с проверкой владельца — свою бронь отменяет любой пользователь, чужую только Admin (`ForbiddenException` при нарушении)
- [x] Хеширование паролей через BCrypt (`IPasswordHasher`/`PasswordHasher`), с поддержкой верификации legacy-хешей SHA-256
- [x] Генерация JWT-токена по данным пользователя (`IJwtTokenGenerator`/`JwtTokenGenerator`), параметры вынесены в конфигурацию (`appsettings.json` + секреты — см. [настройку JWT](#настройка-jwt-аутентификации-и-подключения-к-postgresql))
- [x] Регистрация (`POST /auth/register`) и вход (`POST /auth/login`) с выдачей JWT
- [x] JWT-аутентификация в Web API (`AddJwtBearer`) и авторизация по ролям (`[Authorize(Roles = "Admin")]`)
- [x] Идентификатор текущего пользователя читается из claims токена и передаётся в сценарии бронирования и отмены
- [x] Управление событиями (`POST`/`PUT`/`DELETE /events`) доступно только роли Admin
- [x] `DELETE /bookings/{id}` — отмена брони: владелец отменяет свою, администратор — любую
- [x] При неверных учётных данных на входе возвращается одно и то же сообщение (защита от перебора логинов)
- [x] Swagger настроен для работы с JWT (кнопка Authorize)
- [x] Единый формат Problem Details для доменных исключений и встроенных ответов 401/403 (`CustomizeProblemDetails`)
- [x] Юнит-тесты новых доменных правил: бронирование прошедшего события, лимит активных броней, независимость лимитов разных пользователей
---

## 🛠 Технологический стек

- **Runtime**: .NET 10 (C# 13)
- **Framework**: ASP.NET Core Web API
- **API Documentation**: Swashbuckle (Swagger UI)
- **Database**: PostgreSQL
- **ORM**: Entity Framework Core
- **EF Provider**: Npgsql.EntityFrameworkCore.PostgreSQL
- **Authentication**: JWT Bearer (Microsoft.AspNetCore.Authentication.JwtBearer)
- **Token generation**: System.IdentityModel.Tokens.Jwt
- **Password hashing**: SHA-256 (System.Security.Cryptography)
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
- System.IdentityModel.Tokens.Jwt
- Microsoft.Extensions.Options.ConfigurationExtensions
```

`TicketFlow.Presentation`:

```bash
- Swashbuckle.AspNetCore
- Microsoft.AspNetCore.OpenApi
- Microsoft.EntityFrameworkCore.Design
- Microsoft.AspNetCore.Authentication.JwtBearer
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
- Microsoft.Extensions.Configuration
- Npgsql.EntityFrameworkCore.PostgreSQL
- Testcontainers.PostgreSql
- Microsoft.NET.Test.Sdk
- xunit
- xunit.runner.visualstudio
```

`Microsoft.Extensions.Configuration` нужен тестовому окружению (`PostgreSqlTestFixture`), чтобы собрать in-memory конфигурацию с параметрами `Jwt` — `AddInfrastructureServices` требует `IConfiguration`, а у тестового проекта нет своего `appsettings.json`.

### Настройка JWT-аутентификации и подключения к PostgreSQL

`Jwt:Secret` и `ConnectionStrings:DefaultConnection` (с паролем БД) — секреты и **не хранятся** в `TicketFlow.Presentation/appsettings.json`. Этот файл содержит только несекретные параметры:

```json
{
  "Jwt": {
    "Issuer": "TicketFlow",
    "Audience": "TicketFlowClient",
    "ExpirationMinutes": 60
  }
}
```

- `Secret` — ключ подписи HMAC-SHA256, должен быть не короче 256 бит (32 байта / 64 hex-символа), иначе подпись слабая. Генерируется командой `openssl rand -hex 32`.
- `Issuer` / `Audience` — сверяются при валидации токена (`ValidateIssuer`, `ValidateAudience` в `AddJwtBearer`).
- `ExpirationMinutes` — время жизни токена в минутах.

**Локальная разработка.** Значения для локального docker-postgres лежат в `TicketFlow.Presentation/appsettings.Development.json` (загружается автоматически при `ASPNETCORE_ENVIRONMENT=Development`, как в `launchSettings.json`). Это dev-only секрет, актуальный только для контейнера из `docker-compose.yml`, поэтому хранить его в репозитории допустимо. При желании его можно вынести из файла в `dotnet user-secrets` (проект уже помечен `UserSecretsId`):

```bash
dotnet user-secrets set "Jwt:Secret" "<значение>" --project TicketFlow.Presentation
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<значение>" --project TicketFlow.Presentation
```

**Прод и другие окружения.** Секреты задаются переменными окружения — ASP.NET Core автоматически превращает `__` в `:` при биндинге конфигурации:

```bash
export Jwt__Secret="$(openssl rand -hex 32)"
export ConnectionStrings__DefaultConnection="Host=...;Port=5432;Database=eventapi;Username=...;Password=..."
```

Переменные с префиксом `ASPNETCORE_` (например, `ASPNETCORE_Jwt__Secret`) тоже работают — хост-конфигурация ASP.NET Core сама снимает этот префикс на старте. Если ни один из этих источников не задаёт `Jwt:Secret` в окружении, отличном от Development, приложение упадёт при старте (`InvalidOperationException` в `AddAuthenticationServices`) — это осознанный fail-fast, а не баг.


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
- `users`
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

> ⚠️ Если в базе уже есть данные (например, брони из прошлых спринтов), миграция `AddUsersAndBookingOwnership` не применится — новая колонка `bookings.user_id` обязана ссылаться на существующего пользователя, а таблица `users` на момент миграции пуста. Для локальной разработки проще всего пересоздать базу (`docker compose down -v && docker compose up -d`) и накатить миграции на чистую схему.

### 📡 API Endpoints
 
| Метод    | Путь              | Описание                        | Статусы           |
|----------|-------------------|---------------------------------|-------------------|
| `POST`   | `/auth/register`  | Зарегистрировать пользователя (всегда роль `User` — см. [ролевую модель](#ролевая-модель)) | 204, 400 |
| `POST`   | `/auth/login`     | Войти и получить JWT-токен      | 200, 404          |
| `GET`    | `/events`         | Список событий с фильтрацией и пагинацией | 200 |
| `GET`    | `/events/{id}`    | Получить событие по ID          | 200, 404          |
| `POST`   | `/events`         | Создать новое событие (только Admin) | 201, 400, 401, 403 |
| `PUT`    | `/events/{id}`    | Обновить событие целиком (только Admin) | 200, 400, 401, 403, 404 |
| `DELETE` | `/events/{id}`    | Удалить событие (только Admin)  | 204, 401, 403, 404 |
| `POST`   | `/events/{id}/book` | Забронировать билет на мероприятие (Отложенная обработка) | 202, 400, 401, 404, 409 |
| `GET`    | `/bookings/{id}`    | Получить текущий статус и информацию о бронировании | 200, 401, 404 |
| `DELETE` | `/bookings/{id}`    | Отменить бронь: свою — любой пользователь, чужую — только Admin | 204, 401, 403, 404 |
 
 Параметры запроса (Query): `title` (строка), `from` (дата), `to` (дата), `page` (int), `pageSize` (int).

`/auth/register` и `/auth/login` доступны без токена. Остальные эндпоинты требуют заголовок `Authorization: Bearer <token>`.

### Пример запроса (POST /auth/register)

```json
{
  "login": "john",
  "password": "P@ssw0rd123"
}
```

`RegisterUserDto` не содержит поля `role` — эндпоинт всегда создаёт пользователя с ролью `User`, тело запроса не может повлиять на роль (лишние поля в JSON, включая `"role"`, игнорируются биндером). Это осознанное ограничение: без него любой клиент мог бы зарегистрироваться сразу как `Admin`. Успешная регистрация возвращает `204 No Content`. Как завести администратора — см. [ролевую модель](#ролевая-модель).

### Пример запроса (POST /auth/login)

```json
{
  "login": "admin",
  "password": "admin123"
}
```

### Пример ответа (200 OK)

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIzZmE4NWY2NC01NzE3LTQ1NjItYjNmYy0yYzk2M2Y2NmFmYTYi..."
}
```

Полученный токен нужно вставить в Swagger UI через кнопку **Authorize** в правом верхнем углу — достаточно вставить сам токен без слова `Bearer`, Swagger добавит его сам. После этого будут доступны защищённые эндпоинты, а роль из токена определит, какие операции разрешены (`[Authorize]` против `[Authorize(Roles = "Admin")]`).

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

## 🔐 Аутентификация и авторизация

### Ролевая модель

В системе две роли — `User` и `Admin`, хранятся в `UserRole` и в поле `role` таблицы `users`:

| Роль | Права |
|---|---|
| `User` | Бронирует события (`POST /events/{id}/book`), просматривает и отменяет **только свои** брони (`GET`/`DELETE /bookings/{id}`) |
| `Admin` | Всё то же, что и `User`, плюс управление событиями (`POST`/`PUT`/`DELETE /events`) и отмена **любых** броней, включая чужие |

`POST /auth/register` всегда создаёт пользователя с ролью `User` — `RegisterUserDto` не принимает роль от клиента, так что зарегистрироваться сразу как `Admin` невозможно. Роль попадает в JWT-токен как claim при логине. Проверка роли на контроллерах — декларативная, через `[Authorize(Roles = "Admin")]` для управления событиями; для отмены чужой брони роль проверяется в `BookingService.CancelBookingAsync` (владелец либо `Admin`, иначе `ForbiddenException` → 403).

**Создание администратора.** Единственный способ завести `Admin` — служебная команда, а не HTTP API:

```bash
dotnet run --project TicketFlow.Presentation -- create-admin <login> <password>
```

Команда переиспользует тот же `IUserRepository`/`IPasswordHasher`, что и обычная регистрация, применяет миграции при необходимости и завершает процесс, не поднимая веб-хост. Требует прямого доступа к окружению (сервер/CI) — эндпоинта для этого в API нет.

### Получение и использование JWT-токена в Swagger

1. Откройте Swagger UI (`https://localhost:7241/swagger`).
2. Выполните `POST /auth/register` — создайте пользователя (роль всегда `User`; для тестирования прав администратора заведите его через `dotnet run -- create-admin <login> <password>`, см. [ролевую модель](#ролевая-модель)).
3. Выполните `POST /auth/login` с теми же логином и паролем — в ответе придёт `token`.
4. Нажмите кнопку **Authorize** вверху страницы, вставьте значение `token` в поле (без слова `Bearer` — Swagger подставит его сам) и нажмите **Authorize**, затем **Close**.
5. Все последующие запросы из Swagger UI будут уходить с заголовком `Authorization: Bearer <token>`. Эндпоинты, недоступные текущей роли, вернут `403 Forbidden`; запрос без токена — `401 Unauthorized`.

### Хранение паролей и токена

Пароль никогда не хранится в открытом виде — `PasswordHasher` хеширует его BCrypt (`workFactor: 12`, соль встроена в хеш) и сохраняет результат в `users.password_hash`; верификация также принимает legacy-хеши SHA-256 (созданные до перехода на BCrypt) для обратной совместимости. Токен подписывается `HmacSha256` на секрете из `Jwt:Secret` (см. [настройку JWT](#настройка-jwt-аутентификации-и-подключения-к-postgresql)) и несёт claims `nameid` (Id пользователя, `ClaimTypes.NameIdentifier`), `unique_name` (логин, `ClaimTypes.Name`), `role` (`ClaimTypes.Role`) и `jti` (уникальный идентификатор токена).

При неверном логине или пароле `POST /auth/login` возвращает одинаковое сообщение независимо от причины — это защита от перебора существующих логинов.

---

## ⚠️ Обработка ошибок

Все ошибки в приложении обрабатываются централизованно и возвращаются в формате Problem Details — как доменные исключения через `GlobalExceptionHandlingMiddleware`, так и встроенные ответы аутентификации/авторизации ASP.NET Core (401/403), поскольку оба пути используют один `IProblemDetailsService` с общей настройкой заголовков.

Пример ответа при ошибке (404 Not Found):
```json
{
  "status": 404,
  "title": "Not found",
  "detail": "Event with ID ... not found."
}
```

Пример ответа при отсутствии прав (403 Forbidden) — попытка отменить чужую бронь без роли Admin:
```json
{
  "status": 403,
  "title": "Forbidden",
  "detail": "You can not cancel other user booking."
}
```

Пример ответа без токена (401 Unauthorized):
```json
{
  "status": 401,
  "title": "Unauthorized"
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

Сервисные тесты не зависят от базы данных: порты `IEventRepository` и `IBookingRepository` подменяются моками через Moq, состояние хранится в памяти теста. Проект также ссылается на `Infrastructure`, чтобы напрямую тестировать конкретные реализации без портов — `PasswordHasher` (BCrypt) и `JwtTokenGenerator`.

Общее окружение для сервисных тестов собирается в `TestEnvironment`: он настраивает моки портов, регистрирует их синглтонами и вызывает `AddApplicationServices(configuration)` (с пустой `IConfiguration` — секция `Booking` не задана, действует значение по умолчанию), поэтому тесты работают с теми же сервисами, что и приложение:

```csharp
using var env = TestHelpers.Create();
using var scope = env.CreateScope();

env.SeedEvent(TestHelpers.CreateTestEvent(totalSeats: 5));

var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
```

Основные наборы unit-тестов:

- `EventServiceTests` — проверка бизнес-логики управления событиями: создание, обновление, удаление, получение по ID, фильтрация, пагинация и валидация дат.
- `BookingServiceTests` — проверка сценариев бронирования: создание заявки, проверку отсутствующих событий, sold out-сценарии, защиту от овербукинга и новые доменные правила спринта 8 — запрет брони уже начавшегося события (`CreateBookingAsync_ShouldThrowEventAlreadyStartedException_WhenEventHasAlreadyStarted`), лимит активных броней (`CreateBookingAsync_ShouldThrowBookingLimitExceededException_WhenUserReachesActiveBookingsLimit`) и независимость лимитов разных пользователей (`CreateBookingAsync_ShouldSucceed_WhenAnotherUserHasReachedTheirOwnLimit`).
- `BookingProcessingBackgroundServiceTests` — проверка фоновой обработки заявок: перевод Pending в Confirmed или Rejected, заполнение ProcessedAt, обработка отмены через CancellationToken.
- `EventTests` и `BookingTests` — изолированные тесты доменных моделей.

### Интеграционные тесты

Проект `TicketFlow.IntegrationTests` проверяет слой доступа к данным на реальной PostgreSQL через `Testcontainers.PostgreSql`.
Интеграционные тесты:
1. Автоматически поднимают PostgreSQL-контейнер.
2. Сбрасывают тестовую базу перед тестами.
3. Применяют EF Core-миграции через `Database.MigrateAsync()`.
4. Проверяют создание таблиц `events`, `bookings`, `users` и `__EFMigrationsHistory`.
5. Проверяют внешний ключ `bookings.event_id → events.id`.
6. Покрывают методы `EventRepository`.
7. Покрывают методы `BookingRepository`, в том числе с реальным `bookings.user_id → users.id` (брони в тестах создаются только для существующего пользователя — колонка обязательна и защищена внешним ключом).
8. Проверяют работу фильтрации, пагинации, добавления, обновления, удаления и выборки данных на реальной PostgreSQL.
9. Покрывают сквозной сценарий бронирования: `BookingServiceTests` вызывает `IBookingService` поверх настоящих репозиториев и проверяет, что бронь сохранена, а место у события зарезервировано одним `SaveChangesAsync`.
10. Покрывают фоновую обработку: `BookingProcessingBackgroundServiceTests` запускает воркер против реальной базы и проверяет, что статус `Confirmed` действительно сохраняется.

Окружение для сервисных тестов собирает `PostgreSqlTestFixture.CreateServiceProvider()` — он вызывает `AddInfrastructureServices(connectionString, configuration)` и `AddApplicationServices()`, то есть повторяет composition root приложения. Поскольку у тестового проекта нет `appsettings.json`, `configuration` собирается в памяти (`ConfigurationBuilder().AddInMemoryCollection(...)`) с тестовыми значениями секции `Jwt`.

### Как писать новые тесты

**Выбор уровня.** Правило простое: если проверяется решение, принимаемое кодом, — это unit-тест; если проверяется, что решение доехало до базы, — интеграционный.

| Что проверяем | Куда писать |
|---|---|
| Бизнес-правило сущности (`TryReserveSeats`, `Confirm`, `Cancel`) | `TicketFlow.Tests/Models/` |
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

Брони в интеграционных тестах создаются только для реально сохранённого пользователя (`user_id` защищён внешним ключом) — вспомогательный метод `StoreUser`/`StoreUser(context)` есть в каждом тестовом классе, который создаёт `Booking`.

**Про время выполнения.** Юнит-тесты не обращаются ни к базе, ни к диску. Интеграционные поднимают Docker-контейнер и пересоздают схему на каждый тест, а тест фоновой обработки дополнительно ждёт цикл воркера — это самая медленная часть набора. Поэтому в интеграционный проект стоит выносить только то, что действительно требует настоящей базы.

---

## 🗃️ Репозиторный слой

Доступ к базе данных инкапсулирован в репозиториях, разнесённых по двум слоям:

- интерфейсы портов — `IEventRepository`, `IBookingRepository`, `IUserRepository` — объявлены в `TicketFlow.Application/Abstractions/`;
- реализации-адаптеры — `EventRepository`, `BookingRepository`, `UserRepository` — находятся в `TicketFlow.Infrastructure/Repositories/` и работают через `AppDbContext`.

Сервисы не обращаются к `AppDbContext` напрямую и не знают о конкретных реализациях — они получают интерфейсы через DI, а связывание происходит в composition root.

Репозитории отвечают только за доступ к данным:

- поиск сущностей по ID (и по логину — для `User`);
- добавление сущностей;
- удаление сущностей;
- выборку списка событий с фильтрацией и пагинацией;
- выборку pending-бронирований;
- подсчёт активных броней пользователя (`CountActiveBookingsByUserAsync` — для проверки лимита);
- сохранение изменений через `SaveChangesAsync()`.

Уникальность логина обеспечена индексом `IX_users_login` (`UserConfiguration`), а связь `bookings.user_id → users.id` — внешним ключом с `DeleteBehavior.Restrict` (удаление пользователя с активными бронями запрещено на уровне схемы).

Бизнес-логика остаётся в сервисах и доменных моделях.


## 📅 Документация подсистемы бронирования

### 🎟 Модель данных события (Event)
Сущность `Event` использует концепцию Rich Domain Model и самостоятельно управляет количеством билетов, предотвращая овербукинг на уровне бизнес-логики:
* `Id` (`Guid`) — уникальный идентификатор события.
* `Title`, `Description` — базовая информация о мероприятии.
* `StartAt`, `EndAt` (`DateTime`) — временные рамки проведения.
* `TotalSeats` (`int`) — общее (максимальное) количество мест на мероприятии. Задается при создании и должно быть больше нуля.
* `AvailableSeats` (`int`) — текущее количество свободных мест. Уменьшается при успешном создании заявки и восстанавливается, если фоновый сервис отклоняет бронь.

### 👤 Модель данных пользователя (User)
Сущность `User` хранит учётные данные и роль, создаётся через фабричный метод `Create`, а не публичный конструктор:
* `Id` (`Guid`) — уникальный идентификатор пользователя.
* `Login` (`string`) — логин, уникален в пределах системы (уникальный индекс в БД).
* `PasswordHash` (`string`) — хеш пароля (BCrypt), пароль в открытом виде нигде не хранится.
* `Role` (`UserRole`) — роль пользователя: `User` или `Admin`.

### 📦 Модель данных бронирования (Booking)
Сущность `Booking` описывает заявку на бронирование места на конкретное мероприятие и содержит поля:
* `Id` (`Guid`) — уникальный идентификатор брони.
* `EventId` (`Guid`) — идентификатор связанного события.
* `UserId` (`Guid`) — идентификатор пользователя, создавшего бронь.
* `Status` (`BookingStatus`) — текущее состояние заявки. Принимает значения:
  * `Pending` — бронь создана и ожидает обработки фоновым сервисом.
  * `Confirmed` — бронирование успешно подтверждено.
  * `Rejected` — бронирование отклонено.
  * `Cancelled` — бронь отменена пользователем или администратором.
* `CreatedAt` (`DateTime`) — дата и время инициализации бронирования.
* `ProcessedAt` (`DateTime?`) — дата и время обработки заявки внешней системой или отмены (заполняется фоновым сервисом либо методом `Cancel()`).

### 📏 Доменные правила бронирования

При создании брони (`BookingService.CreateBookingAsync`) сервис последовательно проверяет:

1. Событие существует (иначе `NotFoundException` → 404).
2. Событие ещё не началось: `event.StartAt` должен быть в будущем (иначе `EventAlreadyStartedException` → 400).
3. У пользователя не превышен лимит активных броней — по умолчанию **10** одновременных броней в статусе `Pending`/`Confirmed` (иначе `BookingLimitExceededException` → 409, с указанием значения лимита в сообщении). Лимит задаётся конфигурацией `Booking:MaxActiveBookingsPerUser` (`appsettings.json`, секция `Booking`; биндится в `BookingSettings` через `IOptions`), 10 — значение по умолчанию, если секция не задана.
4. У события есть свободные места (иначе `NoAvailableSeatsException` → 409).

При отмене брони (`BookingService.CancelBookingAsync`, `DELETE /bookings/{id}`):

1. Бронь должна существовать (иначе `NotFoundException` → 404).
2. Отменить бронь может либо её владелец, либо пользователь с ролью `Admin` — иначе `ForbiddenException` → 403.
3. Повторная отмена уже `Cancelled`/`Rejected` брони запрещена доменной моделью (`Booking.Cancel()` бросает `InvalidOperationDomainException` → 400).

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
2. проверку, что событие ещё не началось;
3. подсчёт активных броней пользователя и проверку лимита;
4. проверку доступных мест через `TryReserveSeats()`;
5. уменьшение `AvailableSeats`;
6. создание новой брони;
7. сохранение изменений через `SaveChangesAsync()`.

Так как `AppDbContext` отслеживает и изменённое событие, и новую бронь, один вызов `SaveChangesAsync()` сохраняет оба изменения.

`BookingProcessingBackgroundService` не хранит общий `DbContext` и не использует общий in-memory store. Для работы со scoped-зависимостями он использует `IServiceScopeFactory`: сначала создаёт scope для получения списка `Pending`-бронирований через `IBookingRepository`, затем отдельный scope для обработки каждой брони через `IBookingRepository` и `IEventRepository`.


### 🔄 Пример сквозного сценария использования

**Шаг 0: Аутентификация**
Клиент регистрируется и получает JWT-токен (см. [«Получение и использование JWT-токена в Swagger»](#получение-и-использование-jwt-токена-в-swagger)), затем передаёт его в заголовке `Authorization: Bearer <token>` во всех последующих запросах.

**Шаг 1: Создание бронирования**
Клиент отправляет запрос на бронирование места на существующее событие:
`POST /events/27fffa2f-fe74-42ea-8baa-4e7efa57e541/book`

**Сценарий А: Места есть (202 Accepted)**
Сервис мгновенно выполняет проверку — событие ещё не началось, лимит броней не превышен, есть свободные места — резервирует одно место и возвращает статус `202 Accepted`.
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

**Шаг 4: Отмена брони**
Владелец брони (или администратор) может отменить её в любой момент до наступления события:
`DELETE /bookings/14770068-9649-4b33-816c-9481019d2611` → `204 No Content`.

Если то же самое попробует другой пользователь без роли Admin — ответ `403 Forbidden`. Повторная отмена уже отменённой или отклонённой брони — `400 Bad Request`.

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
