-- =====================================================================
-- ИС мониторинга прогулов учебной группы
-- 04_audit.sql — журнал аудита: триггеры на изменение данных
--
-- Запуск (после 01_schema.sql или миграции EF; повторный запуск безопасен):
--   psql -U postgres -d attendance -f 04_audit.sql
--
-- Кто изменил данные, триггер узнаёт из настройки сеанса app.user_id —
-- её выставляет приложение при каждом подключении (AuditUserInterceptor).
-- Изменения, сделанные вне приложения (psql, pgAdmin), пишутся с user_id = NULL.
-- =====================================================================

BEGIN;

-- SECURITY DEFINER: функция пишет в audit_log с правами владельца,
-- поэтому роли приложения достаточно права SELECT на журнал (см. 03_roles.sql).
CREATE OR REPLACE FUNCTION audit_trigger() RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
    old_row jsonb := CASE WHEN TG_OP <> 'INSERT' THEN to_jsonb(OLD) END;
    new_row jsonb := CASE WHEN TG_OP <> 'DELETE' THEN to_jsonb(NEW) END;
BEGIN
    -- Хеш пароля в журнал не попадает; фиксируется только факт смены пароля.
    IF TG_TABLE_NAME = 'users' THEN
        IF TG_OP = 'UPDATE' AND old_row ->> 'password_hash' IS DISTINCT FROM new_row ->> 'password_hash' THEN
            new_row := new_row || jsonb_build_object('password_changed', true);
        END IF;
        old_row := old_row - 'password_hash';
        new_row := new_row - 'password_hash';
    END IF;

    -- UPDATE без фактических изменений не протоколируется.
    IF TG_OP = 'UPDATE' AND old_row = new_row THEN
        RETURN NULL;
    END IF;

    INSERT INTO audit_log (user_id, action, table_name, record_id, old_values, new_values)
    VALUES (NULLIF(current_setting('app.user_id', true), '')::integer,
            TG_OP,
            TG_TABLE_NAME,
            (COALESCE(new_row, old_row) ->> 'id')::bigint,
            old_row,
            new_row);
    RETURN NULL;
END;
$$;

-- Список таблиц совпадает с AuditService.AuditedTables.
-- roles (неизменяемый справочник), notifications (создаются системой) и сам audit_log не протоколируются.
DO $$
DECLARE
    t text;
BEGIN
    FOREACH t IN ARRAY ARRAY['users', 'groups', 'students', 'teachers', 'subjects',
                             'lessons', 'absences', 'excuse_documents']
    LOOP
        EXECUTE format('DROP TRIGGER IF EXISTS trg_audit ON %I', t);
        EXECUTE format('CREATE TRIGGER trg_audit AFTER INSERT OR UPDATE OR DELETE ON %I
                        FOR EACH ROW EXECUTE FUNCTION audit_trigger()', t);
    END LOOP;
END;
$$;

COMMIT;
