BEGIN;

CREATE OR REPLACE PROCEDURE add_column_if_not_exists(
    p_table      text,
    p_column     text,
    p_definition text)
LANGUAGE plpgsql
AS $$
BEGIN
    IF to_regclass(format('public.%I', p_table)) IS NULL THEN
        RAISE EXCEPTION 'Таблица "%" не найдена', p_table;
    END IF;

    IF p_definition IS NULL OR p_definition = '' OR p_definition ~ '[;]|--' THEN
        RAISE EXCEPTION 'Недопустимое определение столбца: "%"', p_definition;
    END IF;

    IF EXISTS (SELECT 1
               FROM information_schema.columns
               WHERE table_schema = 'public'
                 AND table_name = p_table
                 AND column_name = p_column) THEN
        RAISE NOTICE 'Столбец %.% уже существует, изменения не требуются', p_table, p_column;
        RETURN;
    END IF;

    EXECUTE format('ALTER TABLE %I ADD COLUMN %I %s', p_table, p_column, p_definition);
    RAISE NOTICE 'Столбец %.% (%) добавлен', p_table, p_column, p_definition;
END;
$$;

CREATE OR REPLACE PROCEDURE set_unique_constraint(
    p_table      text,
    p_constraint text,
    p_columns    text[])
LANGUAGE plpgsql
AS $$
DECLARE
    v_missing    text;
    v_columns    text;
    v_not_null   text;
    v_duplicates bigint;
BEGIN
    IF to_regclass(format('public.%I', p_table)) IS NULL THEN
        RAISE EXCEPTION 'Таблица "%" не найдена', p_table;
    END IF;

    IF p_columns IS NULL OR cardinality(p_columns) = 0 THEN
        RAISE EXCEPTION 'Не указаны столбцы ограничения';
    END IF;

    SELECT string_agg(c, ', ')
    INTO v_missing
    FROM unnest(p_columns) AS c
    WHERE NOT EXISTS (SELECT 1
                      FROM information_schema.columns
                      WHERE table_schema = 'public'
                        AND table_name = p_table
                        AND column_name = c);
    IF v_missing IS NOT NULL THEN
        RAISE EXCEPTION 'В таблице "%" нет столбцов: %', p_table, v_missing;
    END IF;

    SELECT string_agg(quote_ident(c), ', '),
           string_agg(quote_ident(c) || ' IS NOT NULL', ' AND ')
    INTO v_columns, v_not_null
    FROM unnest(p_columns) AS c;

    EXECUTE format('SELECT count(*) FROM (SELECT 1 FROM %I WHERE %s GROUP BY %s HAVING count(*) > 1) AS d',
                   p_table, v_not_null, v_columns)
    INTO v_duplicates;
    IF v_duplicates > 0 THEN
        RAISE EXCEPTION 'Нельзя создать ограничение "%": в таблице "%" найдено % групп повторяющихся значений (%)',
            p_constraint, p_table, v_duplicates, v_columns;
    END IF;

    EXECUTE format('ALTER TABLE %I DROP CONSTRAINT IF EXISTS %I', p_table, p_constraint);
    EXECUTE format('ALTER TABLE %I ADD CONSTRAINT %I UNIQUE (%s)', p_table, p_constraint, v_columns);
    RAISE NOTICE 'Ограничение % UNIQUE (%) установлено для таблицы %', p_constraint, v_columns, p_table;
END;
$$;

REVOKE EXECUTE ON PROCEDURE add_column_if_not_exists(text, text, text) FROM PUBLIC;
REVOKE EXECUTE ON PROCEDURE set_unique_constraint(text, text, text[]) FROM PUBLIC;

CALL add_column_if_not_exists('students', 'email', 'varchar(100)');
CALL set_unique_constraint('students', 'uq_students_email', ARRAY['email']);

COMMIT;
