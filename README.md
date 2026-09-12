# TicketFlow .NET

[![Build Status](https://img.shields.io/badge/.NET-10-blueviolet)](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Учебный проект — система для управления мероприятиями и бронированием билетов. С девятого спринта — три независимых микросервиса (ASP.NET Core Web API, .NET 10), общающихся асинхронно через Apache Kafka.

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
- **Спринт 9**: Декомпозиция монолита на три независимых микросервиса (Users, Events, Bookings), каждый со своей БД; асинхронный обмен через Apache Kafka (`BookingConfirmed`); идемпотентная обработка сообщений; JWT проверяется во всех трёх сервисах по общему секрету; вся система поднимается через `docker compose up` ✅
- **Спринт 10**: Кеширование в Redis для сервиса Events — cache-aside для карточки события и нового публичного эндпоинта топ-10 популярных событий (`GET /events/top`); абстракция кеша (`ICacheService`) в Application, реализация на `StackExchange.Redis` — в Infrastructure; TTL вынесены в конфигурацию; Delete-on-Write инвалидация `event:{id}` при изменении, удалении и при обработке Kafka-сообщения; кеш деградирует без ошибки для клиента, если Redis недоступен ✅
---

## 📜 История проекта

Спринты 1–8 строили единый монолит по чистой архитектуре: одна БД, четыре сборки (`TicketFlow.Domain/Application/Infrastructure/Presentation`), синхронная проверка мест и лимитов при бронировании, защита от овербукинга через `KeyedAsyncLock`. Это состояние полностью сохранено в ветке [`sprint-8`](https://github.com/itsanti/ya-ticketflow-net/tree/sprint-8) — если нужна архитектура на одной сборке и одной базе, смотрите её (или любую из веток `sprint-1`…`sprint-7` для более ранних этапов).

С девятого спринта монолит разобран на три сервиса — код старых сборок удалён из `main`/`sprint-9`, актуальная архитектура описана ниже. Часть решений сознательно не перенесена в новую архитектуру, а не забыта:

- **`KeyedAsyncLock`** и синхронная блокировка от овербукинга — удалены вместе с `TicketFlow.Application`. У Bookings больше нет доступа к количеству мест события (оно в другой БД), поэтому блокировать больше нечего — защиту от гонок теперь обеспечивает Kafka (см. [«Асинхронное взаимодействие через Kafka»](#-асинхронное-взаимодействие-через-kafka)).
- **Синхронные проверки в `BookingService`** (существование события, `EventAlreadyStartedException`, `NoAvailableSeatsException`) — удалены. Bookings больше не имеет данных о событиях и не может их проверить синхронно; согласованность стала eventual — через `BookingConfirmed`.
- Общая таблица `bookings.event_id → events.id` (внешний ключ) — упразднена вместе с общей БД. Связь между сервисами — только по `Guid`, без ссылочной целостности на уровне СУБД.

## 🏗 Структура решения

Три независимых сервиса и один общий проект-контракт. Каждый сервис — та же чистая архитектура, что и в монолите (Domain → Application → Infrastructure → Presentation), просто применённая трижды, с собственной БД у каждого:

```text
├── TicketFlow.Contracts/                    # Общий контракт события, не зависит ни от чего
│   ├── KafkaTopics.cs                        # Имя топика константой (booking-confirmed)
│   └── BookingConfirmedEvent.cs              # record: BookingId, EventId, UserId, SeatsCount, ConfirmedAtUtc
│
├── TicketFlow.Users.*/                       # Регистрация, вход, выдача JWT — БД users
│   ├── Domain/                                # User, UserRole, доменные исключения
│   ├── Application/                           # IUserService/UserService, порты (IUserRepository, IPasswordHasher, IJwtTokenGenerator)
│   ├── Infrastructure/                        # UsersDbContext, UserRepository, PasswordHasher (BCrypt), JwtTokenGenerator
│   └── Presentation/                          # AuthController, Program.cs, Swagger
│
├── TicketFlow.Events.*/                       # CRUD событий, учёт мест, кеш чтения — БД events + Redis
│   ├── Domain/                                # Event (TryReserveSeats/ReleaseSeats), ProcessedBookingConfirmation
│   ├── Application/                           # IEventService/EventService, IEventRepository, ICacheService, Caching/CacheKeys, Options/CacheOptions
│   ├── Infrastructure/                        # EventsDbContext, EventRepository, Messaging/ (Kafka-подписчик), Caching/ (RedisCacheService, см. ниже)
│   └── Presentation/                          # EventsController ([Authorize(Roles = "Admin")] на запись), Swagger
│
├── TicketFlow.Bookings.*/                     # Создание и отмена броней — БД bookings
│   ├── Domain/                                # Booking (Confirm/Reject/Cancel), UserRole
│   ├── Application/                           # IBookingService/BookingService, BookingProcessingBackgroundService
│   ├── Infrastructure/                        # BookingsDbContext, BookingRepository, Messaging/ (Kafka-издатель, см. ниже)
│   └── Presentation/                          # BookingsController ([Authorize]), Swagger
│
├── TicketFlow.Tests/                          # Юнит-тесты, разложены по сервисам (Users/ Events/ Bookings/)
├── TicketFlow.IntegrationTests/               # Интеграционные тесты, разложены по сервисам (своя БД-фикстура и WebApplicationFactory на сервис)
│
├── Dockerfile                                 # Один параметризованный multi-stage Dockerfile на все три сервиса (ARG SERVICE_PROJECT/SERVICE_DLL)
└── docker-compose.yml                         # Zookeeper + Kafka + 3×PostgreSQL + Redis + 3 сервиса — поднимаются одной командой
```

Направление зависимостей внутри каждого сервиса — то же, что было в монолите:

```text
Presentation ──> Application <── Infrastructure
      │               │               │
      └──────────> Domain <───────────┘
```

`TicketFlow.Contracts` — единственная связь между сервисами на уровне кода: его подключают `Bookings.Application` (объявляет порт `IBookingConfirmedPublisher`, работающий с `BookingConfirmedEvent`) и `Events.Infrastructure` (десериализует то же сообщение). Прямых `ProjectReference` между самими сервисами нет и быть не должно.

## 🧱 Слои приложения

Смысл слоёв не изменился с седьмого спринта — просто теперь эта структура повторяется в каждом сервисе независимо, с собственным набором сущностей и правил.

### Domain — что такое предметная область

Доменные сущности, перечисления и исключения слоя, без внешних зависимостей и без ссылок на другие сервисы. `Event` (в Events) сам следит за количеством мест (`TryReserveSeats`, `ReleaseSeats`); `Booking` (в Bookings) сам управляет своим статусом (`Confirm`, `Reject`, `Cancel`) и больше не хранит навигационное свойство на `Event` или `User` — только `EventId`/`UserId` как значения.

Нарушение бизнес-правила выражается доменным исключением, наследующим `DomainException`: `ValidationException`, `NotFoundException`, `ForbiddenException`, `BookingLimitExceededException`, `InvalidOperationDomainException` — набор различается по сервисам, поскольку и правила у них разные (см. [«Доменные правила»](#-доменные-правила-по-сервисам)).

### Application — что сервис умеет делать

Сценарии использования и **интерфейсы портов** в `Abstractions/` — что сервису нужно от внешнего мира, без знания, кто и как это реализует. У Bookings появился новый порт — `IBookingConfirmedPublisher`, реализация которого (Kafka) находится в Infrastructure; Application по-прежнему не знает, что события летят в Kafka, а не куда-то ещё.

В десятом спринте у Events по тому же принципу появился порт `ICacheService` (получить по ключу / записать с TTL / удалить): `EventService` знает только про «кеш с временем жизни», а не про Redis — ни одной ссылки на `StackExchange.Redis` в Application нет. Рядом лежат `Caching/CacheKeys` (все ключи кеша собраны в одном месте) и `Options/CacheOptions` (значения TTL из конфигурации), см. [«Кеширование»](#-кеширование-redis-сервис-events).

### Infrastructure — как это технически реализовано

Адаптеры к внешним технологиям: `DbContext` сервиса, Fluent API-конфигурации, миграции, реализации репозиториев — как и раньше. Новое здесь — `Messaging/`: у Bookings это `KafkaBookingConfirmedPublisher` (издатель), у Events — `BookingConfirmedConsumer` и `KafkaTopicInitializer` (подписчик и создание топика). Подробности — в разделе про Kafka.

У Events добавилась ещё одна папка-адаптер — `Caching/`: `RedisCacheService` (реализация порта `ICacheService` поверх `StackExchange.Redis`, с сериализацией в JSON и подавлением ошибок кеша) и `RedisOptions` (строка подключения из конфигурации). Соединение `IConnectionMultiplexer` регистрируется singleton'ом в `AddInfrastructureServices` — это тяжёлый потокобезопасный объект, который создаётся один раз на весь жизненный цикл приложения.

### Presentation — как этим пользоваться снаружи

HTTP-обвязка, JWT-аутентификация (`AddJwtBearer`) и Swagger — у каждого сервиса свои, но настроены идентично и по общим значениям `Jwt:Issuer`/`Jwt:Audience`/`Jwt:Secret`, поэтому токен, выданный Users, принимают и Events, и Bookings.

## 🔌 Сервисы, базы данных и порты

| Сервис | Ответственность | HTTP (dev) | HTTP (Docker) | Swagger | БД (Postgres) | Порт БД (host) |
|---|---|---|---|---|---|---|
| **Users** | Регистрация, вход, выдача JWT | `localhost:5101` / `7001` (https) | `localhost:5101` | `/swagger` | `users` | `5432` |
| **Events** | CRUD событий, учёт мест, подписчик Kafka, кеш чтения в Redis | `localhost:5102` / `7002` (https) | `localhost:5102` | `/swagger` | `events` | `5433` |
| **Bookings** | Создание/отмена брони, издатель Kafka | `localhost:5103` / `7003` (https) | `localhost:5103` | `/swagger` | `bookings` | `5434` |

Внутри Docker-сети все три Postgres слушают стандартный `5432` — наружу пробрасываются разные порты только для локального доступа с хоста. Kafka внутри сети — `kafka:29092`, снаружи (с хоста) — `localhost:9092`.

Redis (кеш сервиса Events) внутри сети — `redis:6379`, снаружи (с хоста) — `localhost:6379`. Тома у него нет и не нужно: это кеш, состояние которого можно потерять без последствий для данных — после перезапуска он просто прогреется заново из базы.

## 📡 Асинхронное взаимодействие через Kafka

Главное архитектурное правило спринта: **сервисы не вызывают друг друга по HTTP**. Bookings ничего не знает об устройстве Events и не проверяет у него ни существование события, ни количество мест — это стало eventual consistency через сообщение `BookingConfirmed`.

**Контракт** (`TicketFlow.Contracts`, подключают оба сервиса):

```csharp
public const string BookingConfirmed = "booking-confirmed"; // KafkaTopics

public sealed record BookingConfirmedEvent(
    Guid BookingId, Guid EventId, Guid UserId, int SeatsCount, DateTime ConfirmedAtUtc);
```

**Издатель — Bookings** (`KafkaBookingConfirmedPublisher`, `Bookings.Infrastructure/Messaging`). `BookingProcessingBackgroundService`, подтверждая заявку, сначала сохраняет `Booking.Confirm()` в свою БД и только затем публикует событие — если публикация не удалась, статус брони в БД уже корректен, ошибка публикации только логируется. `IProducer<string,string>` собирается один раз и живёт синглтоном (регистрируется в DI отдельно от паблишера — это же позволяет подменить его моком в тестах); ключ сообщения — `EventId.ToString()`, чтобы все брони по одному событию попадали в один partition и обрабатывались по порядку.

**Подписчик — Events** (`BookingConfirmedConsumer : BackgroundService`, `Events.Infrastructure/Messaging`). В цикле блокирующего `Consume()` десериализует сообщение и вызывает `Event.TryReserveSeats(SeatsCount)` — на каждое сообщение создаётся свой DI-scope (`IServiceScopeFactory`), поскольку сам консьюмер — синглтон, а `IEventRepository`/`DbContext` — scoped. Три случая обрабатываются пропуском с логированием, не роняя цикл: событие не найдено, свободных мест не осталось, сообщение — не валидный JSON.

**Идемпотентность.** Kafka доставляет сообщения минимум один раз — при повторной доставке (перезапуск консьюмера, ретрай) `BookingConfirmed` может прийти дважды. Чтобы не списать место повторно, Events хранит журнал обработанных броней — `ProcessedBookingConfirmation(BookingId)` (миграция `AddProcessedBookingConfirmations`). Перед уменьшением мест консьюмер проверяет, обработан ли уже этот `BookingId`; если да — сообщение пропускается. Отметка о обработке и уменьшение мест сохраняются одним `SaveChangesAsync()`, то есть атомарно.

**Создание топика.** `KafkaTopicInitializer` (`IHostedService`, не `BackgroundService` — одноразовая задача) создаёт топик `booking-confirmed` (3 partition, replication factor 1) при старте Events, если его ещё нет; ошибку создания топика логирует и не роняет запуск сервиса — топик почти всегда создаётся автоматически брокером (`KAFKA_AUTO_CREATE_TOPICS_ENABLE=true` в `docker-compose.yml`), инициализатор — гарантия на случай, если это отключат.

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
- [x] Синхронизация критических секций: защита от овербукинга с помощью `KeyedAsyncLock` (per-event async-лок на `SemaphoreSlim` внутри) при конкурентном создании и отмене брони
- [x] Параллельная обработка фоновых задач: `BookingProcessingBackgroundService` обрабатывает `Pending`-брони параллельно через `Task.WhenAll`, каждая — в своём scope и со своим `AppDbContext`, без общего изменяемого состояния
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
- [x] Генерация JWT-токена по данным пользователя (`IJwtTokenGenerator`/`JwtTokenGenerator`)
- [x] Регистрация (`POST /auth/register`) и вход (`POST /auth/login`) с выдачей JWT
- [x] JWT-аутентификация в Web API (`AddJwtBearer`) и авторизация по ролям (`[Authorize(Roles = "Admin")]`)
- [x] Идентификатор текущего пользователя читается из claims токена и передаётся в сценарии бронирования и отмены
- [x] Управление событиями (`POST`/`PUT`/`DELETE /events`) доступно только роли Admin
- [x] `DELETE /bookings/{id}` — отмена брони: владелец отменяет свою, администратор — любую
- [x] При неверных учётных данных на входе возвращается одно и то же сообщение (защита от перебора логинов)
- [x] Swagger настроен для работы с JWT (кнопка Authorize)
- [x] Единый формат Problem Details для доменных исключений и встроенных ответов 401/403 (`CustomizeProblemDetails`)
- [x] Юнит-тесты новых доменных правил: бронирование прошедшего события, лимит активных броней, независимость лимитов разных пользователей

 **(Спринт 9)**
- [x]  Монолит разделён на три независимых сервиса — Users, Events, Bookings — у каждого своя БД, своя миграция и свой жизненный цикл
- [x]  Общий контракт события и имя топика вынесены в `TicketFlow.Contracts`, не зависящий ни от одного сервиса
- [x]  Bookings публикует `BookingConfirmed` в Kafka при подтверждении брони; продюсер — singleton `IProducer<string,string>`, освобождается через `IDisposable`, ключ сообщения — `EventId` для порядка обработки по partition
- [x]  Events подписан на топик через `BackgroundService`, уменьшает `AvailableSeats`; ошибки конкретного сообщения (нет события, нет мест, битый JSON) не роняют консьюмер
- [x]  Идемпотентная обработка: повторная доставка одного и того же `BookingConfirmed` не уменьшает места дважды (`ProcessedBookingConfirmation`)
- [x]  Топик `booking-confirmed` создаётся автоматически при старте Events (`KafkaTopicInitializer`), не блокируя запуск сервиса при недоступном брокере
- [x]  JWT проверяется в Events и Bookings по общим `Issuer`/`Audience`/`Secret` с Users; ролевая модель (`Admin`/`User`) не изменилась
- [x]  Bookings больше не обращается к данным о событиях напрямую — синхронные проверки существования/начала события/мест удалены вместе с `KeyedAsyncLock`; согласованность стала eventual через Kafka
- [x]  Один параметризованный multi-stage `Dockerfile` собирает все три сервиса; `docker compose up` поднимает Zookeeper, Kafka, три PostgreSQL и три сервиса одной командой
- [x]  Юнит- и интеграционные тесты переразложены по сервисам; добавлены тесты на Kafka-издатель и Kafka-подписчик, включая идемпотентность

 **(Спринт 10)**
- [x]  Redis подключён к сервису Events: `IConnectionMultiplexer` регистрируется singleton'ом в DI, строка подключения — в конфигурации (`Redis:ConnectionString`)
- [x]  Абстракция кеша (`ICacheService`: получить / записать с TTL / удалить) объявлена в Application, реализация `RedisCacheService` на `StackExchange.Redis` — в Infrastructure
- [x]  Cache-aside для карточки события (`GET /events/{id}`, ключ `event:{id}`): при попадании в кеш обращения к базе нет, при промахе результат читается из базы и сохраняется в кеш с TTL
- [x]  Новый публичный эндпоинт `GET /events/top` — топ-10 событий по проценту проданных мест (`(TotalSeats - AvailableSeats) / TotalSeats`), с кешем по ключу `events:top10`
- [x]  TTL кеша разные для разных данных и вынесены в конфигурацию (`Cache:EventTtlSeconds` = 60, `Cache:TopEventsTtlSeconds` = 300)
- [x]  Все ключи кеша собраны в одном месте — `Caching/CacheKeys`, а не разбросаны по коду
- [x]  Стратегия Delete-on-Write: `event:{id}` инвалидируется после `SaveChangesAsync` при обновлении и удалении события, а также в `BookingConfirmedConsumer` после уменьшения мест — порядок «сначала база, потом кеш» соблюдён везде
- [x]  Кеш топ-10 живёт только по TTL — явной инвалидации при каждом бронировании нет (обоснование — в разделе [«Кеширование»](#-кеширование-redis-сервис-events))
- [x]  Устойчивость к недоступности Redis: ошибки кеша логируются и не пробрасываются клиенту, запрос уходит напрямую в базу; `AbortOnConnectFail = false` позволяет сервису стартовать и работать без поднятого Redis
- [x]  Юнит-тесты кеширования: попадание в кеш (репозиторий не вызывается), промах (чтение из репозитория + запись в кеш), инвалидация при мутирующих операциях и в Kafka-консьюмере
- [x]  Попутно закрыт инвариант вместимости, который обнажился из-за деления на `TotalSeats` в запросе топ-10: изменение вместимости идёт через `Event.ChangeCapacity` с пересчётом свободных мест и запретом опускаться ниже числа проданных
---

## 🛠 Технологический стек

- **Runtime**: .NET 10 (C# 13)
- **Framework**: ASP.NET Core Web API
- **API Documentation**: Swashbuckle (Swagger UI) — в каждом сервисе, с поддержкой JWT (кнопка Authorize)
- **Database**: PostgreSQL — своя база на сервис
- **ORM**: Entity Framework Core
- **EF Provider**: Npgsql.EntityFrameworkCore.PostgreSQL
- **Cache**: Redis (StackExchange.Redis) — кеш чтения в сервисе Events
- **Messaging**: Apache Kafka (Confluent.Kafka), Zookeeper — для координации брокера
- **Authentication**: JWT Bearer (Microsoft.AspNetCore.Authentication.JwtBearer) — выдаёт только Users, проверяют все три
- **Token generation**: System.IdentityModel.Tokens.Jwt
- **Password hashing**: BCrypt (BCrypt.Net-Next), с верификацией legacy-хешей SHA-256 (System.Security.Cryptography)
- **Mocking**: Moq (подмена портов и `IProducer`/`IEventRepository` в юнит-тестах)
- **Integration Tests Database**: PostgreSQL через Testcontainers (своя БД-фикстура на сервис)
- **Containers**: Docker / Docker Compose — Dockerfile, Testcontainers.PostgreSql

---

## 🗃️ Репозиторный слой

Доступ к базе данных инкапсулирован в репозиториях, разнесённых по двум слоям в каждом сервисе:

- интерфейсы портов — `IUserRepository` (Users), `IEventRepository` (Events), `IBookingRepository` (Bookings) — объявлены в `<Сервис>.Application/Abstractions/`;
- реализации-адаптеры — `UserRepository`, `EventRepository`, `BookingRepository` — находятся в `<Сервис>.Infrastructure/Repositories/` и работают через собственный `DbContext` (`UsersDbContext`/`EventsDbContext`/`BookingsDbContext`).

Сервисы не обращаются к `DbContext` напрямую и не знают о конкретных реализациях — связывание происходит в composition root (`AddInfrastructureServices`). Репозитории отвечают только за доступ к данным: поиск по ID (и по логину — для `User`), добавление, удаление, выборку с фильтрацией и пагинацией (Events), выборку топ-10 по проценту проданных мест (`GetTopPopularAsync`, Events — сортировка считается в SQL, не в памяти), выборку pending-бронирований и подсчёт активных броней пользователя (Bookings), а также идемпотентный журнал `ProcessedBookingConfirmation` (Events, см. [Kafka](#-асинхронное-взаимодействие-через-kafka)). Уникальность логина в Users обеспечена индексом `IX_users_login`. Внешнего ключа `bookings.event_id → events.id` больше нет — базы разные.

Кеш живёт слоем выше репозиториев — в `EventService` (cache-aside), поэтому репозиторий остаётся «немым» источником данных и ничего не знает про Redis, см. [«Кеширование»](#-кеширование-redis-сервис-events).

Бизнес-логика остаётся в сервисах и доменных моделях.

## ⚙️ Запуск проекта

### Предварительные требования

- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- Docker Desktop / Docker Engine с Docker Compose — для запуска системы целиком и для интеграционных тестов (Testcontainers)

### Используемые NuGet-пакеты

Версии управляются централизованно через `Directory.Packages.props`; каждый проект объявляет только то, что использует.

`TicketFlow.Contracts` и `*.Domain` (все три сервиса) — ни одного пакета.

`*.Application` (все три сервиса) — `Microsoft.Extensions.DependencyInjection.Abstractions`; у Bookings дополнительно `Microsoft.Extensions.Hosting.Abstractions` и `Microsoft.Extensions.Options*` (там же живёт `BookingProcessingBackgroundService`), у Events — `Microsoft.Extensions.Options*` (биндинг `CacheOptions` и `IOptions` в `EventService`).

`Users.Infrastructure`:
```bash
- Microsoft.EntityFrameworkCore / .Relational
- Npgsql.EntityFrameworkCore.PostgreSQL
- System.IdentityModel.Tokens.Jwt
- Microsoft.Extensions.Options.ConfigurationExtensions
- BCrypt.Net-Next
```

`Events.Infrastructure` / `Bookings.Infrastructure` — то же самое плюс `Confluent.Kafka` (издатель/подписчик); у Events дополнительно `Microsoft.Extensions.Hosting.Abstractions` (`BackgroundService`/`IHostedService` для консьюмера и создателя топика) и `StackExchange.Redis` (клиент кеша, `RedisCacheService`).

`*.Presentation` (все три сервиса):
```bash
- Swashbuckle.AspNetCore
- Microsoft.AspNetCore.OpenApi
- Microsoft.EntityFrameworkCore.Design   # нужен dotnet ef в startup-проекте
- Microsoft.AspNetCore.Authentication.JwtBearer   # только Events и Bookings — Users токен выдаёт, а не проверяет
```

`TicketFlow.Tests` (юнит, ссылается на Domain/Application всех сервисов, плюс `Events.Infrastructure`/`Bookings.Infrastructure` — для прямого тестирования консьюмера и издателя Kafka):
```bash
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Configuration
- Confluent.Kafka
- Moq
- Microsoft.NET.Test.Sdk / xunit / xunit.runner.visualstudio
```

`TicketFlow.IntegrationTests` (ссылается дополнительно на Presentation каждого сервиса — для `WebApplicationFactory`):
```bash
- Microsoft.AspNetCore.Mvc.Testing
- Microsoft.EntityFrameworkCore / .Relational
- Npgsql.EntityFrameworkCore.PostgreSQL
- Testcontainers.PostgreSql
- Microsoft.Extensions.Configuration
- Moq
- Microsoft.NET.Test.Sdk / xunit / xunit.runner.visualstudio
```

### Вариант 1 — вся система в Docker (рекомендуется)

Поднимает Zookeeper, Kafka, три PostgreSQL, Redis и все три сервиса одной командой.

**Предварительные требования:** Docker Desktop / Docker Engine с Docker Compose.

```bash
git clone git@github.com:itsanti/ya-ticketflow-net.git
cd ya-ticketflow-net
docker compose up --build
```

После старта доступны:

- Users — `http://localhost:5101/swagger`
- Events — `http://localhost:5102/swagger`
- Bookings — `http://localhost:5103/swagger`

Миграции каждый сервис применяет сам при старте (`app.Services.ApplyMigrations()`), базы создавать вручную не нужно.

### Вариант 2 — сервис локально, инфраструктура в Docker

Для разработки одного сервиса без пересборки контейнеров: поднимите инфраструктуру частично (например, только `events-db`, `redis` и `kafka`/`zookeeper` из `docker-compose.yml`) и запустите сервис через `dotnet run`:

```bash
docker compose up -d zookeeper kafka events-db redis
dotnet run --project TicketFlow.Events.Presentation
```

`redis` в этом списке не обязателен: без него Events поднимется и будет работать, просто каждый запрос пойдёт в базу — ошибки кеша логируются и не доходят до клиента (см. [«Кеширование»](#-кеширование-redis-сервис-events)).

Локальные `appsettings.Development.json` каждого сервиса уже указывают на `localhost` с портами из таблицы [«Сервисы, базы данных и порты»](#-сервисы-базы-данных-и-порты).

### Настройка JWT

Секрет, издатель и аудитория должны совпадать во всех трёх сервисах — иначе токен, выданный Users, не пройдёт проверку в Events/Bookings. Несекретные значения (`Issuer`, `Audience`, `ExpirationMinutes`) лежат в `appsettings.json` каждого сервиса и одинаковы:

```json
{
  "Jwt": {
    "Issuer": "TicketFlow",
    "Audience": "TicketFlowClient",
    "ExpirationMinutes": 60
  }
}
```

`Jwt:Secret` и `ConnectionStrings:DefaultConnection` — секреты, в `appsettings.json` их нет.

- `Secret` — ключ подписи HMAC-SHA256, не короче 256 бит (32 байта / 64 hex-символа), иначе подпись слабая. Генерируется:
```bash
openssl rand -hex 32
```

**Локальная разработка.** Один и тот же dev-секрет лежит в `appsettings.Development.json` каждого сервиса (загружается при `ASPNETCORE_ENVIRONMENT=Development`, как в `launchSettings.json`) — это dev-only значение, актуальное только для локального docker-postgres/compose, поэтому хранить его в репозитории допустимо. При желании вынести из файла — проекты помечены `UserSecretsId`:

```bash
dotnet user-secrets set "Jwt:Secret" "<то же значение для всех трёх сервисов>" --project TicketFlow.Users.Presentation
dotnet user-secrets set "Jwt:Secret" "<то же значение>" --project TicketFlow.Events.Presentation
dotnet user-secrets set "Jwt:Secret" "<то же значение>" --project TicketFlow.Bookings.Presentation
```

**Прод и другие окружения.** Секреты задаются переменными окружения — ASP.NET Core превращает `__` в `:` при биндинге, одинаковые значения выставляются во всех трёх контейнерах в `docker-compose.yml`:

```bash
export Jwt__Secret="$(openssl rand -hex 32)"
export ConnectionStrings__DefaultConnection="Host=...;Port=5432;Database=...;Username=...;Password=..."
```

Если ни один источник не задаёт `Jwt:Secret` в окружении, отличном от Development, сервис упадёт при старте (`InvalidOperationException` в `AddAuthenticationServices`) — это осознанный fail-fast, а не баг.

### Настройка Redis и TTL кеша

Параметры кеша лежат в `appsettings.json` сервиса Events — секретов здесь нет, поэтому значения по умолчанию хранятся в репозитории:

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "Cache": {
    "EventTtlSeconds": 60,
    "TopEventsTtlSeconds": 300
  }
}
```

`Redis:ConnectionString` биндится в `RedisOptions` (Infrastructure) и передаётся в `IConnectionMultiplexer`; `Cache:*` — в `CacheOptions` (Application), откуда `EventService` берёт TTL для каждого ключа. Значения по умолчанию заданы и в самом классе `CacheOptions`, так что отсутствие секции не ломает старт сервиса.

В Docker строка подключения переопределяется переменной окружения на имя контейнера (`docker-compose.yml`, сервис `events-service`) — тот же приём, что и с базой и Kafka:

```bash
Redis__ConnectionString: "redis:6379"
```

Соединение создаётся с `AbortOnConnectFail = false`, поэтому недоступный на старте Redis не роняет сервис: клиент продолжает переподключаться в фоне, а запросы всё это время обслуживаются напрямую из базы.

### Создание администратора

Как и раньше, HTTP-эндпоинта для этого нет — только служебная команда, но теперь она у сервиса Users:

```bash
dotnet run --project TicketFlow.Users.Presentation -- create-admin <login> <password>
```

В Docker — тем же способом, но внутри уже запущенного контейнера:

```bash
docker compose exec users-service dotnet TicketFlow.Users.Presentation.dll create-admin <login> <password>
```

### Миграции

Схема БД каждого сервиса управляется через EF Core Migrations. Применение инкапсулировано в Infrastructure — в `Program.cs` каждого сервиса остаётся один вызов `app.Services.ApplyMigrations()`, который создаёт scope, получает `<Сервис>DbContext` и вызывает `Database.Migrate()`; веб-проект не ссылается на EF Core напрямую.

DbContext и миграции каждого сервиса лежат в его `Infrastructure`, а точка входа — в `Presentation`, поэтому `dotnet ef` требует двух параметров: `--project` — сборка с контекстом, `--startup-project` — откуда читается конфигурация и строка подключения. Для Events:

```bash
dotnet ef migrations add MigrationName \
  --project ./TicketFlow.Events.Infrastructure/TicketFlow.Events.Infrastructure.csproj \
  --startup-project ./TicketFlow.Events.Presentation/TicketFlow.Events.Presentation.csproj \
  --output-dir Persistence/Migrations
```

Применить миграции вручную (обычно не требуется — сервис делает это сам при старте):

```bash
dotnet ef database update \
  --project ./TicketFlow.Events.Infrastructure/TicketFlow.Events.Infrastructure.csproj \
  --startup-project ./TicketFlow.Events.Presentation/TicketFlow.Events.Presentation.csproj
```

Для Users и Bookings — те же две команды с заменой `Events` на `Users`/`Bookings` везде, включая имя БД в строке подключения.

### 📡 API Endpoints

**Users** (`/auth`):

| Метод | Путь | Описание | Статусы |
|---|---|---|---|
| `POST` | `/auth/register` | Зарегистрировать пользователя (всегда роль `User`) | 204, 400 |
| `POST` | `/auth/login` | Войти и получить JWT-токен | 200, 401 |

**Events** (`/events`, запись — только Admin):

| Метод | Путь | Описание | Статусы |
|---|---|---|---|
| `GET` | `/events` | Список событий с фильтрацией и пагинацией | 200 |
| `GET` | `/events/top` | Топ-10 событий по проценту проданных мест (кеш `events:top10`) | 200 |
| `GET` | `/events/{id}` | Получить событие по ID (кеш `event:{id}`) | 200, 404 |
| `POST` | `/events` | Создать новое событие (только Admin) | 201, 400, 401, 403 |
| `PUT` | `/events/{id}` | Обновить событие целиком (только Admin) | 200, 400, 401, 403, 404 |
| `DELETE` | `/events/{id}` | Удалить событие (только Admin) | 204, 401, 403, 404 |

Параметры `GET /events`: `title` (строка), `from`/`to` (дата), `page`/`pageSize` (int).

`GET /events/top` параметров не принимает — всегда 10 событий, отсортированных по убыванию доли проданных мест `(totalSeats - availableSeats) / totalSeats`; при равной доле порядок стабилизируется по `id`. Эндпоинт рассчитан на виджет главной страницы, который открывают анонимные посетители, поэтому токен не требуется, а результат кешируется на 5 минут (подробности — в разделе [«Кеширование»](#-кеширование-redis-сервис-events)). Маршрут `top` — литеральный сегмент, поэтому он не конфликтует с `GET /events/{id}`: ASP.NET Core отдаёт литералам приоритет над параметрами маршрута.

**Bookings** (`/bookings`, `/events/{id}/book`):

| Метод | Путь | Описание | Статусы |
|---|---|---|---|
| `POST` | `/events/{id}/book` | Забронировать место (отложенная обработка) | 202, 401, 409 |
| `GET` | `/bookings/{id}` | Статус брони: свою — любой пользователь, чужую — только Admin | 200, 401, 403, 404 |
| `DELETE` | `/bookings/{id}` | Отменить бронь: свою — любой пользователь, чужую — только Admin | 204, 401, 403, 404 |

`POST /events/{id}/book` больше не проверяет существование события и не возвращает `404`/`409 sold-out` — Bookings не знает о данных Events. `409` теперь означает только превышение лимита активных броней (`BookingLimitExceededException`). Уменьшение мест происходит асинхронно в Events (см. [Kafka](#-асинхронное-взаимодействие-через-kafka)) и не отражается на ответе `POST`.

`/auth/*` доступны без токена — как и `GET`-эндпоинты Events (`/events`, `/events/top`, `/events/{id}`): каталог событий и виджет топ-10 читают анонимные посетители, `[Authorize]` в Events стоит только на записи. Остальные эндпоинты требуют `Authorization: Bearer <token>`, выданный сервисом Users.

#### Пример запроса (POST /auth/register)

```json
{
  "login": "john",
  "password": "P@ssw0rd123"
}
```

`RegisterUserDto` не содержит поля `role` — эндпоинт всегда создаёт пользователя с ролью `User`, лишние поля в JSON (включая `"role"`) игнорируются биндером. Успешная регистрация возвращает `204 No Content`. Пароль (`RegisterUserDto`/`LoginUserDto`) валидируется `[StringLength(64, MinimumLength = 8)]` — от 8 до 64 символов, иначе `400` ещё на уровне модели.

#### Пример запроса (POST /auth/login)

```json
{
  "login": "admin",
  "password": "admin123"
}
```

#### Пример ответа (200 OK)

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIzZmE4NWY2NC01NzE3LTQ1NjItYjNmYy0yYzk2M2Y2NmFmYTYi..."
}
```

#### Пример запроса (POST /events, только Admin)

```json
{
  "title": "Tech Conference 2026",
  "description": "Ежегодная конференция по современным технологиям",
  "startAt": "2026-04-15T10:00:00",
  "endAt": "2026-04-17T18:00:00",
  "totalSeats": 100
}
```

#### Пример ответа (201 Created)

```json
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

#### Пример ответа (GET /events/{id}, 200 OK)

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

#### Пример ответа (GET /events/top, 200 OK)

Плоский массив без обёртки-пагинации — размер ответа всегда не больше 10 элементов:

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "Tech Conference 2026",
    "description": "Ежегодная конференция по современным технологиям",
    "startAt": "2026-04-15T10:00:00",
    "endAt": "2026-04-17T18:00:00",
    "totalSeats": 100,
    "availableSeats": 3
  },
  {
    "id": "8b1f0c52-4e2a-4a51-9f0e-2d7a5c1b9e44",
    "title": "Выставка роботов",
    "description": "Короткое интерактивное шоу",
    "startAt": "2026-08-10T12:00:00",
    "endAt": "2026-08-10T12:30:00",
    "totalSeats": 50,
    "availableSeats": 20
  }
]
```

Первое событие продано на 97 %, второе — на 60 %, поэтому порядок именно такой.

#### Пример запроса с фильтрацией и пагинацией (GET /events?title=Tech&page=1&pageSize=10)

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

Токен по-прежнему выдаёт только Users; Events и Bookings его проверяют, не выдавая собственных. Ролевая модель не изменилась:

| Роль | Права |
|---|---|
| `User` | Бронирует события, просматривает и отменяет **только свои** брони |
| `Admin` | Всё то же, что и `User`, плюс управление событиями и отмена **любых** броней |

`POST /auth/register` всегда создаёт роль `User` — `RegisterUserDto` не принимает роль от клиента. Роль попадает в JWT как claim при логине и проверяется декларативно (`[Authorize(Roles = "Admin")]` в Events, `[Authorize]` в Bookings — с ручной проверкой владельца в `BookingService`).

### Получение и использование JWT-токена в Swagger

У каждого сервиса свой Swagger, но токен всегда только один — от Users:

1. Откройте Swagger Users (`http://localhost:5101/swagger` в Docker или `https://localhost:7001/swagger` локально).
2. Выполните `POST /auth/register` — создайте пользователя (роль всегда `User`; для проверки прав администратора заведите его через `dotnet run -- create-admin <login> <password>`, см. [«Создание администратора»](#создание-администратора)).
3. Выполните `POST /auth/login` с теми же логином и паролем — в ответе придёт `token`.
4. Откройте Swagger нужного сервиса — Events (`:5102`) или Bookings (`:5103`) — нажмите кнопку **Authorize** вверху страницы, вставьте `token` (без слова `Bearer` — Swagger подставит его сам) и нажмите **Authorize**, затем **Close**.
5. Все последующие запросы из этого Swagger UI будут уходить с заголовком `Authorization: Bearer <token>`. Эндпоинты, недоступные текущей роли, вернут `403`; запрос без токена — `401`. Токен, полученный один раз в Users, действителен в обоих сервисах одновременно, потому что оба проверяют его по общему секрету.

Пароль хешируется BCrypt (`workFactor: 12`) в Users, с поддержкой верификации legacy-хешей SHA-256. Токен подписывается `HmacSha256` и несёт claims `nameid`, `unique_name`, `role`, `jti`. При неверном логине/пароле `POST /auth/login` возвращает одинаковое сообщение и всё равно выполняет `IPasswordHasher.Verify` против фиктивного хеша при отсутствующем логине — защита от перебора и от timing-атаки.

---

## ⚠️ Обработка ошибок

Каждый сервис обрабатывает ошибки централизованно и возвращает Problem Details — как доменные исключения через `GlobalExceptionHandlingMiddleware`, так и встроенные ответы аутентификации/авторизации (401/403).

Пример ответа при ошибке (404 Not Found):
```json
{
  "status": 404,
  "title": "Not found",
  "detail": "Event with ID ... not found."
}
```

Пример ответа при отсутствии прав (403 Forbidden):
```json
{
  "status": 403,
  "title": "Forbidden",
  "detail": "You can not cancel other user booking."
}
```

---

## 🧪 Тестирование

Как и раньше, два уровня — `TicketFlow.Tests` (юнит) и `TicketFlow.IntegrationTests` (интеграционные, требуют Docker). Оба теперь ссылаются на все три сервиса и разложены по одноимённым папкам (`Users/`, `Events/`, `Bookings/`) — единого веб-проекта, на который можно было бы сослаться одним `WebApplicationFactory`, больше нет.

```bash
dotnet test
dotnet test ./TicketFlow.IntegrationTests/TicketFlow.IntegrationTests.csproj
```

### Unit-тесты

Проект разложен по папкам `Users/`, `Events/`, `Bookings/` — по одной на сервис. Схема окружения та же, что и в монолите: `TestEnvironment` каждого сервиса поднимает `AddApplicationServices()` с мок-портами (Moq) вместо реальной БД, состояние хранится в памяти теста:

```csharp
using var env = TestHelpers.Create();
using var scope = env.CreateScope();

env.SeedEvent(TestHelpers.CreateTestEvent(totalSeats: 5));

var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
```

У Bookings в окружение добавлен мок `IBookingConfirmedPublisher` — иначе `BookingProcessingBackgroundService` не получит его через DI. У Events — идемпотентный in-memory журнал обработанных `BookingId`, повторяющий поведение `ProcessedBookingConfirmation`, и мок `ICacheService` (`env.CacheService`): без явной настройки он ведёт себя как пустой кеш — `GetAsync` возвращает `null`, то есть промах, и сценарий идёт в мок-репозиторий. Поэтому тесты, которые про кеш ничего не знают, продолжают проверять ровно ту же бизнес-логику, что и до его появления, а тесты про кеш задают поведение явно через `Setup`/`Verify`.

Основные наборы:

- `Users/UserServiceTests` — `RegisterAsync`: дубликат логина → `ValidationException`, роль всегда `User`; `LoginAsync`: несуществующий логин/неверный пароль → `UnauthorizedException` с одинаковым сообщением, валидные данные → токен; несуществующий логин всё равно вызывает `IPasswordHasher.Verify` с фиктивным хешем (защита от timing-атаки).
- `Users/PasswordHasherTests` — формат хеша (BCrypt), разная соль на одинаковый пароль, верификация нового формата и legacy SHA-256.
- `Users/JwtTokenGeneratorTests` — состав claims (`nameid`/`unique_name`/`role`/`jti`), issuer/audience, уникальность `jti`, успешная/неуспешная (чужой секрет) валидация.
- `Events/EventServiceTests` — создание, обновление, удаление, получение по ID, фильтрация, пагинация, валидация дат.
- `Events/EventTests` — изолированные тесты доменной модели `Event` (`TryReserveSeats`/`ReleaseSeats`, `ChangeCapacity`: пересчёт свободных мест при увеличении и уменьшении вместимости, граница «ровно проданные места», отказ при вместимости ниже проданных и при неположительном значении).
- `Events/RedisCacheServiceTests` — сама реализация кеша с мок-`IConnectionMultiplexer`/`IDatabase` (класс тестируется напрямую, интерфейса-обёртки над ним нет): чтение существующего и отсутствующего ключа, а также graceful degradation — падение команды Redis, битый JSON в значении и недоступное соединение возвращают промах / не бросают исключение наружу, но пишут `LogWarning`.
- `Events/EventCachingTests` — кеш-логика `EventService` с моками кеша и репозитория: попадание в кеш для `GetEventAsync`/`GetTopEventsAsync` (репозиторий не вызывается вообще — `Times.Never`), промах (данные читаются из репозитория и записываются в кеш нужным ключом), инвалидация ключа `event:{id}` после `UpdateEventAsync` и `RemoveEventAsync`.
- `Events/BookingConfirmedConsumerTests` — обработка сообщения напрямую (`HandleMessageAsync` сделан `internal` + `InternalsVisibleTo`, без поднятия настоящего Kafka-консьюмера): резерв места, событие не найдено, мест не осталось, битый JSON, дубликат по идемпотентности (повторный вызов не должен уменьшать места дважды), необработанное исключение репозитория не должно ронять обработчик, инвалидация кеша события после успешного резерва — и её отсутствие, когда сообщение пропущено (мест не осталось).
- `Bookings/BookingServiceTests` — создание брони с проверкой лимита активных броней и его независимости между пользователями; отмену — успешную (владелец), `ForbiddenException` для чужой брони, `InvalidOperationDomainException` при повторной отмене; `ForbiddenException` при просмотре чужой брони не-владельцем.
- `Bookings/BookingTests` — изолированные тесты домена `Booking` (`Confirm`/`Reject`/`Cancel`).
- `Bookings/BookingProcessingBackgroundServiceTests` — перевод `Pending` в `Confirmed`, заполнение `ProcessedAt`, обработка отмены через `CancellationToken`.
- `Bookings/KafkaBookingConfirmedPublisherTests` — `IProducer<string,string>` инжектируется через конструктор (не создаётся внутри), что позволяет подменить его моком: топик `booking-confirmed`, ключ сообщения (`EventId`), JSON корректно сериализуется и восстанавливается, `Dispose` паблишера вызывает только `Flush` продюсера — сам `Dispose()` продюсера паблишер не трогает, потому что `IProducer` зарегистрирован в DI отдельным singleton'ом и контейнер освобождает его сам; вызывать оба `Dispose` было бы избыточным двойным освобождением.

Из старого набора удалены без замены (тестировали поведение, которого больше нет, а не просто перенесённое в другой сервис): `KeyedAsyncLockTests` (класс удалён вместе с блокировкой), часть `BookingServiceTests` про существование события/наличие мест/овербукинг/уже начавшееся событие — Bookings это больше не проверяет.

### Интеграционные тесты

Структура та же идея, что и в юнитах — своя папка, своя `PostgreSqlTestFixture` и свой `CustomWebApplicationFactory` на каждый сервис (свой `DbContext`, свой `Program`, своя xUnit-коллекция `"<Сервис> PostgreSql collection"`, отключающая параллельный запуск внутри сервиса). Каждый тест начинается с `ResetDatabaseAsync()` — база пересоздаётся и миграции применяются заново.

- `*/<Сервис>RepositoryTests` — фильтрация, пагинация, добавление/обновление/удаление/выборка на реальной PostgreSQL; `UserRepositoryTests` дополнительно проверяет нарушение уникального индекса логина (`DbUpdateException`, `PostgresException.SqlState == "23505"`).
- `Bookings/BookingProcessingBackgroundServiceTests` — воркер против реальной БД, статус `Confirmed` действительно сохраняется (паблишер — стаб-мок, чтобы не требовать Kafka).
- `*/AuthHttpTests` — HTTP-уровень через `CustomWebApplicationFactory`: запрос без токена → 401, `POST /events` не от Admin → 403, неверный пароль на `/auth/login` → 401, отмена чужой брони → 403, попытка передать `"role": "Admin"` в `/auth/register` игнорируется (роль в выданном JWT всё равно `User`).

HTTP-тесты Events и Bookings не поднимают реальный Users — токен минтится локально тем же `JwtTokenGenerator`, что использует Users в проде (`TestTokenFactory`, общий для обоих проектов), с тем же dev-секретом: сетевой зависимости между тестами сервисов нет, а проверка подписи токена всё равно настоящая.

Из старого набора удалены: `MigrationTests` (проверял FK и бэкофилл, которых в разделённой схеме больше нет), интеграционный `Services/BookingServiceTests` (проверял синхронное уменьшение мест в общей БД — такого пути больше не существует). `BookingRepositoryTests` лишился теста на `DbUpdateException` при несуществующем событии — вставка с любым `EventId` теперь ожидаемо проходит, внешнего ключа нет.

### Как писать новые тесты

**Выбор уровня.** Если проверяется решение, принимаемое кодом, — unit-тест; если проверяется, что решение доехало до базы (или до HTTP-пайплайна) — интеграционный.

| Что проверяем | Куда писать |
|---|---|
| Бизнес-правило сущности (`TryReserveSeats`, `Confirm`, `Cancel`) | `TicketFlow.Tests/<Сервис>/` |
| Логика use case: валидация, доменные исключения, маппинг в DTO | `TicketFlow.Tests/<Сервис>/` |
| Обработка сообщения Kafka (`HandleMessageAsync`), формирование сообщения издателем | `TicketFlow.Tests/<Сервис>/`, без реального брокера |
| Взаимодействие с портом (сколько раз вызван `SaveChangesAsync`) | `TicketFlow.Tests`, через `Verify` |
| Работа с кешем: попадание, промах, инвалидация ключа | `TicketFlow.Tests/Events/`, мок `ICacheService` + `Verify` |
| Трансляция LINQ в SQL: фильтры, сортировка, пагинация | `TicketFlow.IntegrationTests/<Сервис>/` |
| Сохранение изменений, миграции, ограничения БД | `TicketFlow.IntegrationTests/<Сервис>/` |
| HTTP-уровень: аутентификация, авторизация по ролям | `TicketFlow.IntegrationTests/<Сервис>/` |

**Именование.** `Method_ShouldExpectedResult_WhenCondition`, например `CreateBookingAsync_ShouldThrowBookingLimitExceededException_WhenLimitReached`. Часть `_When...` можно опустить, если условие очевидно из названия.

Чего не стоит делать в юнит-тестах: тянуть `Infrastructure` в тест сервиса через мок-порты (тест перестанет быть юнитом) — ссылка на `Infrastructure` в `TicketFlow.Tests` существует только там, где тестируемый класс сам и есть реализация без интерфейса (`PasswordHasherTests`, `JwtTokenGeneratorTests`, Kafka-издатель/подписчик); проверять поведение хранилища через мок-репозиторий — его фильтрация лишь приблизительно повторяет SQL, новые правила выборки проверяются интеграционным тестом.

В интеграционных — работать с датами только в UTC (колонки `timestamp with time zone`, Npgsql отвергает `Kind = Unspecified`) и проверять результат из **нового** контекста с `AsNoTracking()`, иначе можно прочитать объект из кэша change tracker'а и не заметить, что запись в базу не дошла.

**Про время выполнения.** Юнит-тесты не трогают ни диск, ни сеть. Интеграционные поднимают Docker-контейнер и пересоздают схему на каждый тест — самая медленная часть набора, поэтому в интеграционный проект стоит выносить только то, что действительно требует настоящей БД или HTTP-пайплайна.

---

## 🗄 Кеширование (Redis, сервис Events)

Кеш появился ровно там, где чтений заметно больше записей, — в сервисе Events. Redis изолирован за портом `ICacheService` (Application), реализация `RedisCacheService` — в Infrastructure, соединение `IConnectionMultiplexer` регистрируется singleton'ом. `EventService` реализует **cache-aside**: сначала читает кеш, при промахе идёт в репозиторий и кладёт результат обратно с TTL. Ключи собраны в `Caching/CacheKeys`, TTL — в `Options/CacheOptions` из секции `Cache` (см. [«Настройка Redis и TTL кеша»](#настройка-redis-и-ttl-кеша)).

| Ключ | Что лежит | Стратегия актуализации | TTL |
|---|---|---|---|
| `event:{id}` | `EventInfoDto` одного события (`GET /events/{id}`) | Delete-on-Write + TTL как подстраховка | 60 сек |
| `events:top10` | Топ-10 событий по проценту проданных мест (`GET /events/top`) | только TTL | 300 сек |

Кешируются два независимых сценария, каждый со своей стратегией обновления.

**`events:top10` (TTL-only, 300 сек).** Список читает без авторизации каждый посетитель главной страницы — при промахе шло бы обращение в базу на каждый визит. Явная инвалидация избыточна: рейтинг всё равно меняется постепенно (по мере бронирований), а не скачками, и небольшое устаревание для агрегата некритично. TTL 5 минут — компромисс между нагрузкой на БД и свежестью списка.

**`event:{id}` (Delete-on-Write, TTL 60 сек как подстраховка).** Для карточки события устаревание заметнее — доступные места должны быть близки к актуальным. Выбран Delete-on-Write (инвалидация вместо обновления): при изменении, удалении события (`EventService`) и при уменьшении мест из `BookingConfirmedConsumer` ключ `event:{id}` просто удаляется из кеша. Следующее чтение прогревает кеш заново из базы.

Update-on-Write отвергнут не потому, что его тяжело реализовать — здесь он был бы дёшев: `BookingConfirmedConsumer` уже держит свежую сущность в памяти, а `EventInfoDto` собирается в одно выражение, то есть критерий «объект легко собрать для кеша» выполняется. Он отвергнут потому, что его единственное реальное преимущество в этой системе не востребовано: постоянно прогретый ключ нужен, когда одну и ту же карточку читают десятки-сотни раз в секунду и даже короткое окно «пустого» кеша создаёт очередь запросов к базе. Такой нагрузки на карточку отдельного события здесь не ожидается, поэтому за неиспользуемую выгоду не стоит платить ни сложностью (при параллельной записи Update-on-Write требует защиты от гонок — блокировок либо сверки версий данных), ни риском записать в кеш несогласованный слепок: в `UpdateEventAsync` DTO собирается из отслеживаемой сущности, и если писать его в кеш, можно сохранить `AvailableSeats`, который в этот момент уже уменьшил Kafka-консьюмер. При удалении ключа такого риска нет — записывать нечего.

TTL 60 сек — не основной механизм актуальности (её обеспечивает инвалидация), а страховка сразу от двух вещей: от пропущенного пути изменения данных и от остаточной гонки самого cache-aside. Гонка выглядит так: читатель промахнулся и уже идёт в базу, в этот момент писатель сохраняет изменения и удаляет ключ, а читатель следом записывает в кеш свой — уже устаревший — результат. Полностью её убирают версионированием значений или отложенным повторным удалением; для учебного сервиса это неоправданное усложнение, поэтому окно неконсистентности просто ограничено сверху временем жизни ключа.

Цена выбранной стратегии, принятая осознанно: первый читатель после каждой записи платит за поход в базу — при Update-on-Write не платил бы никто. На ожидаемой нагрузке это один лишний запрос на событие после его изменения.

**Создание события.** `AddEventAsync` кеш не трогает вообще, и это не пропущенный случай: идентификатор у события новый, поэтому ключа `event:{id}` в Redis ещё не существует — инвалидировать нечего, а появится он при первом чтении карточки. Топ-10 при создании тоже не сбрасывается: новое событие с нулём проданных мест в рейтинг всё равно не попадёт, а когда места начнут продаваться, список обновится по TTL.

**Порядок операций.** Во всех местах кеш меняется только после успешного `SaveChangesAsync`: если процесс оборвётся между записью в базу и удалением ключа, база останется корректной, а кеш максимум доживёт до окончания TTL.

**Недоступность Redis.** `RedisCacheService` перехватывает исключения на каждой операции, логирует предупреждение и возвращает признак промаха / no-op — сервис работает напрямую с базой, клиент ошибки не видит. `IConnectionMultiplexer` регистрируется с `AbortOnConnectFail = false`, поэтому сервис поднимается и остаётся рабочим, даже если Redis недоступен на старте.

**Изменение данных через асинхронные события.** Места уменьшает не HTTP-запрос, а `BookingConfirmedConsumer` при обработке `BookingConfirmed` из Kafka (см. [«Асинхронное взаимодействие через Kafka»](#-асинхронное-взаимодействие-через-kafka)) — поэтому инвалидация стоит и там, сразу после `SaveChangesAsync`, тем же ключом `event:{id}`. Если сообщение пропущено (событие не найдено, мест не осталось, дубликат по идемпотентности), кеш не трогается: данные не менялись, инвалидировать нечего. Топ-10 при этом сознательно не инвалидируется — иначе каждое подтверждённое бронирование сбрасывало бы общий для всех посетителей ключ, а выигрыш в свежести рейтинга был бы незаметен.

**Что кешем не покрыто.** Список `GET /events` с произвольными фильтрами и пагинацией — у него слишком много вариантов ключа при небольшой выгоде, он ходит в базу напрямую. Кеша на запись (write-through) тоже нет — ни одна мутирующая операция не прогревает ключ заранее, кеш наполняют только читатели.

Поведение кеша закреплено юнит-тестами: `Events/EventCachingTests` (попадание, промах, инвалидация на уровне сервиса), `Events/RedisCacheServiceTests` (деградация самой реализации при недоступном Redis и битом значении) и два теста инвалидации в `Events/BookingConfirmedConsumerTests` — см. [«Unit-тесты»](#unit-тесты).

---

## 📅 Доменные правила по сервисам

### 🎟 Модель данных события (Event, сервис Events)

Rich Domain Model, как и раньше — сущность сама управляет количеством билетов:
- `Id` (`Guid`) — уникальный идентификатор события.
- `Title`, `Description` — базовая информация о мероприятии.
- `StartAt`, `EndAt` (`DateTime`) — временные рамки проведения.
- `TotalSeats` (`int`) — общее количество мест, должно быть больше нуля; сеттер приватный, менять можно только через `ChangeCapacity(totalSeats)` (при создании — через фабрику `Event.Create`).
- `AvailableSeats` (`int`) — свободные места; изменяется через `TryReserveSeats(count)`/`ReleaseSeats(count)` и пересчитывается в `ChangeCapacity`.

**Изменение вместимости** (`ChangeCapacity`, вызывается из `EventService.UpdateEventAsync`) сохраняет число уже проданных мест `TotalSeats - AvailableSeats`: при увеличении вместимости все новые места становятся свободными, при уменьшении — свободных становится меньше на ту же величину. Уменьшить вместимость ниже числа проданных мест нельзя (`ValidationException` → 400): Events не может «отменить» уже подтверждённые брони — они живут в другом сервисе. Инвариант держит сама сущность, а не слой приложения, поэтому обойти его присваиванием поля невозможно; на уровне API есть ещё и быстрый отказ `[Range(1, int.MaxValue)]` в `UpdateEventDto`.

Разница со спринтом 8: `TryReserveSeats` теперь вызывается не из HTTP-запроса на бронирование, а из `BookingConfirmedConsumer` при обработке сообщения Kafka — синхронной связи между бронированием и уменьшением мест больше нет.

На этих же двух полях считается «популярность» события для топ-10: доля проданных мест `(TotalSeats - AvailableSeats) / TotalSeats`. Отдельного поля-счётчика в сущности нет — величина производная и вычисляется запросом (`GetTopPopularAsync`), см. [«Кеширование»](#-кеширование-redis-сервис-events).

### 👤 Модель данных пользователя (User, сервис Users)

Создаётся через фабричный метод `Create`, а не публичный конструктор:
- `Id` (`Guid`) — уникальный идентификатор.
- `Login` (`string`) — уникален в пределах системы (уникальный индекс в БД).
- `PasswordHash` (`string`) — хеш пароля (BCrypt), пароль в открытом виде не хранится.
- `Role` (`UserRole`) — `User` или `Admin`.

### 📦 Модель данных бронирования (Booking, сервис Bookings)

- `Id` (`Guid`) — уникальный идентификатор брони.
- `EventId`, `UserId` (`Guid`) — только значения-идентификаторы, без навигационных свойств: Event и User — сущности других сервисов, в БД Bookings их нет.
- `Status` (`BookingStatus`): `Pending` (создана, ждёт обработки) → `Confirmed`/`Rejected` (фоновым сервисом) или `Cancelled` (пользователем/админом).
- `CreatedAt` (`DateTime`) — момент создания.
- `ProcessedAt` (`DateTime?`) — момент обработки фоновым сервисом либо отмены.

**При создании** (`BookingService.CreateBookingAsync`) единственная оставшаяся проверка — лимит активных броней пользователя, по умолчанию **10** одновременных `Pending`/`Confirmed` (`Booking:MaxActiveBookingsPerUser`, секция `Booking` в `appsettings.json`, биндится через `IOptions`; иначе `BookingLimitExceededException` → 409). Проверки существования события, его начала (`EventAlreadyStartedException`) и наличия мест (`NoAvailableSeatsException`) удалены вместе с доступом Bookings к данным Events.

**При отмене** (`CancelBookingAsync`, `DELETE /bookings/{id}`): бронь должна существовать (`NotFoundException` → 404), отменяет владелец либо Admin (`ForbiddenException` → 403), повторная отмена уже `Cancelled`/`Rejected` брони запрещена (`Booking.Cancel()` → `InvalidOperationDomainException` → 400). Проверка «событие ещё не началось» отсюда тоже удалена — Bookings не знает дат события.

### Фоновая обработка (Bookings)

`BookingProcessingBackgroundService` не изменилась в части опроса: раз в 5 секунд забирает `Pending`-брони, на каждую — свой scope, задержка 2 секунды имитирует внешнюю интеграцию. Разница — после `booking.Confirm()` и сохранения в БД сервис публикует `BookingConfirmed` в Kafka (см. [«Асинхронное взаимодействие через Kafka»](#-асинхронное-взаимодействие-через-kafka)); ошибка публикации логируется отдельно и не откатывает уже подтверждённую бронь.

### 🔄 Пример сквозного сценария

**Шаг 0.** Клиент получает JWT в Users (`POST /auth/login`) и передаёт его в заголовке `Authorization: Bearer <token>` во все запросы к Events/Bookings.

**Шаг 1.** Админ создаёт событие в Events: `POST /events`, `totalSeats: 100` → `availableSeats: 100`.

**Шаг 2.** Пользователь бронирует место: `POST /events/{id}/book` → `202 Accepted`, бронь в статусе `Pending` (Bookings не проверяет ни лимит мест события, ни его существование — только собственный лимит броней пользователя).

**Шаг 3.** Через 2–7 секунд фоновый сервис Bookings подтверждает бронь: `Pending` → `Confirmed`, публикует `BookingConfirmed` в Kafka.

**Шаг 4.** Events получает сообщение, уменьшает `availableSeats` на `SeatsCount` и сразу после сохранения в базу удаляет ключ `event:{id}` из Redis. Повторный `GET /events/{id}` промахнётся по кешу, прочитает актуальное значение из базы и прогреет кеш заново — раньше уменьшение мест происходило синхронно в момент бронирования, теперь асинхронно, с задержкой на публикацию и обработку сообщения.

**Шаг 4a.** Виджет главной страницы (`GET /events/top`) увидит изменившуюся долю проданных мест не сразу, а в пределах TTL (до 5 минут) — это осознанный компромисс, см. [«Кеширование»](#-кеширование-redis-сервис-events).

**Шаг 5.** Владелец (или Admin) может отменить бронь: `DELETE /bookings/{id}` → `204 No Content`. Отмена — по-прежнему только в Bookings; `availableSeats` в Events при этом не восстанавливается (обратного потока «BookingCancelled» в этом спринте не реализовано).

---

## 📝 Лицензия

Распространяется под лицензией MIT.

---

Автор: Александр Куров [@itsanti](https://github.com/itsanti)

Курс: [Продвинутая разработка на C# и .NET ](https://practicum.yandex.ru/middle-csharp) (Яндекс Практикум)
