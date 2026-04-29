# TicketFlow .NET

[![Build Status](https://img.shields.io/badge/.NET-10-blueviolet)](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Учебный проект — сервис для управления мероприятиями на базе ASP.NET Core Web API (.NET 10).

---

## 🚀 Текущий статус

- **Спринт 1**: REST CRUD API для управления мероприятиями, хранение данных в памяти ✅
- **Спринт 2**: 📅 План

---

## 🏗 Структура проекта
```
TicketFlow/
├── Controllers/        # Эндпоинты REST API
├── DTOs/Events/        # Объекты передачи данных (CreateEventDto, UpdateEventDto)
├── Middlewares/        # Кастомные middleware (логирование запросов)
├── Models/             # Доменные модели (Event)
├── Services/           # Бизнес-логика (IEventService, EventService)
├── Program.cs          # Точка входа и конфигурация приложения
└── appsettings.json    # Настройки приложения
```

## ✨ Реализованный функционал

- [x] CRUD операции для мероприятий (`Event`)
- [x] Хранение данных в памяти (`List<Event>`)
- [x] Бизнес-логика вынесена в сервис через DI
- [x] Валидация входных данных (обязательные поля, `EndAt > StartAt`)
- [x] Единый формат ошибок через Problem Details
- [x] Логирование HTTP-запросов
- [x] Swagger UI для тестирования API

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
| `GET`    | `/events`         | Получить список всех событий    | 200               |
| `GET`    | `/events/{id}`    | Получить событие по ID          | 200, 404          |
| `POST`   | `/events`         | Создать новое событие           | 201, 400          |
| `PUT`    | `/events/{id}`    | Обновить событие целиком        | 200, 400, 404     |
| `DELETE` | `/events/{id}`    | Удалить событие                 | 204, 404          |
 
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
 
---

## 📝 Лицензия

Распространяется под лицензией MIT.

---

Автор: Александр Куров [@itsanti](https://github.com/itsanti)

Курс: [Продвинутая разработка на C# и .NET ](https://practicum.yandex.ru/middle-csharp) (Яндекс Практикум)

