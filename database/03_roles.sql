BEGIN;

DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'attendance_app') THEN
        CREATE ROLE attendance_app LOGIN PASSWORD 'AppUser2026';
    END IF;
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'attendance_report') THEN
        CREATE ROLE attendance_report LOGIN PASSWORD 'Report2026';
    END IF;
END;
$$;

REVOKE CREATE ON SCHEMA public FROM PUBLIC;
GRANT USAGE ON SCHEMA public TO attendance_app, attendance_report;
GRANT CONNECT ON DATABASE attendance TO attendance_app, attendance_report;

GRANT SELECT, INSERT, UPDATE, DELETE
    ON users, groups, students, teachers, subjects, lessons, absences, excuse_documents, notifications
    TO attendance_app;
GRANT SELECT ON roles, audit_log TO attendance_app;
GRANT USAGE ON ALL SEQUENCES IN SCHEMA public TO attendance_app;

GRANT SELECT ON roles, groups, students, teachers, subjects, lessons, absences, excuse_documents
    TO attendance_report;
GRANT SELECT (id, login, full_name, role_id, is_locked, created_at) ON users TO attendance_report;

COMMIT;
