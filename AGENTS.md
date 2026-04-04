# Rapi — AI Agent Instructions

## Project Overview

**Rapi** is a cross-platform remote access library for .NET. It allows a .NET client to control a remote machine (VM, container, physical host) over HTTP — managing files, spawning processes, proxying SFTP transfers, and relaying HTTP requests.

The system has two sides:
- **`Rapi/`** — client library (interfaces, DTOs, connection logic)
- **`RapiAgent/`** — ASP.NET Core HTTP server deployed on the remote machine

Supporting projects:
- **`Rapi.Mocks/`** — in-process mock implementations for unit testing consumers
- **`Rapi.Tests/`** — integration tests against a live in-process agent
- **`Rapi.Sandbox/`** — manual connectivity console app
- **`test-image/`** — shell scripts for building a Debian VM disk image with RapiAgent as a systemd service

## Tech Stack

| Layer | Technology |
|---|---|
| Language | C# (latest) |
| Runtime | .NET 10.0 (all projects) |
| Web framework | ASP.NET Core (Kestrel), Generic Host (`WebApplication`) |
| RPC | CoreRPC + CoreRPC.AspNetCore (JSON over HTTP at `/rpc`) |
| JSON | Newtonsoft.Json 13.x |
| SFTP | SSH.NET |
| Testing | NUnit 4 |
| Platform interop | P/Invoke: libc/libSystem (Unix), kernel32 (Windows) |
| Package management | Central Package Management (`Directory.Packages.props`) |
| CI/CD | GitHub Actions (`.github/workflows/`) |

## Build and Run Commands

```bash
# Build the entire solution
dotnet build Rapi.sln

# Run all integration tests (requires local OpenSSH for SFTP tests)
dotnet test Rapi.Tests/Rapi.Tests.csproj

# Start the agent (replace with desired URL)
dotnet run --project RapiAgent -- http://0.0.0.0:5000

# Start as a Windows service (uses UseWindowsService() / Generic Host)
dotnet run --project RapiAgent -- http://0.0.0.0:5000 --service

# Run the sandbox connectivity check
dotnet run --project Rapi.Sandbox -- http://HOST:5000/rpc
```

## Architecture and Key Patterns

### RPC Services

Five named RPC services are registered in `RapiAgent/Program.cs` via `DictionaryTargetSelector`:

| Target name | Interface (client) | Implementation (agent) | Mock |
|---|---|---|---|
| `"FileSystem"` | `IRapiFileSystemRpc` | `RapiFileSystemRpc` | `MockFileSystem` |
| `"Processes"` | `IRapiProcesses` | `RapiProcessesRpc` | `RapiProcessesMock` |
| `"Sftp"` | `IRapiSftpRpc` | `RapiSftpRpc` | `RapiSftpMock` |
| `"SystemInfo"` | `IRapiSystemInfoRpc` | `RapiSystemInfoRpc` | `MockSystemInfo` |
| `"WebRequest"` | `IRapiWebRequestRpc` | `RapiWebRequestRpc` | `RapiWebRequestMock` |

The target name string **must match exactly** between the client (`ConstTargetExtractor`) and the server (`DictionaryTargetSelector`).

> **Note:** `RapiAgent/Startup.cs` is kept alongside `Program.cs` specifically for the test infrastructure (`RapiTestHost` uses `WebHostBuilder` + `UseStartup<Startup>()`). Do not remove it.

### File Streaming

Large files bypass RPC messages entirely. Use the HTTP endpoints:
- `GET /filestream/read?path=...` — download from agent
- `POST /filestream/write?path=...` — upload to agent

Client-side: `RapiFileStream` / `IRapiFileStream` (uses a shared long-lived `HttpClient`).

### Path Handling

Always use `RapiPath` (from `Rapi/RapiPath.cs`) on the client side instead of `System.IO.Path`. It resolves path semantics (separator, roots, invalid chars) based on the remote platform detected from `RapiSystemInfo`.

### Process Spawning

- **Unix**: `posix_spawnp` via P/Invoke + a temporary Python shim for `setsid`/`TIOCSCTTY` (`UnixProcessFactory.cs`)
- **Windows**: `CreateProcessW` + Job Objects with `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE` (`Win32ProcessFactory.cs`)

The factory is selected at runtime in the shared agent service registration based on `RuntimeInformation.IsOSPlatform`.

### HttpClient lifetime

Follow the Microsoft guideline at <https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines#recommended-use>.

- In hosted/server code (for example `RapiAgent`), configure outbound HTTP through `AddHttpClient(...)` / `IHttpClientFactory`. Do **not** create `new HttpClient()` per request, RPC call, or operation.
- In library code where DI is not available (for example `Rapi/` connection helpers), use a shared long-lived `HttpClient` backed by `SocketsHttpHandler` with `PooledConnectionLifetime` set explicitly.
- Disable automatic cookie handling unless the feature explicitly requires a cookie container; pooled handlers can otherwise leak cookies between unrelated requests.
- When a dependency exposes both a `string` overload and an overload that accepts `HttpClient`, prefer the overload that accepts a managed `HttpClient` so lifetime stays under our control.
- Any new HTTP helper should reuse the existing shared/factory-managed client strategy instead of introducing another ad-hoc client.

## Code Conventions

### Nullable reference types are enabled everywhere

`<Nullable>enable</Nullable>` is set globally in `Directory.Build.props`. All new code must be nullable-correct:
- Use `string?`, `T?` for genuinely optional values.
- Use `!` (null-forgiving) only when null is structurally impossible but the compiler cannot prove it.
- DTO properties from JSON deserialization (e.g., `RapiSystemInfo`, `RapiPlatformInfo`) are typed as nullable — use `!` at the call site when the value is known to be present.

### All warnings are errors

`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` is set globally in `Directory.Build.props`. The build must produce **0 warnings**. Always run `dotnet build Rapi.sln` and fix everything before considering a task done.

Exceptions explicitly suppressed in `Directory.Build.props`:
- `NU1900`–`NU1904` — audit warnings from transitive `Microsoft.AspNetCore.*` 2.x pulled in by `CoreRPC.AspNetCore`.
- `ASPDEPR004`, `ASPDEPR008` — suppressed in `Rapi.Tests` only, because `RapiTestHost` intentionally uses the legacy `WebHostBuilder`.
- `CS8981` — suppressed in `UnixNative.cs` only, for the lowercase-named P/Invoke delegates (`dup`, `fork`, `kill`, etc.).

### All service methods are async

Every interface method returns `Task` or `Task<T>`. Never write blocking calls.

### Unsafe blocks are allowed in RapiAgent

`<AllowUnsafeBlocks>true</AllowUnsafeBlocks>` is set in `RapiAgent.csproj`. Use unsafe code only for P/Invoke interop.

### Target framework

All projects target **net10.0**. Do not downgrade any project.

### Central Package Management

All NuGet package versions are declared in `Directory.Packages.props`. Do **not** add `Version="..."` attributes to `<PackageReference>` in individual `.csproj` files — only add the package name there. When adding a new dependency, add its version to `Directory.Packages.props` first.

### NuGet sources

`nuget.config` at the repo root restricts sources to `nuget.org` only. This prevents `NU1507` errors from machine-level package source configuration. Do not add other sources without also adding package source mappings.

### Adding a new RPC service

Follow this checklist in order:
1. Define the interface and DTOs in `Rapi/` (e.g., `IRapiExampleRpc.cs`)
2. Implement it in `RapiAgent/Rpc/` (e.g., `RapiExampleRpc.cs`)
3. Create a mock in `Rapi.Mocks/` (e.g., `RapiExampleMock.cs`)
4. Register the implementation in the shared `RapiAgent` service/RPC registration used by both `Program.cs` and `Startup.cs`
5. Register the mock in `Rapi.Mocks/MockRapiMachine.cs` and keep `RapiAgent/Startup.cs` wired through the shared registration path for tests
6. Expose it via `RapiConnection.cs` (add a property to the returned connection object)
7. Add integration tests in `Rapi.Tests/` (e.g., `RapiExampleTests.cs`)

## Testing

Tests use **NUnit 4** (`[TestFixture]`, `[Test]`, `[TestCase]`, `[OneTimeSetUp]`, etc.).

Integration tests use `RapiTestHost` (`[SetUpFixture]`) which:
- Starts a real in-process RapiAgent on a loopback port (auto-incrementing from 5000) using legacy `WebHostBuilder` + `Startup`
- Merges `config.json` and `config.local.json` (gitignored) for SFTP credentials

SFTP tests require a locally running OpenSSH server and `Rapi.Tests/config.local.json`:
```json
{
  "Sftp": {
    "Login": "user",
    "Password": "pass",
    "Host": "localhost",
    "Port": 22
  }
}
```

Skip SFTP tests if no local SSH server is available. Other test classes have no external dependencies.

## CI/CD

Two GitHub Actions workflows live in `.github/workflows/`:

| File | Trigger | Jobs |
|---|---|---|
| `test.yml` | Pull request (any branch) | restore → build → test |
| `release.yml` | Push of tag `v*.*.*` | test → NuGet publish → RapiAgent single-file publish (linux-x64, win-x64, osx-x64) → GitHub Release |

**NuGet publish** pushes `Rapi` as a package to GitHub Packages using `GITHUB_TOKEN`.

**RapiAgent publish** produces self-contained single-file binaries (`PublishSingleFile=true`, `--self-contained`) for each platform and attaches them to a GitHub Release created by `gh release create`.

## MCP Tools Usage

When you need to look up .NET, ASP.NET Core, or other Microsoft technology documentation, use the `microsoft-learn` MCP tools:
- Use `microsoft_docs_search` for conceptual questions and API lookups
- Use `microsoft_docs_fetch` to read a specific docs page
- Use `microsoft_code_sample_search` for .NET/C# code examples

When you need to look up library docs (CoreRPC, SSH.NET, NUnit, etc.), use the `context7` tools.

When working on complex multi-step tasks (e.g., adding a new service end-to-end), use `sequential-thinking` to plan your approach before writing code.

## Agent Skills

Keep every repository skill in its own file under `.github/skills/<skill-name>/SKILL.md`.
Do not inline skill definitions in `AGENTS.md`.
Repository-wide guidance in `AGENTS.md` may reference skills, but the skill content itself must live in the dedicated skill file.
