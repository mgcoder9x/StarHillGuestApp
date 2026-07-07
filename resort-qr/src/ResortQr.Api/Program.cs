using System.Text.Json.Serialization;
using ResortQr.Api;
using ResortQr.Api.Endpoints;
using ResortQr.Api.Observability;
using ResortQr.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Nền: Options (validate-on-start) + DI convention (Scrutor) + Auth/AuthZ.
builder.Services.AddResortQr(builder.Configuration);

// Enum serialize dạng STRING trong JSON (hợp đồng API rõ ràng, vd RoomStatus="Active"), không phải số.
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Cấu hình cookie phiên khách (mặc định an toàn nếu thiếu section "Guest").
builder.Services.Configure<ResortQr.Api.GuestAccess.GuestOptions>(
    builder.Configuration.GetSection(ResortQr.Api.GuestAccess.GuestOptions.SectionName));

// Persistence app: đăng ký AppDbContext + tầng nền KHI có connection string.
// - Có connString → wire DB (Npgsql + snake_case) + seeder.
// - Không có + Production → từ chối khởi động (fail-fast, README §Fail-fast).
// - Không có + Dev/Test → bỏ qua (integration test cấp store in-memory, không cần Postgres).
var connectionString = builder.Configuration.GetConnectionString("Postgres");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddResortQrDatabase(connectionString);
    builder.Services.AddScoped<ResortSeeder>();
}
else if (builder.Environment.IsProduction())
{
    throw new InvalidOperationException(
        "Thiếu ConnectionStrings:Postgres — Production bắt buộc phải cấu hình kết nối database.");
}

var app = builder.Build();

app.UseResortQr();
app.MapResortQrHealthChecks();
app.MapResortQrAuthEndpoints();
app.MapResortQrAdminRoomEndpoints();
app.MapResortQrGuestEndpoints();

// Migrate + seed opt-in (single-instance deployment). Mặc định TẮT → không mở kết nối lúc khởi động.
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await MigrateAndSeedAsync(app);
}

app.Run();

static async Task MigrateAndSeedAsync(WebApplication app)
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    var adminEmail = app.Configuration["Seed:AdminEmail"];
    var adminPassword = app.Configuration["Seed:AdminPassword"];
    if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
    {
        var seeder = scope.ServiceProvider.GetRequiredService<ResortSeeder>();
        await seeder.SeedAsync(adminEmail, adminPassword);
    }
}

// Cho phép WebApplicationFactory<Program> trong integration test.
public partial class Program;
