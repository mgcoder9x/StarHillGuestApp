using ResortQr.SharedKernel.DependencyInjection;

namespace ResortQr.Application.Abstractions;

/// <summary>
/// Ngữ cảnh người dùng hiện tại (từ JWT/claims). Dùng cho audit (actor) + authorization.
/// Trả null khi không có ngữ cảnh đăng nhập (vd request ẩn danh) — KHÔNG ném.
/// </summary>
public interface ICurrentUser : IScopedService
{
    Guid? UserId { get; }

    bool IsAuthenticated { get; }

    IReadOnlyCollection<string> Roles { get; }

    bool IsInRole(string role);
}
