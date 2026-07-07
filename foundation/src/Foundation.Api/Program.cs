using Foundation.Api;
using Foundation.Api.Endpoints;
using Foundation.Api.Observability;

var builder = WebApplication.CreateBuilder(args);

// Nền: Options (validate-on-start) + DI convention (Scrutor) + Auth/AuthZ.
// LƯU Ý: app cụ thể PHẢI bổ sung EF DbContext + impl IUserAuthStore/IRefreshTokenStore/IUnitOfWork
// của mình (base để trống — cần DB). Thiếu → DI báo lỗi khi khởi động (fail-fast, đúng ý đồ).
builder.Services.AddFoundation(builder.Configuration);

var app = builder.Build();

app.UseFoundation();
app.MapFoundationHealthChecks();
app.MapFoundationAuthEndpoints();

app.Run();

// Cho phép WebApplicationFactory<Program> trong integration test.
public partial class Program;
