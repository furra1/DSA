# Анализ исходного архива

## Подтверждено по исходникам
- API был настроен через `UseMySql` и `ConnectionStrings:MySql`.
- SQL-файл явно предназначен для Microsoft SQL Server 2019+ и создаёт объекты в схеме `dsa`.
- Исходный `AppDbContext` отображал только Users на таблицу `users` с колонками в snake_case — это не соответствует SQL Server script.
- Исходный `appsettings.json` содержал лишь JSON статуса API и не содержал ConnectionStrings/JWT.
- `Controllers/AuthController.cs` и `Servise/JwtTokenService.cs` содержали дублированную реализацию `UserService`, а не auth controller / JWT token service.
- Исходный API controller имел только health endpoint и admin user endpoints.
- Client — Java library с HTTP transport/DTO, а не полноценный веб UI.

## Границы комплекта
- Маппинг EF Core выполнен для основных таблиц Users, Groups, Products, Tasks, Plans.
- Остальные таблицы создаются SQL-скриптом, но API/EF модели для них ещё не реализованы.
- Frontend — демонстрационный интерфейс. Данные статистики и задачи на Dashboard — визуальные примеры, не реальные значения из БД.
- Реальная загрузка списка пользователей предусмотрена в разделе «Команда» при наличии API URL и Admin JWT. UI-форма логина и полноценные CRUD-экраны — следующий этап.
