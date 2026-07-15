using Bedrock.Application.Ports.Persistence;
using Faq.Domain;

namespace Faq.Application;

/// <summary>
/// Kiểm bất biến CÂY khi gán <c>ParentId</c> cho một <see cref="FaqItem"/> (Req 4.1 "cha-con" — spec KHÔNG nói ràng
/// buộc; AI tự ra để chống đồ thị vòng gây render loop phía guest). Ba luật: (1) parent phải TỒN TẠI; (2) parent
/// CÙNG <c>CategoryId</c>; (3) không tạo CHU TRÌNH (parent không phải hậu duệ của chính item — chỉ áp cho Update,
/// vì item mới tạo chưa thể nằm trong chuỗi tổ tiên). Walk tổ tiên bằng <see cref="IRepository{T}.FindByIdAsync"/>
/// lặp (F9 — không IQueryable), dữ liệu nhỏ (~chục item/resort) nên O(depth) chấp nhận; chặn vòng lặp vô hạn bằng
/// giới hạn độ sâu (phòng dữ liệu hỏng từ trước, dù DB by-construction không có cycle).
/// </summary>
internal static class FaqItemParentValidator
{
    private const int MaxDepth = 1000;

    /// <summary>
    /// Trả <c>null</c> nếu <paramref name="parentId"/> hợp lệ cho item thuộc <paramref name="categoryId"/>;
    /// ngược lại trả lý do (không hợp lệ). <paramref name="selfId"/> = item đang sửa (Update) để phát hiện chu trình;
    /// <c>null</c> khi Create (item chưa tồn tại → không thể tạo cycle).
    /// </summary>
    public static async Task<bool> IsValidParentAsync(
        IRepository<FaqItem> items,
        Guid categoryId,
        Guid? parentId,
        Guid? selfId,
        CancellationToken ct)
    {
        if (parentId is null)
        {
            return true; // item gốc — hợp lệ.
        }

        if (selfId is not null && parentId.Value == selfId.Value)
        {
            return false; // tự trỏ mình.
        }

        var parent = await items.FindByIdAsync(parentId.Value, ct).ConfigureAwait(false);
        if (parent is null || parent.CategoryId != categoryId)
        {
            return false; // parent không tồn tại hoặc khác danh mục.
        }

        // Chu trình chỉ xảy ra khi Update (selfId != null): nếu selfId là tổ tiên của parent → gán parent sẽ tạo vòng.
        if (selfId is null)
        {
            return true;
        }

        var cursor = parent;
        var depth = 0;
        while (cursor is not null)
        {
            if (cursor.Id == selfId.Value)
            {
                return false; // parent nằm dưới selfId trong cây → tạo chu trình.
            }

            if (cursor.ParentId is null || ++depth > MaxDepth)
            {
                break;
            }

            cursor = await items.FindByIdAsync(cursor.ParentId.Value, ct).ConfigureAwait(false);
        }

        return true;
    }
}
