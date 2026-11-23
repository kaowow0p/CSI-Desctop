-- =====================================================================
-- ИС мониторинга прогулов учебной группы
-- 03_roles.sql — роли PostgreSQL и права доступа (принцип минимальных привилегий)
--
-- Запуск от суперпользователя (после 01_schema.sql или миграции EF; повторный запуск безопасен):
--   psql -U postgres -d attendance -f 03_roles.sql
--
-- ВНИМАНИЕ: пароли ниже — учебные. При развёртывании замените их:
--   ALTER ROLE attendance_app PASSWORD '...';
-- =====================================================================

BEGIN;

DO $$
BEGIN
    -- Роль, под которой работает настольное приложение.
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'attendance_app') THEN
        CREATE ROLE attendance_app LOGIN PASSWORD 'AppUser2026';
    END IF;
    -- Роль для аналитики и выгрузок: только чтение, без хешей паролей и журнала аудита.
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'attendance_report') THEN
        CREATE ROLE attendance_report LOGIN PASSWORD 'Report2026';
    END IF;
END;
$$;

-- Никто, кроме владельца, не может создавать объекты в схеме.
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
GRANT USAGE ON SCHEMA public TO attendance_app, attendance_report;
GRANT CONNECT ON DATABASE attendance TO attendance_app, attendance_report;

-- Приложение: чтение и изменение данных, без права менять структуру БД.
GRANT SELECT, INSERT, UPDATE, DELETE
    ON users, groups, students, teachers, subjects, lessons, absences, excuse_documents, notifications
    TO attendance_app;
-- Справочник ролей и журнал аудита — только чтение (журнал пишут триггеры, см. 04_audit.sql).
GRANT SELECT ON roles, audit_log TO attendance_app;
GRANT USAGE ON ALL SEQUENCES IN SCHEMA public TO attendance_app;

-- Отчётная роль: только чтение; из users — без password_hash и служебных полей.
GRANT SELECT ON roles, groups, students, teachers, subjects, lessons, absences, excuse_documents
    TO attendance_report;
GRANT SELECT (id, login, full_name, role_id, is_locked, created_at) ON users TO attendance_report;

COMMIT;
