namespace Bedrock.Application.Ports.Search;

/// <summary>
/// Yêu cầu tìm kiếm (contract-first, F26). Read-model/search projection — KHÔNG thay repository chính (DB vẫn
/// source-of-truth, eventual consistency). Adapter (vd Elasticsearch) impl ở <c>Adapters.*</c>.
/// </summary>
public sealed record SearchRequest(
    string Query,
    int Page = 1,
    int PageSize = 20,
    IReadOnlyDictionary<string, string>? Filters = null);

/// <summary>Kết quả tìm kiếm: danh sách hit của trang + tổng số khớp (phục vụ phân trang).</summary>
public sealed record SearchResult<TDoc>(IReadOnlyList<TDoc> Hits, long Total);

/// <summary>Ghi/xoá tài liệu vào index tìm kiếm (mỗi <typeparamref name="TDoc"/> thuộc một module — ownership F26).</summary>
public interface ISearchIndex<TDoc>
{
    Task IndexAsync(TDoc document, CancellationToken ct = default);

    Task DeleteAsync(string id, CancellationToken ct = default);
}

/// <summary>Truy vấn index tìm kiếm (tách khỏi <see cref="ISearchIndex{TDoc}"/> — CQRS-lite read side).</summary>
public interface ISearchQuery<TDoc>
{
    Task<SearchResult<TDoc>> SearchAsync(SearchRequest request, CancellationToken ct = default);
}
