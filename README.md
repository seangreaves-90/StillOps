# StillOps

`StillOps` is a .NET 10 Aspire-based starter solution for a rum distillery operations platform.

The current codebase establishes the initial runnable topology and operator shell:

- a Blazor Server web application for the internal workspace
- a background ingestion worker for future device/sensor processing
- shared service defaults for health checks, OpenTelemetry, service discovery, and HTTP resilience
- integration tests that verify the starter topology boots and exposes health/auth behavior correctly

At this stage, the repository is primarily an operational foundation rather than a finished domain implementation.

## Intent

StillOps is being built to support internal distillery operations workflows. The current starter emphasizes:

- an authenticated internal shell
- health and status visibility
- reloadable operational settings
- a separate ingestion process for future live device traffic
- Aspire orchestration for local multi-service development

The existing code and comments indicate that fuller ingestion and sensor workflows are planned for later work, while the current topology intentionally reports starter or placeholder states where live integrations do not yet exist.

## Solution Structure

Top-level layout:

```text
StillOps/
|-- src/
|   |-- entrypoints/
|   |   |-- StillOps.Web/
|   |   `-- StillOps.Ingestion/
|   `-- hosting/
|       |-- StillOps.AppHost/
|       `-- StillOps.ServiceDefaults/
|-- tests/
|   `-- integration/
|       `-- StillOps.AppHost.IntegrationTests/
|-- Directory.Build.props
|-- Directory.Packages.props
|-- global.json
`-- StillOps.slnx
```

## Projects

### `src/hosting/StillOps.AppHost`

The Aspire app host that composes the local distributed application. It currently starts:

- `stillops-web`
- `stillops-ingestion`

Entry point:

- [AppHost.cs](/C:/Dev/codebase/StillOps/src/hosting/StillOps.AppHost/AppHost.cs)

### `src/hosting/StillOps.ServiceDefaults`

Shared hosting extensions used by the web app and worker. It centralizes:

- OpenTelemetry logging, metrics, and tracing
- health check registration
- service discovery
- default HTTP client resilience configuration
- development-only `/health` and `/alive` endpoints for web apps

Key file:

- [Extensions.cs](/C:/Dev/codebase/StillOps/src/hosting/StillOps.ServiceDefaults/Extensions.cs)

### `src/entrypoints/StillOps.Web`

The internal web shell built as an ASP.NET Core / Blazor Server app.

Current responsibilities:

- cookie-based authentication for internal users
- authorization policy for internal operator/admin access
- Razor Pages for login/logout
- interactive server-rendered Blazor components
- starter shell pages for home, health, and configuration
- reloadable operational settings from `stillops-operational-settings.json`
- starter health reporting including a degraded sensor-connectivity readiness check

Key files:

- [Program.cs](/C:/Dev/codebase/StillOps/src/entrypoints/StillOps.Web/Program.cs)
- [Routes.razor](/C:/Dev/codebase/StillOps/src/entrypoints/StillOps.Web/Components/Routes.razor)
- [ShellLayout.razor](/C:/Dev/codebase/StillOps/src/entrypoints/StillOps.Web/Components/Layout/ShellLayout.razor)
- [StarterSettings.cs](/C:/Dev/codebase/StillOps/src/entrypoints/StillOps.Web/Configuration/StarterSettings.cs)
- [stillops-operational-settings.json](/C:/Dev/codebase/StillOps/src/entrypoints/StillOps.Web/stillops-operational-settings.json)

Current shell navigation includes:

- `/shell`
- `/shell/health`
- `/shell/configuration`

The public root page at `/` is a lightweight landing page linking into the internal workspace.

### `src/entrypoints/StillOps.Ingestion`

Background worker service intended to become the ingestion pipeline for live device and sensor traffic.

Current state:

- host boots successfully under Aspire
- worker logs periodic heartbeat-style messages
- no live device traffic or ingestion pipeline logic is implemented yet

Key files:

- [Program.cs](/C:/Dev/codebase/StillOps/src/entrypoints/StillOps.Ingestion/Program.cs)
- [Worker.cs](/C:/Dev/codebase/StillOps/src/entrypoints/StillOps.Ingestion/Worker.cs)

### `tests/integration/StillOps.AppHost.IntegrationTests`

Integration tests validate the starter topology end-to-end through Aspire's testing support.

Current coverage includes:

- app host startup
- both services reaching the running state
- `/health` returning `200 OK`
- the sensor-connectivity health check being present
- unauthenticated `/shell` requests redirecting to `/login`

Key file:

- [AppHostIntegrationTests.cs](/C:/Dev/codebase/StillOps/tests/integration/StillOps.AppHost.IntegrationTests/AppHostIntegrationTests.cs)

## Current Functional Scope

Implemented now:

- Aspire application topology with web and worker projects
- internal web shell scaffold
- cookie auth and authorization policy
- login/logout endpoints
- health and liveness infrastructure
- OpenTelemetry wiring
- reload-on-change operational settings
- integration tests for the starter baseline

Not implemented yet:

- real sensor ingestion
- device communication
- production identity flows
- domain persistence or business workflows beyond the starter shell

The code comments explicitly describe some of the current behavior as starter-only and note that fuller flows are planned for later work.

## Configuration

### SDK

The repository is pinned to:

- .NET SDK `10.0.201`

See [global.json](/C:/Dev/codebase/StillOps/global.json).

### Central package management

Package versions are managed centrally in:

- [Directory.Packages.props](/C:/Dev/codebase/StillOps/Directory.Packages.props)

Notable packages in use:

- Aspire hosting and testing packages
- `Microsoft.Extensions.*` hosting and resilience packages
- OpenTelemetry packages
- xUnit test packages

### Operational settings

The web project loads a reloadable JSON settings file:

- [stillops-operational-settings.json](/C:/Dev/codebase/StillOps/src/entrypoints/StillOps.Web/stillops-operational-settings.json)

Current settings include:

- sensor warning thresholds for temperature, humidity, and ABV
- sensor heartbeat timeout seconds
- starter alerting flags for missed heartbeats and threshold exceedance

These settings are bound to `StarterSettings` via `IOptionsMonitor`, so changes can take effect without restarting the web app.

## Authentication and Access

The current web shell uses cookie authentication with a minimal internal-only starter setup.

Current behavior:

- unauthenticated access to protected shell content redirects to `/login`
- authorized users must satisfy the `InternalShell` policy
- the policy requires authenticated users in either `InternalOperator` or `InternalAdmin` roles

The code comments indicate that the current auth setup is a starter implementation intended to be replaced later by OpenIddict-based flows.

## Health and Observability

Shared service defaults wire:

- OpenTelemetry logging
- OpenTelemetry metrics
- OpenTelemetry tracing
- service discovery
- default resilience handlers for `HttpClient`

For web applications using `MapDefaultEndpoints()`, development environments expose:

- `/health`
- `/alive`

The web app also adds a starter `sensor-connectivity` readiness check. It currently reports `Degraded` because no live sensor topology exists yet.

## Running the Solution

Run commands from `C:\Dev\codebase\StillOps`.

### Start the full Aspire topology

```powershell
dotnet run --project .\src\hosting\StillOps.AppHost\
```

This is the primary local entry point because the AppHost orchestrates both the web and ingestion services together.

### Start individual projects

Web shell:

```powershell
dotnet run --project .\src\entrypoints\StillOps.Web\
```

Ingestion worker:

```powershell
dotnet run --project .\src\entrypoints\StillOps.Ingestion\
```

### Build

```powershell
dotnet build .\StillOps.slnx
```

### Test

```powershell
dotnet test .\StillOps.slnx
dotnet test .\tests\integration\StillOps.AppHost.IntegrationTests\
```

The integration tests start the Aspire topology and may require local Aspire runtime support and the environment needed by `Aspire.Hosting.Testing`.

## Development Notes

- Shared conventions and package versions are defined through `Directory.Build.props` and `Directory.Packages.props`.
- `StillOps.Web` is currently a server-rendered interactive Blazor app, not a separate SPA frontend.
- `StillOps.Ingestion` is intentionally lightweight at this stage and serves as a verified hosting placeholder for future ingestion logic.
- `StillOps.ServiceDefaults` should remain the common place for cross-cutting hosting concerns rather than duplicating health/telemetry wiring in each app.

## Recommended Entry Points for Exploration

If you are new to the codebase, start here:

1. [AppHost.cs](/C:/Dev/codebase/StillOps/src/hosting/StillOps.AppHost/AppHost.cs)
2. [Program.cs](/C:/Dev/codebase/StillOps/src/entrypoints/StillOps.Web/Program.cs)
3. [Worker.cs](/C:/Dev/codebase/StillOps/src/entrypoints/StillOps.Ingestion/Worker.cs)
4. [Extensions.cs](/C:/Dev/codebase/StillOps/src/hosting/StillOps.ServiceDefaults/Extensions.cs)
5. [AppHostIntegrationTests.cs](/C:/Dev/codebase/StillOps/tests/integration/StillOps.AppHost.IntegrationTests/AppHostIntegrationTests.cs)
