BEGIN;

TRUNCATE audit_log, notifications, absences, excuse_documents, lessons,
         students, teachers, subjects, groups, users
RESTART IDENTITY;

INSERT INTO users (id, login, password_hash, full_name, role_id, must_change_password) VALUES
    (1, 'admin',   '$2a$11$3XkL.TRcZWLnaln/F08XLe9RrEVEHRKrOA8CVBfXDjEXEHHEvkliW', 'Администратор системы',      1, FALSE),
    (2, 'curator', '$2a$11$mJi.irN1q0vwn41XCqmHIutdDHZuHKsfojtxu7yOOKXpuutp0Jwt6', 'Михайлова Татьяна Сергеевна', 2, FALSE),
    (3, 'teacher', '$2a$11$qpYMzKcd6KZywxglXYnnY.90xUhi1FcTNtdj7AT4smKqg71FZFRWi', 'Иванов Пётр Алексеевич',      3, FALSE),
    (4, 'headman', '$2a$11$KFh1N2.Us0B8oKY0HAiuHeOjE86NGx4pMrQ8ihPCh1/I5Ov5iXymO', 'Андреев Илья Сергеевич',      4, FALSE),
    (5, 'student', '$2a$11$.iCwxyZImIrFLD311EPEK.sNgolg4m4IpjC3Z99OwKRlA2phFJBHS', 'Белова Анна Дмитриевна',      5, TRUE);

INSERT INTO groups (id, name, specialty, course, curator_user_id) VALUES
    (1, 'ИС-21', '09.02.07 Информационные системы и программирование', 3, 2);

INSERT INTO teachers (id, last_name, first_name, middle_name, user_id) VALUES
    (1, 'Иванов',   'Пётр',    'Алексеевич',   3),
    (2, 'Смирнова', 'Елена',   'Викторовна',   NULL),
    (3, 'Петрова',  'Ольга',   'Николаевна',   NULL),
    (4, 'Сидоров',  'Алексей', 'Михайлович',   NULL),
    (5, 'Кузьмин',  'Виктор',  'Геннадьевич',  NULL);

INSERT INTO subjects (id, name) VALUES
    (1, 'Базы данных'),
    (2, 'Основы алгоритмизации и программирования'),
    (3, 'Веб-программирование'),
    (4, 'Иностранный язык'),
    (5, 'Элементы высшей математики'),
    (6, 'Физическая культура');

INSERT INTO students (id, group_id, last_name, first_name, middle_name, record_book_number, user_id) VALUES
    ( 1, 1, 'Андреев',   'Илья',      'Сергеевич',    '21-001', 4),
    ( 2, 1, 'Белова',    'Анна',      'Дмитриевна',   '21-002', 5),
    ( 3, 1, 'Васильев',  'Максим',    'Андреевич',    '21-003', NULL),
    ( 4, 1, 'Григорьева','Дарья',     'Олеговна',     '21-004', NULL),
    ( 5, 1, 'Козлов',    'Артём',     'Игоревич',     '21-005', NULL),
    ( 6, 1, 'Кузнецова', 'Полина',    'Алексеевна',   '21-006', NULL),
    ( 7, 1, 'Новикова',  'Виктория',  'Павловна',     '21-007', NULL),
    ( 8, 1, 'Орлов',     'Кирилл',    'Романович',    '21-008', NULL),
    ( 9, 1, 'Павлов',    'Денис',     'Викторович',   '21-009', NULL),
    (10, 1, 'Романова',  'Софья',     'Евгеньевна',   '21-010', NULL),
    (11, 1, 'Семёнов',   'Егор',      'Николаевич',   '21-011', NULL),
    (12, 1, 'Соколова',  'Алина',     'Максимовна',   '21-012', NULL),
    (13, 1, 'Тарасов',   'Никита',    'Владимирович', '21-013', NULL),
    (14, 1, 'Фёдорова',  'Ксения',    'Артёмовна',    '21-014', NULL),
    (15, 1, 'Захаров',   'Тимофей',   'Ильич',        '21-015', NULL),
    (16, 1, 'Морозова',  'Елизавета', 'Сергеевна',    '21-016', NULL),
    (17, 1, 'Лебедев',   'Даниил',    'Олегович',     '21-017', NULL),
    (18, 1, 'Волкова',   'Мария',     'Андреевна',    '21-018', NULL),
    (19, 1, 'Никитин',   'Арсений',   'Павлович',     '21-019', NULL),
    (20, 1, 'Зайцева',   'Ульяна',    'Денисовна',    '21-020', NULL);

INSERT INTO lessons (date, pair_number, subject_id, teacher_id, group_id, hours)
SELECT d::date, t.pair, t.subject_id, t.teacher_id, 1, 2
FROM generate_series(date_trunc('week', current_date) - interval '7 days',
                     date_trunc('week', current_date) + interval '6 days',
                     interval '1 day') AS d
JOIN (VALUES
    (1, 1, 1, 1), (1, 2, 5, 4), (1, 3, 4, 3),
    (2, 1, 2, 1), (2, 2, 3, 2), (2, 3, 6, 5),
    (3, 1, 1, 1), (3, 2, 2, 1), (3, 3, 5, 4),
    (4, 1, 3, 2), (4, 2, 4, 3), (4, 3, 1, 1),
    (5, 1, 2, 1), (5, 2, 3, 2), (5, 3, 6, 5)
) AS t(weekday, pair, subject_id, teacher_id) ON t.weekday = extract(isodow FROM d)
ORDER BY d, t.pair;

INSERT INTO excuse_documents (id, student_id, type, number, issue_date, valid_from, valid_to, status,
                              registered_by_user_id, registered_at, reviewed_by_user_id, reviewed_at, review_comment)
SELECT v.id, v.student_id, v.type, v.number, w.mon + v.issue_off, w.mon + v.from_off, w.mon + v.to_off, v.status,
       4, (w.mon + v.issue_off) + time '12:00',
       CASE WHEN v.status <> 'Pending' THEN 2 END,
       CASE WHEN v.status <> 'Pending' THEN (w.mon + v.issue_off + 1) + time '10:00' END,
       v.comment
FROM (SELECT (date_trunc('week', current_date) - interval '7 days')::date AS mon) AS w
CROSS JOIN (VALUES
    (1,  7, 'MedicalCertificate', '095-1123', 3, 1, 2, 'Approved', NULL),
    (2,  9, 'MedicalCertificate', '095-1187', 4, 3, 3, 'Pending',  NULL),
    (3, 12, 'Statement',          'З-17',     4, 4, 4, 'Rejected', 'Заявление подано после пропуска без согласования')
) AS v(id, student_id, type, number, issue_off, from_off, to_off, status, comment);

INSERT INTO absences (student_id, lesson_id, status, marked_by_user_id, marked_at, excuse_document_id)
SELECT a.student_id, l.id, a.status,
       CASE WHEN l.teacher_id = 1 THEN 3 ELSE 4 END,
       l.date + time '08:30' + (l.pair_number - 1) * interval '100 minutes',
       a.document_id
FROM lessons AS l
CROSS JOIN LATERAL (
    SELECT 5 AS student_id, 'Unexcused' AS status, NULL::integer AS document_id
    WHERE l.pair_number IN (1, 3)
    UNION ALL
    SELECT 7, 'Excused', 1
    FROM excuse_documents AS e WHERE e.id = 1 AND l.date BETWEEN e.valid_from AND e.valid_to
    UNION ALL
    SELECT 9, 'Unexcused', NULL
    FROM excuse_documents AS e WHERE e.id = 2 AND l.date BETWEEN e.valid_from AND e.valid_to
    UNION ALL
    SELECT 12, 'Unexcused', NULL
    FROM excuse_documents AS e WHERE e.id = 3 AND l.date = e.valid_from AND l.pair_number = 3
    UNION ALL
    SELECT s.id, 'Unexcused', NULL
    FROM students AS s
    WHERE s.id NOT IN (5, 7, 9, 12) AND (s.id * 3 + l.id) % 17 = 0
) AS a
WHERE l.date < current_date;

INSERT INTO notifications (user_id, student_id, message, created_at)
SELECT g.curator_user_id, s.id,
       format('Студент %s %s (%s): %s ч неуважительных пропусков за %s — превышен порог 12 ч.',
              s.last_name, s.first_name, g.name, sum(l.hours), to_char(current_date, 'MM.YYYY')),
       now()
FROM absences AS a
JOIN lessons  AS l ON l.id = a.lesson_id
JOIN students AS s ON s.id = a.student_id
JOIN groups   AS g ON g.id = s.group_id
WHERE a.status = 'Unexcused'
  AND l.date >= date_trunc('month', current_date)
GROUP BY s.id, s.last_name, s.first_name, g.name, g.curator_user_id
HAVING sum(l.hours) > 12;

SELECT setval(pg_get_serial_sequence('users', 'id'),            (SELECT max(id) FROM users));
SELECT setval(pg_get_serial_sequence('groups', 'id'),           (SELECT max(id) FROM groups));
SELECT setval(pg_get_serial_sequence('teachers', 'id'),         (SELECT max(id) FROM teachers));
SELECT setval(pg_get_serial_sequence('subjects', 'id'),         (SELECT max(id) FROM subjects));
SELECT setval(pg_get_serial_sequence('students', 'id'),         (SELECT max(id) FROM students));
SELECT setval(pg_get_serial_sequence('excuse_documents', 'id'), (SELECT max(id) FROM excuse_documents));

TRUNCATE audit_log RESTART IDENTITY;

COMMIT;
