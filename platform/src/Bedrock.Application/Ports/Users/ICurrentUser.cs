namespace Bedrock.Application.Ports.Users;

/// <summary>
/// Ngữ cảnh người dùng hiện tại (từ JWT/claims). Dùng cho audit (actor) + authorization.
/// Trả null/empty khi request ẩn danh — KHÔNG ném. Mở rộng cho hệ lớn (F23): permission-based authz,
/// multi-tenant, session tracking — KHÔNG chỉ role. Base cung cấp CƠ CHẾ; role/permission cụ thể do
/// app/module khai (F3 — lõi không hardcode Admin/Staff).
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    bool IsAuthenticated { get; }

    /// <summary>Vai trò (nguồn suy ra permission). App tự định nghĩa tập role.</summary>
    IReadOnlyCollection<string> Roles { get; }

    /// <summary>Quyền chi tiết (permission-based authz — ưu tiên hơn role ở hệ lớn — F23).</summary>
    IReadOnlyCollection<string> Permissions { get; }

    /// <summary>Tenant hiện tại (multi-tenant — F23). Null nếu single-tenant/không áp dụng.</summary>
    Guid? TenantId { get; }

    /// <summary>Định danh phiên (audit/revoke theo session — F23). Null nếu không áp dụng.</summary>
    Guid? SessionId { get; }

    bool IsInRole(string role);

    bool HasPermission(string permission);
}
