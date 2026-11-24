BEGIN;

ALTER TABLE lessons ADD COLUMN IF NOT EXISTS classroom varchar(10);
ALTER TABLE students ADD COLUMN IF NOT EXISTS phone varchar(20);

COMMENT ON COLUMN lessons.classroom IS 'Номер аудитории, в которой проводится занятие';
COMMENT ON COLUMN students.phone IS 'Контактный телефон студента в формате +7XXXXXXXXXX';

ALTER TABLE notifications ALTER COLUMN message TYPE text;

UPDATE lessons AS l
SET classroom = m.room
FROM (VALUES
        ('Базы данных', '305'),
        ('Основы алгоритмизации и программирования', '307'),
        ('Веб-программирование', '310'),
        ('Иностранный язык', '214'),
        ('Элементы высшей математики', '201'),
        ('Физическая культура', 'Спортзал')
     ) AS m(subject, room)
JOIN subjects AS s ON s.name = m.subject
WHERE l.subject_id = s.id
  AND l.classroom IS NULL;

ALTER TABLE lessons DROP CONSTRAINT IF EXISTS uq_lessons_teacher_date_pair;
ALTER TABLE lessons ADD CONSTRAINT uq_lessons_teacher_date_pair UNIQUE (teacher_id, date, pair_number);

ALTER TABLE students DROP CONSTRAINT IF EXISTS ck_students_phone;
ALTER TABLE students ADD CONSTRAINT ck_students_phone CHECK (phone ~ '^\+7[0-9]{10}$');

COMMIT;
