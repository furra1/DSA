# DSA Manager — мобильная PWA + ASP.NET Core + SQL Server

Это мобильная, устанавливаемая веб-версия (PWA): интерфейс адаптируется под смартфон, имеет нижнюю панель навигации и может быть добавлен на главный экран Android/iOS из браузера. Серверная часть — ASP.NET Core .NET 8, данные — Microsoft SQL Server.

## Уже реализовано

- Авторизация по email или табельному номеру через JWT.
- Однократное создание первого администратора через `POST /api/auth/bootstrap-admin` (доступно только пока таблица Users пуста).
- Дашборд со статистикой из SQL Server.
- Задачи: список, поиск, фильтр, создание, назначение пользователя/группы/продукта, изменение статуса.
- Продукты: просмотр и создание.
- Команда: список пользователей и групп; создание сотрудников (Admin), создание групп.
- Excel: Admin/Manager может открыть раздел «Команда» и выгрузить профиль сотрудника + список его выполненных задач в `.xlsx` (две вкладки).
- Планы: просмотр и создание.
- Мобильная PWA-оболочка, manifest, service worker и иконка.

Пока не реализованы: редактирование/удаление сущностей, пункты плана и их KPI, комментарии, вложения, push-уведомления, refresh tokens, аудит действий в интерфейсе, полноценная ролевая матрица доступа. Экспорт Excel доступен для завершённых задач сотрудника. Это следующая очередь разработки; текущую сборку не следует считать production-ready.

## Требования

- Windows 10/11
- .NET 8 SDK
- Python 3
- SQL Server 2022 Developer и SSMS

## База данных

Уже созданную БД `DSAManager` и выполненный SQL-скрипт повторно создавать/запускать не нужно. Если настраиваешь с нуля: создай пустую БД `DSAManager` и выполни `database/DSA_Manager_SQL_Server_v1.sql` именно в ней.

Проверь `backend/appsettings.Development.json`:

```json
"ConnectionStrings": {
  "SqlServer": "Server=localhost;Database=DSAManager;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

Замени демонстрационный `Jwt:Key` на случайный секрет не короче 32 байт. В PowerShell его можно создать так:

```powershell
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
```

## Запуск на компьютере

Из корневой папки проекта:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\start-dsa.ps1
```

Скрипт восстановит пакеты, соберёт API и запустит:

- Frontend: `http://localhost:5500`
- API: `http://localhost:5000`
- Swagger: `http://localhost:5000/swagger`
- Health: `http://localhost:5000/api/health`

Не закрывай два окна PowerShell, пока пользуешься приложением. Для остановки API — Ctrl+C в окне backend; frontend-окно закрой отдельно.

## Первый вход

Открой `http://localhost:5500`, вкладка **«Первый запуск»**, создай администратора. Это одноразовая операция: endpoint перестаёт создавать Admin после появления первого пользователя. Сохрани пароль.

Дальше используй вкладку «Войти». Email или табельный номер и пароль.

## Запуск на телефоне в той же Wi-Fi сети

1. Запусти `start-dsa.ps1` на компьютере.
2. Скрипт выведет адреса вида `http://192.168.x.x:5500` и `http://192.168.x.x:5000`.
3. На телефоне, подключённом к той же Wi-Fi сети, открой адрес frontend с IP компьютера, например `http://192.168.1.20:5500`.
4. На первом экране нажми «Настройки сервера» и укажи API адрес с тем же IP, например `http://192.168.1.20:5000`.
5. Создай администратора/выполни вход. В браузере телефона выбери «Добавить на главный экран» / «Установить приложение».

Если телефон не открывает адрес, разреши Python и .NET доступ в частной сети Windows Firewall. Не открывай эти dev-порты в публичный интернет. Для доступа вне локальной сети нужен отдельный HTTPS-деплой и безопасная конфигурация.

## API

- `POST /api/auth/bootstrap-admin` — первый Admin, только при пустой Users.
- `POST /api/auth/login` — вход.
- `GET /api/dashboard` — сводка.
- `GET /api/tasks`, `POST /api/tasks`, `PATCH /api/tasks/{id}/status`.
- `GET /api/products`, `POST /api/products`.
- `GET /api/users`, `GET /api/groups`, `POST /api/groups`.
- `GET /api/plans`, `POST /api/plans`.
- `GET /api/admin/users/{userId}/export.xlsx` — Excel-отчёт сотрудника (только Admin/Manager).

Большинство рабочих endpoint требуют JWT. Для production необходимо добавить rate limiting, HTTPS, секреты через environment/user-secrets, детальную ролевую авторизацию, тесты и аудит.
