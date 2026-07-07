# 23 — Solution Skeleton (bộ khung build: CPM, Directory.*.props, csproj, AssemblyMarker)

> **File authoritative cho:** bộ khung solution cụ thể để bắt đầu code — cấu trúc file, `Directory.Build.props`, Central Package Management, tham chiếu csproj (enforce dependency rule **ngay ở tầng project**), AssemblyMarker cho DI, analyzers/warnings-as-errors. Đây là **bước 1** của lộ trình (`00` §Lộ trình).
>
> **No-fabrication:** file này **không hardcode số version** — dùng placeholder + trỏ `technology-stack.md` (đã verify major/LTS) để **pin patch khi implement** (TK-005/018).

## 1. Vì sao khung này (bản chất)

- **Central Package Management (CPM)**: pin version ở **một chỗ** (`Directory.Packages.props`) → mọi project cùng version, không lệch/không trùng version giữa project (nguồn bug khó chịu ở solution nhiều project). Commercial: audit/nâng version một nơi.
- **Directory.Build.props**: đặt thuộc tính chung (target, nullable, analyzers) **một chỗ** → không lặp trong từng csproj, không quên.
- **Dependency rule enforce 2 tầng**: (1) **tĩnh** ở `.csproj` (Domain không `ProjectReference` tới EF/Infrastructure — không compile được nếu vi phạm); (2) **test** NetArchTest (bắt vi phạm tinh vi qua reflection). Hai tầng bổ trợ, không thay thế nhau.

## 2. Cấu trúc file (concrete)

```text
/backend
  ResortQr.sln
  Directory.Build.props           # thuộc tính chung mọi project
  Directory.Packages.props        # CPM: pin version tập trung
  .editorconfig                   # code style + analyzer severity
  global.json                     # pin .NET SDK band (tùy chọn, ổn định CI)
  src/
    ResortQr.SharedKernel/   ResortQr.SharedKernel.csproj   + AssemblyMarker.cs
    ResortQr.Domain/         ResortQr.Domain.csproj         + AssemblyMarker.cs
    ResortQr.Application/    ResortQr.Application.csproj     + AssemblyMarker.cs
    ResortQr.Infrastructure/ ResortQr.Infrastructure.csproj  + AssemblyMarker.cs + AppDbContextFactory.cs
    ResortQr.Api/            ResortQr.Api.csproj             + Program.cs + appsettings*.json
  tests/
    ResortQr.UnitTests/          .csproj
    ResortQr.IntegrationTests/   .csproj
    ResortQr.ArchitectureTests/  .csproj
```

## 3. `Directory.Build.props` (thuộc tính chung)

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>          <!-- LTS, technology-stack.md -->
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>  <!-- strict; nullable/async là lỗi, không cảnh báo -->
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>latest-Recommended</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
  </PropertyGroup>
</Project>
```

> **Lý do `TreatWarningsAsErrors=true` (chính xác):** nullable/async warning (ví dụ `await` bị quên — CS4014, khả năng null — CS86xx) là **bug tiềm ẩn**; để dạng "cảnh báo" sẽ trôi. Coi là lỗi buộc sửa ngay = correctness-by-construction ở tầng biên dịch. Nếu có warning ngoài ý muốn từ generated code, dùng `<WarningsNotAsErrors>` khoanh vùng thay vì tắt toàn bộ.

## 4. `Directory.Packages.props` (Central Package Management)

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
  <ItemGroup>
    <!-- Version = PIN KHI IMPLEMENT theo design/technology-stack.md (đã verify major/LTS); KHÔNG bịa số ở đây -->
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="…10.x…" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design"  Version="…10.x…" />
    <PackageVersion Include="Scrutor" Version="…" />
    <PackageVersion Include="FluentValidation" Version="…" />
    <PackageVersion Include="FluentValidation.DependencyInjectionExtensions" Version="…" />
    <PackageVersion Include="Riok.Mapperly" Version="…" />          <!-- Apache-2.0 -->
    <PackageVersion Include="Serilog.AspNetCore" Version="…" />
    <PackageVersion Include="QRCoder" Version="…" />
    <PackageVersion Include="QuestPDF" Version="…" />               <!-- ⚠️ license theo doanh thu (TK-019) -->
    <PackageVersion Include="Ganss.Xss" Version="…" />
    <PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="…10.x…" />
    <PackageVersion Include="Konscious.Security.Cryptography.Argon2" Version="…" />
    <!-- Test -->
    <PackageVersion Include="xunit" Version="…" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="…" />
    <PackageVersion Include="NSubstitute" Version="…" />
    <PackageVersion Include="FsCheck.Xunit" Version="…" />
    <PackageVersion Include="Testcontainers.PostgreSql" Version="…" />
    <PackageVersion Include="NetArchTest.Rules" Version="…" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="…" />
  </ItemGroup>
</Project>
```

- Trong từng `.csproj` chỉ `<PackageReference Include="X" />` (**không** ghi Version — CPM lo).
- `Microsoft.AspNetCore.App`/`Microsoft.NET.Sdk.Web` là framework reference (không cần pin).

## 5. Tham chiếu csproj (enforce dependency rule TĨNH)

| Project | SDK | ProjectReference (chỉ những cái này) |
|---|---|---|
| `SharedKernel` | Microsoft.NET.Sdk | **(none)** — sạch tuyệt đối (không EF/ASP.NET) |
| `Domain` | Microsoft.NET.Sdk | `SharedKernel` |
| `Application` | Microsoft.NET.Sdk | `Domain`, `SharedKernel` |
| `Infrastructure` | Microsoft.NET.Sdk | `Application`, `Domain`, `SharedKernel` (implement port của Application) |
| `Api` | Microsoft.NET.Sdk.Web | `Application`, `Infrastructure` (composition root) |

- **Domain KHÔNG** reference EF Core/ASP.NET/Infrastructure/Api (Property B1) — không có `ProjectReference` nào cho phép → **vi phạm không compile được**.
- **Application KHÔNG** reference Api/Infrastructure → chỉ khai **port (interface)**; Infrastructure implement.
- `Api` là **chỗ duy nhất** biết cả Application lẫn Infrastructure (ghép DI).
- Package theo project (ví dụ): EF Core/Npgsql/Serilog/QRCoder/QuestPDF/Ganss.Xss/Argon2 ở **Infrastructure**; FluentValidation ở **Application**; JwtBearer/Serilog.AspNetCore ở **Api**; Scrutor ở **Api** (composition) hoặc nơi gọi `AddResortQrModules`.

## 6. AssemblyMarker (cho DI scan — `01` §5)

```csharp
// Mỗi project (Application, Infrastructure) có 1 class rỗng để lấy Assembly tường minh
namespace ResortQr.Application;   public sealed class AssemblyMarker { }
namespace ResortQr.Infrastructure; public sealed class AssemblyMarker { }
```

- `AddResortQrModules` (`01` §5) scan `typeof(Application.AssemblyMarker).Assembly` + `typeof(Infrastructure.AssemblyMarker).Assembly` → **bỏ hẳn `ForceLoadAssembly`** của reference (E6).

## 7. `AppDbContextFactory` (design-time cho migration — TK-023)

```csharp
// Infrastructure: EF tools cần tạo DbContext lúc design-time (add/apply migration)
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDesignTimeDbContext(string[] args)
    {
        var cfg = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables().Build();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(cfg.GetConnectionString("Postgres")).Options;
        return new AppDbContext(options, /* clock/currentUser no-op cho design-time */);
    }
}
```

> Cần vì startup project (`Api`) khác project chứa DbContext (`Infrastructure`); factory giúp `dotnet ef` tạo context không cần chạy cả app.

## 8. `.editorconfig` & analyzers (tóm tắt)
- Bật analyzer .NET (`latest-Recommended`), style enforce trong build.
- Naming rule khớp `06`: interface `I*`, use case `*UseCase`, validator `*Validator`.
- Severity: nullable/async = error (qua TreatWarningsAsErrors); style = warning/suggestion tùy nhóm.

## 9. Kiểm chứng khung (Definition of Done bước 1)
- `dotnet build` xanh với `TreatWarningsAsErrors=true`.
- `ArchitectureTests` xanh: dependency rule + SharedKernel sạch + naming (`06`, `17` B1).
- `dotnet ef migrations add Foundation_Init` chạy được (nhờ `AppDbContextFactory`).
- CPM: `dotnet list package` cho thấy version thống nhất, không cảnh báo version lệch.

## 10. Truy vết
- **Validates: Requirements 1.1, 1.2, 1.3, 1.6 (cấu trúc), 2 (DI), 19.6-19.7 (architecture test)**
- Align: `01` (kiến trúc), `06` (conventions), `technology-stack` (version), `07`/`17` (test).
