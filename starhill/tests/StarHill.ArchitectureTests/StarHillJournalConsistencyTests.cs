using System.Globalization;
using System.Text.RegularExpressions;
using Xunit;

namespace StarHill.ArchitectureTests;

/// <summary>
/// ANTI-DRIFT pha QR — bản sao cơ chế đã chứng minh ở base (Bedrock JournalConsistencyTests / AD-030), NHƯNG
/// nhắm DECISION JOURNAL của pha <c>starhill-qr</c> (<c>.kiro/specs/starhill-qr/journal</c>) với tiền tố ID
/// <c>QR-</c>. Trước đó journal QR chỉ được review THỦ CÔNG (QR-N-003/N-004) → lỗ hổng drift: base copy vẫn
/// chạy JournalConsistencyTests nhắm journal BASE, KHÔNG bảo vệ journal QR. Test này biến kỷ luật journal QR
/// thành BUILD GATE: lệch = FAIL BUILD.
/// <para>
/// Enforce 5 bất biến (mirror INV-1..5 của base, đổi sang tiền tố QR-):
/// INV-1 ID mỗi loại (QR-AD/DV/TO/N) duy nhất + liên tục 1..N; INV-2 (keystone) mọi QR-AD xuất hiện trong
/// 05-anti-drift.md; INV-3 không tham chiếu QR-*-### dangling; INV-4 QR-AD & QR-DV có Status + Provenance;
/// INV-5 tham chiếu CP## nằm trong 1..15 (QR có đúng 15 Correctness Property — design.md).
/// </para>
/// <para>
/// KHÔNG skip khi không thấy journal (<see cref="RequireJournalDir"/> gọi <c>Assert.Fail</c>): cổng anti-drift
/// không được tự tắt âm thầm — không tìm thấy = layout repo governance đã vỡ = drift thật cần chặn.
/// </para>
/// </summary>
public sealed class StarHillJournalConsistencyTests
{
    private static readonly Regex HeadingRegex =
        new(@"^### QR-(AD|DV|TO|N)-(\d+)\b", RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex ReferenceRegex =
        new(@"\bQR-(AD|DV|TO|N)-(\d+)\b", RegexOptions.Compiled);

    private static readonly Regex CorrectnessPropertyRegex =
        new(@"\bCP(\d+)\b", RegexOptions.Compiled);

    private static readonly Regex AutonomousDecisionTokenRegex =
        new(@"\bQR-AD-(\d+)\b", RegexOptions.Compiled);

    private static readonly Regex SectionHeadRegex =
        new(@"^QR-(AD|DV|TO|N)-(\d+)", RegexOptions.Compiled);

    private const string JournalSpec = "starhill-qr";
    private const int CorrectnessPropertyCount = 15;

    // (prefix, file) cho từng loại bản ghi.
    private static readonly (string Prefix, string File)[] RecordFiles =
    [
        ("AD", "01-decisions.md"),
        ("DV", "02-deviations.md"),
        ("TO", "03-tradeoffs.md"),
        ("N", "04-notes.md"),
    ];

    // Bản ghi QR-AD & QR-DV bắt buộc đủ trường schema (TO/N schema nhẹ hơn — mirror base).
    private static readonly (string Prefix, string File)[] SchemaCheckedFiles =
    [
        ("AD", "01-decisions.md"),
        ("DV", "02-deviations.md"),
    ];

    // Mọi file có thể chứa cross-reference cần kiểm (gồm 05 anti-drift).
    private static readonly string[] CrossReferenceFiles =
    [
        "01-decisions.md",
        "02-deviations.md",
        "03-tradeoffs.md",
        "04-notes.md",
        "05-anti-drift.md",
    ];

    // INV-1: ID mỗi loại (QR-AD/DV/TO/N) DUY NHẤT + LIÊN TỤC 1..N.
    [Fact]
    public void INV1_record_ids_are_unique_and_contiguous()
    {
        var journal = RequireJournalDir();

        foreach (var (prefix, file) in RecordFiles)
        {
            var ids = HeadingIds(File.ReadAllText(Path.Combine(journal, file)), prefix);

            Assert.True(ids.Count > 0, $"{file}: không tìm thấy bản ghi QR-{prefix}-### nào.");

            var duplicates = ids.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).OrderBy(x => x).ToList();
            Assert.True(
                duplicates.Count == 0,
                $"{file}: ID QR-{prefix} bị TRÙNG: {FormatIds(prefix, duplicates)}.");

            var max = ids.Max();
            var missing = Enumerable.Range(1, max).Except(ids).OrderBy(x => x).ToList();
            Assert.True(
                missing.Count == 0,
                $"{file}: ID QR-{prefix} KHÔNG liên tục (thiếu trong 1..{max}): {FormatIds(prefix, missing)}.");
        }
    }

    // INV-2 (KEYSTONE tự động): mọi QR-AD trong 01 PHẢI xuất hiện trong 05-anti-drift.md.
    [Fact]
    public void INV2_every_decision_appears_in_antidrift_guard_map()
    {
        var journal = RequireJournalDir();

        var definedDecisions = HeadingIds(File.ReadAllText(Path.Combine(journal, "01-decisions.md")), "AD").ToHashSet();

        var antiDriftText = File.ReadAllText(Path.Combine(journal, "05-anti-drift.md"));
        var guardedDecisions = AutonomousDecisionTokenRegex.Matches(antiDriftText)
            .Select(m => ParseInt(m.Groups[1].Value))
            .ToHashSet();

        var missing = definedDecisions.Except(guardedDecisions).OrderBy(x => x).ToList();
        Assert.True(
            missing.Count == 0,
            "KEYSTONE: các QR-AD sau thiếu bản ghi guard trong 05-anti-drift.md "
            + $"(mỗi quyết định phải khai trạng thái guard): {FormatIds("AD", missing)}.");
    }

    // INV-3: mọi tham chiếu QR-AD/DV/TO/N-### PHẢI trỏ tới bản ghi CÓ THẬT (chống dangling).
    [Fact]
    public void INV3_no_dangling_references()
    {
        var journal = RequireJournalDir();

        var defined = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);
        foreach (var (prefix, file) in RecordFiles)
        {
            defined[prefix] = HeadingIds(File.ReadAllText(Path.Combine(journal, file)), prefix).ToHashSet();
        }

        var dangling = new List<string>();
        foreach (var file in CrossReferenceFiles)
        {
            var text = File.ReadAllText(Path.Combine(journal, file));
            foreach (Match match in ReferenceRegex.Matches(text))
            {
                var prefix = match.Groups[1].Value;
                var id = ParseInt(match.Groups[2].Value);
                if (!defined[prefix].Contains(id))
                {
                    dangling.Add($"{file}: QR-{prefix}-{id:D3}");
                }
            }
        }

        Assert.True(
            dangling.Count == 0,
            $"Tham chiếu tới bản ghi KHÔNG tồn tại (dangling): {string.Join("; ", dangling.Distinct())}.");
    }

    // INV-4: mỗi bản ghi QR-AD & QR-DV có đủ 'Status:' + 'Provenance/Evidence:' (chống bịa).
    [Fact]
    public void INV4_decisions_and_deviations_have_status_and_provenance()
    {
        var journal = RequireJournalDir();

        var offenders = new List<string>();
        foreach (var (prefix, file) in SchemaCheckedFiles)
        {
            var text = File.ReadAllText(Path.Combine(journal, file));
            foreach (var (id, body) in RecordSections(text, prefix))
            {
                if (!body.Contains("- Status:", StringComparison.Ordinal))
                {
                    offenders.Add($"{id}: thiếu 'Status:'");
                }

                if (!body.Contains("Provenance/Evidence:", StringComparison.Ordinal))
                {
                    offenders.Add($"{id}: thiếu 'Provenance/Evidence:'");
                }
            }
        }

        Assert.True(offenders.Count == 0, $"Bản ghi thiếu trường bắt buộc: {string.Join("; ", offenders)}.");
    }

    // INV-5: mọi tham chiếu CP## trong journal QR nằm trong 1..15 (tập đóng — 15 Correctness Property).
    [Fact]
    public void INV5_correctness_property_references_within_range()
    {
        var journal = RequireJournalDir();

        var offenders = new List<string>();
        foreach (var file in CrossReferenceFiles)
        {
            var text = File.ReadAllText(Path.Combine(journal, file));
            foreach (Match match in CorrectnessPropertyRegex.Matches(text))
            {
                var id = ParseInt(match.Groups[1].Value);
                if (id is < 1 or > CorrectnessPropertyCount)
                {
                    offenders.Add($"{file}: CP{id}");
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            $"Tham chiếu CP ngoài phạm vi 1..{CorrectnessPropertyCount}: {string.Join("; ", offenders.Distinct())}.");
    }

    private static List<int> HeadingIds(string text, string prefix) =>
        HeadingRegex.Matches(text)
            .Where(m => string.Equals(m.Groups[1].Value, prefix, StringComparison.Ordinal))
            .Select(m => ParseInt(m.Groups[2].Value))
            .ToList();

    private static IEnumerable<(string Id, string Body)> RecordSections(string text, string prefix)
    {
        foreach (var chunk in text.Split("\n### "))
        {
            var head = SectionHeadRegex.Match(chunk);
            if (head.Success && string.Equals(head.Groups[1].Value, prefix, StringComparison.Ordinal))
            {
                yield return ($"QR-{prefix}-{ParseInt(head.Groups[2].Value):D3}", chunk);
            }
        }
    }

    private static int ParseInt(string value) => int.Parse(value, CultureInfo.InvariantCulture);

    private static string FormatIds(string prefix, IEnumerable<int> ids) =>
        string.Join(", ", ids.Select(x => $"QR-{prefix}-{x:D3}"));

    private static string RequireJournalDir()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, ".kiro", "specs", JournalSpec, "journal");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        // FAIL rõ ràng (không skip): cổng anti-drift QR KHÔNG được tự tắt âm thầm.
        Assert.Fail(
            $"Không tìm thấy .kiro/specs/{JournalSpec}/journal khi đi lên từ "
            + $"'{AppContext.BaseDirectory}'. Cổng journal-consistency pha QR không thể chạy — "
            + "kiểm tra lại layout repo (journal phải ở tổ tiên của thư mục test).");
        return null!; // unreachable — Assert.Fail ném XunitException.
    }
}
