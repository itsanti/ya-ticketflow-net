# TicketFlow .NET

[![Build Status](https://img.shields.io/badge/.NET-10-blueviolet)](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Учебный проект — сервис для управления мероприятиями на базе ASP.NET Core Web API (.NET 10).

---

## 🚀 Текущий статус

- **Спринт 1**: REST CRUD API для управления мероприятиями, хранение данных в памяти ✅
- **Спринт 2**: Глобальная обработка ошибок, фильтрация, пагинация и Unit-тесты ✅

---

## 🏗 Структура проекта
```
├── TicketFlow/
│   ├── Controllers/        # Эндпоинты REST API
│   ├── DTOs
│   │   ├── Events/         # Объекты передачи данных (CreateEventDto, UpdateEventDto)
│   │   └── Pagination/
│   ├── Exceptions/        # пользовательские исключения
│   ├── Middlewares/     # Кастомные middleware (логирование запросов)
│   ├── Models/             # Доменные модели (Event)
│   ├── Services/            # Бизнес-логика (IEventService, EventService)
│   ├── Program.cs          # Точка входа и конфигурация приложения
│   └── appsettings.json    # Настройки приложения
└── TicketFlow.Tests/     # Юнит-тесты для бизнес-логики (xUnit)
```

## ✨ Реализованный функционал

- [x] CRUD операции для мероприятий (`Event`)
- [x] Хранение данных в памяти (`List<Event>`)
- [x] Бизнес-логика вынесена в сервис через DI
- [x] Валидация входных данных (обязательные поля, `EndAt > StartAt`)
- [x] Единый формат ошибок через Problem Details
- [x] Логирование HTTP-запросов
- [x] Swagger UI для тестирования API
- [x] Глобальный обработчик исключений (`Middleware`) с возвратом Problem Details (RFC 7807)
- [x] Фильтрация событий по названию (регистронезависимая) и диапазону дат
- [x] Пагинация результатов (страница, размер страницы)
- [x] Покрытие бизнес-логики `EventService` юнит-тестами (успешные и неуспешные сценарии)
---

## 🛠 Технологический стек

- **Runtime**: .NET 10 (C# 13)
- **Framework**: ASP.NET Core Web API
- **API Documentation**: Swashbuckle (Swagger UI)

---

## ⚙️ Запуск проекта

### Предварительные требования

- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

### Зависимости

```bash
dotnet add package Swashbuckle.AspNetCore # Swagger UI
```

### Установка и запуск
 
1. **Клонируйте репозиторий:**
   ```bash
   git clone https://github.com/itsanti/ticketflow.git
   cd ticketflow
   ```
 
2. **Перейдите в папку проекта:**
   ```bash
   cd TicketFlow
   ```
 
3. **Соберите проект:**
   ```bash
   dotnet build
   ```
 
4. **Запустите проект:**
   ```bash
   dotnet run
   ```
 
5. **Откройте Swagger UI:**
   ```
   https://localhost:7241/swagger
   ```

## 📡 API Endpoints
 
| Метод    | Путь              | Описание                        | Статусы           |
|----------|-------------------|---------------------------------|-------------------|
| `GET`    | `/events`         | Список событий с фильтрацией и пагинацией | 200 |
| `GET`    | `/events/{id}`    | Получить событие по ID          | 200, 404          |
| `POST`   | `/events`         | Создать новое событие           | 201, 400          |
| `PUT`    | `/events/{id}`    | Обновить событие целиком        | 200, 400, 404     |
| `DELETE` | `/events/{id}`    | Удалить событие                 | 204, 404          |
 
 Параметры запроса (Query): `title` (строка), `from` (дата), `to` (дата), `page` (int), `pageSize` (int).
 
### Пример запроса (POST /events)
 
```json
{
  "title": "Tech Conference 2026",
  "description": "Ежегодная конференция по современным технологиям",
  "startAt": "2026-04-15T10:00:00",
  "endAt": "2026-04-17T18:00:00"
}
```
 
### Пример ответа (200 OK)
 
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "Tech Conference 2026",
  "description": "Ежегодная конференция по современным технологиям",
  "startAt": "2026-04-15T10:00:00",
  "endAt": "2026-04-17T18:00:00"
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
      "startAt": "2026-04-15T10:00:00",
      "endAt": "2026-04-17T18:00:00"
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

Проект содержит юнит-тесты на базе **xUnit**, покрывающие CRUD-операции, фильтрацию и валидацию в `EventService`.

Для запуска тестов используйте команду:
```bash
dotnet test
```
 
---

## 📝 Лицензия

Распространяется под лицензией MIT.

---

Автор: Александр Куров [@itsanti](https://github.com/itsanti)

Курс: [Продвинутая разработка на C# и .NET ](https://practicum.yandex.ru/middle-csharp) (Яндекс Практикум)

