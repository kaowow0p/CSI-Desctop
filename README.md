# ИС для организации мониторинга прогулов учебной группы

Курсовой проект. C# / .NET 8 / WPF (MVVM), PostgreSQL 16, Entity Framework Core.

## Возможности

| Раздел | Кому доступен | Что делает |
|---|---|---|
| Журнал посещаемости | администратор, куратор, преподаватель, староста | отметка отсутствующих на занятии, смена статуса пропуска (куратор) |
| Справки | администратор, куратор, староста | регистрация оправдательных документов (староста), подтверждение / отклонение (куратор) |
| Мои пропуски | студент | собственные пропуски и их статусы |
| Уведомления | администратор, куратор | превышение месячного лимита неуважительных пропусков (12 ч) |
| Отчёты | администратор, куратор, преподаватель, староста | сводка пропусков группы за период, выгрузка в Excel |
| Расписание, группы, студенты, преподаватели, дисциплины, пользователи | администратор | справочники и учётные записи |
| Журнал аудита | администратор | кто, когда и что изменил (значения до и после) |

## Структура

| Путь | Назначение |
|---|---|
| `src/Attendance.Domain` | сущности, перечисления |
| `src/Attendance.Data` | `AttendanceDbContext`, Fluent API, миграции EF Core |
| `src/Attendance.Services` | бизнес-логика, проверка прав, отчёты |
| `src/Attendance.Desktop` | WPF-клиент (Views, ViewModels, DI) |
| `database/01_schema.sql` | создание таблиц и индексов |
| `database/02_seed.sql` | тестовые данные |
| `database/03_roles.sql` | роли PostgreSQL и права доступа |
| `database/04_audit.sql` | триггеры журнала аудита |
| `database/backup.ps1`, `restore.ps1` | резервное копирование и восстановление |

## Быстрый старт

1. Запустить PostgreSQL 16. Проще всего — в Docker (параметры совпадают с `appsettings.json`):
   ```bash
   docker compose up -d
   ```
   Если PostgreSQL установлен локально — создать БД `attendance` и поправить строку подключения
   в `src/Attendance.Desktop/appsettings.json`.

2. Создать схему — **одним** из способов:
   ```bash
   dotnet tool restore
   dotnet ef database update --project src/Attendance.Data --startup-project src/Attendance.Desktop
   ```
   или
   ```bash
   psql -U postgres -d attendance -f database/01_schema.sql
   ```

3. Загрузить тестовые данные, роли и триггеры аудита (все скрипты можно запускать повторно;
   `02_seed.sql` очищает таблицы и заполняет их заново):
   ```bash
   psql -U postgres -d attendance -f database/02_seed.sql
   psql -U postgres -d attendance -f database/03_roles.sql
   psql -U postgres -d attendance -f database/04_audit.sql
   ```
   Через Docker (PowerShell):
   ```powershell
   foreach ($f in '02_seed.sql', '03_roles.sql', '04_audit.sql') {
       docker cp database/$f attendance-db:/tmp/$f
       docker exec attendance-db psql -U postgres -d attendance -v ON_ERROR_STOP=1 -f /tmp/$f
   }
   ```

4. Открыть `Attendance.sln` в Visual Studio 2022 и запустить `Attendance.Desktop`
   (или `dotnet run --project src/Attendance.Desktop`).

## Тестовые учётные записи

| Логин | Пароль | Роль | Кто это |
|---|---|---|---|
| `admin` | `Admin2026` | Администратор | — |
| `curator` | `Curator2026` | Куратор | Михайлова Т. С., куратор группы ИС-21 |
| `teacher` | `Teacher2026` | Преподаватель | Иванов П. А. (Базы данных, Основы алгоритмизации и программирования) |
| `headman` | `Headman2026` | Староста | Андреев И. С., староста ИС-21 |
| `student` | `Student2026` | Студент | Белова А. Д. — при первом входе система потребует сменить пароль |

## Тестовые данные (`02_seed.sql`)

- Группа ИС-21 (3 курс), 20 студентов, 5 преподавателей, 6 дисциплин.
- Расписание на прошлую и текущую неделю (пн–пт по 3 пары) — даты считаются от дня запуска скрипта.
- Пропуски только по прошедшим занятиям:
  - **Козлов** систематически пропускает 1-ю и 3-ю пары — превышен лимит 12 ч, куратору создано уведомление;
  - **Новикова** — 2 дня по подтверждённой справке (пропуски уважительные);
  - **Павлов** — день пропусков, справка ждёт проверки куратором;
  - **Соколова** — заявление отклонено, пропуск остался неуважительным;
  - у остальных — единичные неуважительные пропуски.

## Безопасность

- Пароли пользователей хранятся только как bcrypt-хеш (cost 11). После 5 неудачных попыток входа учётная запись
  блокируется; пароль — не менее 8 символов, заглавные и строчные буквы, цифры; временный пароль меняется при первом входе.
- Права проверяются в слое сервисов (`UserSession.Demand`, `AccessScope`): куратор видит только свои группы,
  преподаватель — свои занятия, староста — свою группу, студент — свои пропуски.
- Медицинские диагнозы не хранятся — только реквизиты и период действия документа.

### Роли PostgreSQL (`03_roles.sql`)

| Роль | Права |
|---|---|
| `attendance_app` | чтение и изменение данных; `roles` и `audit_log` — только чтение; изменять структуру БД нельзя |
| `attendance_report` | только чтение, без `password_hash` и журнала аудита (для аналитики и выгрузок) |

Для разработки приложение подключается как `postgres`. При развёртывании смените учебные пароли ролей
(`ALTER ROLE attendance_app PASSWORD '...'`) и укажите роль приложения в строке подключения `appsettings.json`:
```
Host=localhost;Port=5432;Database=attendance;Username=attendance_app;Password=...
```

### Журнал аудита (`04_audit.sql`)

Триггеры на таблицах `users`, `groups`, `students`, `teachers`, `subjects`, `lessons`, `absences`, `excuse_documents`
пишут в `audit_log` операцию, таблицу, id записи и значения до/после изменения (jsonb). Пользователя триггер берёт из
настройки сеанса `app.user_id`, которую приложение выставляет при каждом подключении; изменения вне приложения
(psql, pgAdmin) записываются без пользователя. Хеш пароля в журнал не попадает — только отметка `password_changed`.
Функция триггера работает с правами владельца (`SECURITY DEFINER`), поэтому роль приложения не может изменить журнал.

## Резервное копирование и восстановление

Скрипты работают с Docker-контейнером `attendance-db`; копии сохраняются в `backups/` (папка исключена из git —
в копиях персональные данные).

```powershell
# Полная копия (pg_dump, формат custom), проверка архива, удаление копий старше 14 дней
powershell -ExecutionPolicy Bypass -File database\backup.ps1

# Восстановление (текущие данные заменяются; без -Force скрипт спросит подтверждение)
powershell -ExecutionPolicy Bypass -File database\restore.ps1 -File backups\attendance_20261124_0300.dump
```

Ежедневный запуск — Планировщик заданий Windows:
```powershell
schtasks /Create /TN "Attendance backup" /SC DAILY /ST 03:00 /TR "powershell -ExecutionPolicy Bypass -File C:\путь\к\проекту\database\backup.ps1"
```

Без Docker — те же команды напрямую: `pg_dump -U postgres -d attendance -Fc -f attendance.dump` и
`pg_restore -U postgres -d attendance --clean --if-exists --single-transaction attendance.dump`.
