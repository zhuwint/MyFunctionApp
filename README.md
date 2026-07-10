# MyFunctionApp

A demo project showing how to refactor an Azure Functions project into a cross-platform containerized HTTP server.

---

## Project Structure

```
MyFunctionApp/
├── src/
│   ├── FunctionApp/       # Azure Functions (dotnet-isolated)
│   └── WebApp/            # ASP.NET Core Minimal API (container-ready)
└── README.md
```

---

## Design Philosophy

Both projects share identical business logic (`GreetingService`) while using different framework stacks, achieving **complete decoupling of business code from framework code**.

### Layered Architecture

```
┌─────────────────────────────────────────┐
│            Framework Layer               │
│  FunctionApp: Azure Functions Worker     │
│  WebApp:     ASP.NET Core Minimal API    │
├─────────────────────────────────────────┤
│           Interface Layer                │
│         IGreetingService                │
├─────────────────────────────────────────┤
│           Business Layer                 │
│         GreetingService                 │
└─────────────────────────────────────────┘
```

### Dependency Injection

- **FunctionApp**: `HostBuilder.ConfigureServices()`, constructor injection
- **WebApp**: `WebApplicationBuilder.Services`, lambda parameter injection

### Each Project Has Only 2 Source Files

| Layer | File |
|---|---|
| Framework | `Program.cs` — startup, DI, HTTP trigger / route mapping |
| Business | `GreetingService.cs` — logging + building response body (identical across both projects) |

---

## src/FunctionApp — Azure Functions

### Tech Stack

| Component | Choice |
|---|---|
| Runtime | .NET 8 Isolated Worker |
| Functions Version | v4 |
| Trigger | HTTP Trigger |
| NuGet Packages | `Microsoft.Azure.Functions.Worker` / `Worker.Sdk` / `Worker.Extensions.Http` |

### Key Files

| File | Responsibility |
|---|---|
| `MyFunctionApp.csproj` | Project config, references Functions SDK |
| `Program.cs` | Startup + HTTP trigger + DI registration |
| `GreetingService.cs` | Business logic (interface + implementation, identical to WebApp) |
| `host.json` | Functions runtime config |
| `local.settings.json` | Local dev settings |

### Request/Response

```
GET/POST https://myfuncdemoapp.azurewebsites.net/api/satdemofunc/{username}
Response: {"message": "hello {username}"}
```

### Deploy

```bash
# 1. Create storage account (first-time only)
az storage account create \
  --name myfuncdemo \
  --resource-group myfuncdemo \
  --location eastasia \
  --sku Standard_LRS

# 2. Create Function App (first-time only)
az functionapp create \
  --resource-group myfuncdemo \
  --consumption-plan-location eastasia \
  --runtime dotnet-isolated \
  --runtime-version 8.0 \
  --functions-version 4 \
  --name MyFuncDemoApp \
  --storage-account myfuncdemo

# 3. Publish
cd src/FunctionApp
func azure functionapp publish MyFuncDemoApp --dotnet-isolated
```

---

## src/WebApp — ASP.NET Core HTTP Server

### Tech Stack

| Component | Choice |
|---|---|
| Runtime | .NET 8 ASP.NET Core |
| API Style | Minimal API |
| Container | Docker (multi-stage build) |
| Port | 8080 |

### Key Files

| File | Responsibility |
|---|---|
| `MyFunctionApp.csproj` | Project config, references `Microsoft.NET.Sdk.Web` |
| `Program.cs` | Startup + route mapping + DI registration |
| `GreetingService.cs` | Business logic (interface + implementation, identical to FunctionApp) |
| `Dockerfile` | Multi-stage Docker build |
| `.dockerignore` | Docker ignore rules |

### Request/Response

```
GET/POST http://localhost:8080/api/satdemofunc/{username}
Response: {"message": "hello {username}"}
```

### Run Locally

```bash
cd src/WebApp
dotnet run
```

### Docker Build & Run

```bash
cd src/WebApp
docker build -t myfunctionapp .
docker run -p 8080:8080 myfunctionapp
```

---

## Migrating from FunctionApp to WebApp

### Migration Steps

#### 1. .csproj — Switch SDK

```diff
- <Project Sdk="Microsoft.NET.Sdk">
-   ...
-   <AzureFunctionsVersion>v4</AzureFunctionsVersion>
-   <PackageReference Include="Microsoft.Azure.Functions.Worker" ... />
-   <PackageReference Include="Microsoft.Azure.Functions.Worker.Sdk" ... />
-   <PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Http" ... />

+ <Project Sdk="Microsoft.NET.Sdk.Web">
+   <!-- No extra NuGet packages needed -->
```

Switch SDK to `Microsoft.NET.Sdk.Web` and remove all Functions packages. ASP.NET Core HTTP capabilities are built into the SDK.

#### 2. Program.cs — Switch Hosting Model

```diff
- var host = new HostBuilder()
-     .ConfigureFunctionsWorkerDefaults()
-     .ConfigureServices(services =>
-     {
-         services.AddSingleton<IGreetingService, GreetingService>();
-     })
-     .Build();
- host.Run();

+ var builder = WebApplication.CreateBuilder(args);
+ builder.Services.AddSingleton<IGreetingService, GreetingService>();
+ var app = builder.Build();
+ app.MapMethods("/api/satdemofunc/{username}", ["GET", "POST"],
+     (string username, IGreetingService greetingService) =>
+         Results.Json(greetingService.Handle(username)));
+ app.Run();
```

Replace `HostBuilder` + `ConfigureFunctionsWorkerDefaults()` with `WebApplication.CreateBuilder()`. Inline route registration replaces function discovery.

#### 3. Function Handler → Endpoint Route

```diff
- [Function("SATDemoFunc")]
- public HttpResponseData Run(
-     [HttpTrigger(..., Route = "satdemofunc/{username}")] HttpRequestData req,
-     string username)
- {
-     var result = _greetingService.Handle(username);
-     var response = req.CreateResponse(HttpStatusCode.OK);
-     response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
-     response.WriteString(JsonSerializer.Serialize(result));
-     return response;
- }

+ app.MapMethods("/api/satdemofunc/{username}", ["GET", "POST"],
+     (string username, IGreetingService greetingService) =>
+         Results.Json(greetingService.Handle(username)));
```

Key differences:

| Azure Functions | ASP.NET Core |
|---|---|
| `[Function("...")]` attribute | `app.MapMethods()` explicit route |
| `HttpRequestData` + `HttpResponseData` | `Results.Json()` |
| Route parameter bound via method parameter | Route parameter injected via lambda |
| Auto-discovered by framework | Manually registered route |

#### 4. Business Code — No Changes Needed

`GreetingService.cs` (interface + implementation) remains identical. Just copy it over.

#### 5. Remove Functions-Specific Files

```
Remove: host.json, local.settings.json
```

#### 6. Add Docker Support

Add `Dockerfile` and `.dockerignore` for cross-platform container deployment.

---

### Migration Checklist

- [ ] .csproj: `Microsoft.NET.Sdk.Web`, remove Functions NuGet packages
- [ ] Program.cs: `WebApplication.CreateBuilder()` + explicit route registration
- [ ] HTTP: `HttpRequestData`/`HttpResponseData` → `Results.Json()`
- [ ] Route: `[HttpTrigger(..., Route = "...")]` → `MapMethods("/api/...", ...)`
- [ ] Remove: `host.json`, `local.settings.json`
- [ ] Add: `Dockerfile`, `.dockerignore`
- [ ] Business code: **no changes needed**
