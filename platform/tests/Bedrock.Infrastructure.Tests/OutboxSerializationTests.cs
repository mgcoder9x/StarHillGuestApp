using System.Text.Json;
using System.Text.Json.Serialization;
using Bedrock.Infrastructure.Persistence.Messaging;

namespace Bedrock.Infrastructure.Tests;

/// <summary>
/// Task 17 (F32/§9.1) — khoá HÀNH VI tolerant-reader của hợp đồng serialization Outbox (AD-015). Bản chất
/// versioning event: producer thêm field optional (backward-compat) thì consumer CŨ vẫn deserialize được
/// (bỏ qua field lạ) — R22.2/R22.3. Test dùng CHÍNH <see cref="OutboxSerialization.Options"/> (internal, qua
/// InternalsVisibleTo) → nếu ai đó đổi options phá tolerant (vd đặt Disallow) thì FAIL BUILD (guard drift).
/// </summary>
public sealed class OutboxSerializationTests
{
    private sealed record EventV1(string Name, int Count);

    [Fact]
    public void Options_tolerant_reader_ignores_unknown_members()
    {
        // Payload "v2" (producer mới) có field optional lạ + object lồng lạ; consumer "v1" phải đọc được.
        const string v2Json =
            """{"name":"x","count":3,"addedInV2":"future","nestedFuture":{"a":1,"b":[1,2]}}""";

        var v1 = JsonSerializer.Deserialize<EventV1>(v2Json, OutboxSerialization.Options);

        Assert.NotNull(v1);
        Assert.Equal("x", v1!.Name);
        Assert.Equal(3, v1.Count);
    }

    [Fact]
    public void Options_do_not_disallow_unmapped_members()
    {
        // Guard cấu hình: Disallow sẽ ném khi gặp field lạ → phá tolerant-reader. Phải KHÁC Disallow.
        Assert.NotEqual(JsonUnmappedMemberHandling.Disallow, OutboxSerialization.Options.UnmappedMemberHandling);
    }
}
