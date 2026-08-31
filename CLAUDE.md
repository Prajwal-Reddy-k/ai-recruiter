# AI Recruiter — Project Instructions

## Database safety (read before any migration work)

**NEVER run `dotnet ef database drop`, `DROP DATABASE`, or any destructive
recreate of `AIRecruiterDb` without first checking for non-seed data.**

Before dropping the local database for any reason (schema conflicts, a
migration that won't apply cleanly, "just reset it"), run:

```bash
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -C -i "database/check-non-seed-data.sql"
```

- If it returns **zero rows**, the database only contains seed data
  (`DataSeeder.cs` demo accounts) and it is safe to drop and recreate.
- If it returns **any rows**, a real person has registered or the app has
  been used for real — **stop and ask the user for explicit confirmation**
  before dropping. Dropping the database destroys those rows and everything
  that references them (profiles, applications, saved jobs, interviews,
  resumes on disk) with no way to recover them.

**Prefer a normal migration over a drop whenever possible** — `dotnet ef
database update` after adding a new migration handles almost every schema
change without data loss. Reach for `database drop` only as a last resort
for LocalDB-specific scaffolding issues, and only after the check above
comes back clean.

This rule exists because an earlier session dropped the database to apply a
migration while a user's freshly-registered account was the only non-seed
row in it, deleting it permanently.
