# AGENTS.md

## Cursor Cloud specific instructions

### Product context

This repo is a **drop-in ASP.NET WebForms chatbot module** for an existing expenses application. The runnable module lives on branch `cursor/add-expenses-chatbot-3b1a` (not on stub `main`). Full UI hosting requires **Windows + IIS / IIS Express** with a host WebForms site; Linux cloud agents validate the **chatbot service + SQL** path instead.

### Services

| Service | Linux cloud dev | Notes |
|---------|-----------------|-------|
| SQL Server (`dev/docker-compose.yml`) | Required for integration tests | Port `1433`, DB `ExpensesDev`, SA password `YourStrong@Passw0rd` |
| ASP.NET WebForms host | Not runnable on Linux | Copy module into host site per `README.md` |

### Standard commands

See `README.md` for integration into a host app. For this repo’s Linux dev loop:

```bash
# Start SQL Server and apply schema/procedures/seed data
./scripts/dev-db-up.sh

# Build and run integration tests (uses App_Code via test project)
export PATH="$HOME/.dotnet:$PATH"
dotnet test umschatbox.sln

# Stop SQL Server
./scripts/dev-db-down.sh
```

If `docker` permission is denied, the scripts fall back to `sudo docker`.

### Gotchas

- **No `package.json` / npm** — backend-only C# + SQL.
- **No linter config** in-repo; `dotnet build` is the compile check.
- Integration tests compile `App_Code/*.cs` directly; a small `System.Web` shim in the test project avoids needing the full WebForms stack on Linux.
- SQL init is **not** automatic on container first boot; always run `./scripts/dev-db-up.sh` after starting Docker.
