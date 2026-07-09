namespace Bedrock.Application.Ports.Storage;

/// <summary>Blob file để lưu trữ (contract-first, F28). <see cref="Content"/> là stream đọc được lúc lưu.</summary>
public sealed record FileBlob(string FileName, string ContentType, Stream Content);

/// <summary>
/// Lưu trữ file/blob (adapter S3/Azure Blob/đĩa ở <c>Adapters.*</c>). <see cref="SaveAsync"/> trả về key để
/// đọc lại sau. Default khi chưa có adapter = fail-loud (mất file âm thầm nguy hiểm — §5.5/R16.4).
/// </summary>
public interface IFileStorage
{
    Task<string> SaveAsync(FileBlob blob, CancellationToken ct = default);

    Task<Stream> OpenAsync(string key, CancellationToken ct = default);
}
