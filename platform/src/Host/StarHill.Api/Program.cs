using Bedrock.Api;
using Bedrock.Application.DependencyInjection;
using Bedrock.Infrastructure.DependencyInjection;
using Identity.Api.DependencyInjection;
using Identity.Infrastructure.DependencyInjection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Fail-fast DI MỌI môi trường (I9/F7, design §9.4): validate scope + build ngay lúc Build().
builder.Host.UseDefaultServiceProvider((_, options) =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

var services = builder.Services;
var configuration = builder.Configuration;

// (1) Cơ chế Bedrock: Api (auth verify + HTTP hardening + routing + health), Security (Argon2/JWT sign + port bắt buộc),
//     startup validator (RequiredPortsValidator chặn boot nếu thiếu port bắt buộc).
services.AddBedrockApi(configuration);
services.AddBedrockSecurity(configuration);
services.AddBedrockStartupValidation();

// Posture DEFAULT AN TOÀN cho MỌI extension port (design §5.5/§6.1/§13 — "AddXxxCore luôn gọi"): mỗi port có
// default degrade (NullAppCache) hoặc fail-loud (Throwing*). BẮT BUỘC vì pipeline behaviors (§8) phụ thuộc port
// default: Idempotency behavior inject IIdempotencyStore → thiếu AddCacheCore = KHÔNG resolve được use case đã
// decorate (N-042). Adapter thật (task 14) OVERRIDE bằng Replace; bỏ adapter = quay về default.
services.AddMessagingCore();
services.AddCacheCore();
services.AddEmailCore();
services.AddSearchCore();
services.AddStorageCore();
services.AddExternalAuthCore();

// (2) Modules — composition tách nửa-Infra + nửa-Api (DV-013). Host CHỌN provider (Npgsql) + connection string.
var identityConnectionString = configuration.GetConnectionString("Identity")
    ?? throw new InvalidOperationException(
        "Thiếu ConnectionStrings:Identity — fail-fast (F35). Cấu hình connection string cho module Identity.");
services.AddIdentityInfrastructure(options => options.UseNpgsql(identityConnectionString));
services.AddIdentityApi();

// (3) Bọc pipeline behaviors SAU khi use case đã đăng ký (AD-037 — Scrutor chỉ decorate service đã có mặt).
services.AddBedrockCore();

// (4) Duplicate-guard port single-impl (F18) — chạy trước Build (báo GỘP mọi vi phạm).
services.ValidateSingleImplementationPorts();

var app = builder.Build();

// Slot #4 (HSTS/HTTPS-redirect) = TRÁCH NHIỆM HOST (AD-035). Sample host này chạy sau reverse-proxy terminate TLS
// (design §3.5) nên KHÔNG tự redirect; deployment thật bật UseHsts/UseHttpsRedirection khi cần.
app.UseBedrockApi();

await app.RunAsync().ConfigureAwait(false);

/// <summary>Lộ entry point cho <c>WebApplicationFactory&lt;Program&gt;</c> (smoke test boot). Không dùng runtime.</summary>
public partial class Program;
