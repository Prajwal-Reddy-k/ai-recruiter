-- Safety check: run this BEFORE any `dotnet ef database drop` against AIRecruiterDb.
-- Lists every user row whose email is NOT one of the DataSeeder demo accounts.
-- If this returns any rows, DO NOT drop the database without explicit user
-- confirmation — those rows (and everything referencing them: profiles,
-- applications, saved jobs, interviews, resumes on disk, etc.) will be lost
-- permanently and cannot be recovered from a `DROP DATABASE`.

SELECT Id, Email, Role, CreatedAt
FROM AIRecruiterDb.dbo.Users
WHERE Email NOT LIKE '%@demo.airecruiter.dev'
ORDER BY CreatedAt;
