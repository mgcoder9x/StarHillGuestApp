using System.Globalization;
using System.Text.RegularExpressions;
using Xunit;

namespace Bedrock.ArchitectureTests;

/// <summary>
/// ANTI-DRIFT layer L4 (AD-030): biến kỷ luật của DECISION JOURNAL thành BUILD GATE. Trước đây việc giữ
/// journal nhất quán (ID không trùng/thiếu, mọi quyết định có guard, ref không dangling, có bằng chứng) là
/// quy trình THỦ CÔNG — dựa trí nhớ. Test này đọc chính các file markdown journal và enforce 5 bất biến;
/// lệch = FAIL BUILD (đúng tinh thần "con người quên, build thì không", nay áp cho cả tài liệu).
/// <para>
/// Journal là governance-side (nằm ở <c>.kiro/</c>, KHÔNG ship theo product). Test project cũng là dev-time
/// (không đóng gói vào sản phẩm) nên luôn chạy trong repo governance nơi <c>.kiro</c> tồn tại. Nếu KHÔNG tìm
/// thấy journal → <see cref="RequireJournalDir"/> gọi <c>Assert.Fail</c> (KHÔNG skip): cổng anti-drift không
/// được tự tắt âm thầm — không tìm thấy nghĩa là layout repo đã vỡ = drift cần chặn.
/// </para>
/// </summary>
public sealed class JournalConsistencyTests
{
    private static readonly Regex HeadingRegex =
        new(@"^### (AD|DV|TO|N)-(\d+)\b", RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex ReferenceRegex =
        new(@"\b(AD|DV|TO|N)-(\d+)\b", RegexOptions.Compiled);

    private static readonly Regex CorrectnessPropertyRegex =
        new(@"\bCP(\d+)\b", RegexOptions.Compiled);

    private static readonly Regex AutonomousDecisionTokenRegex =
        new(@"\bAD-(\d+)\b", RegexOptions.Compiled);

    private static readonly Regex SectionHeadRegex =
        new(@"^(AD|DV|TO|N)-(\d+)", RegexOptions.Compiled);

    // (prefix, file) cho từng loại bản ghi.
    private static readonly (string Prefix, string File)[] RecordFiles =
    [
        ("AD", "01-decisions.md"),
        ("DV", "02-deviations.md"),
        ("TO", "03-tradeoffs.md"),
        ("N", "04-notes.md"),
    ];

    // Bản ghi AD & DV bắt buộc đủ trường schema (TO/N dùng schema nhẹ hơn — xem README journal).
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

    // INV-1: ID mỗi loại (AD/DV/TO/N) DUY NHẤT + LIÊN TỤC 1..N (phát hiện mất/nhân đôi/đánh số lại).
    [Fact]
    public void INV1_record_ids_are_unique_and_contiguous()
    {
        var journal = RequireJournalDir();

        foreach (var (prefix, file) in RecordFiles)
        {
            var ids = HeadingIds(File.ReadAllText(Path.Combine(journal, file)), prefix);

            Assert.True(ids.Count > 0, $"{file}: không tìm thấy bản ghi {prefix}-### nào.");

            var duplicates = ids.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).OrderBy(x => x).ToList();
            Assert.True(
                duplicates.Count == 0,
                $"{file}: ID {prefix} bị TRÙNG: {FormatIds(prefix, duplicates)}.");

            var max = ids.Max();
            var missing = Enumerable.Range(1, max).Except(ids).OrderBy(x => x).ToList();
            Assert.True(
                missing.Count == 0,
                $"{file}: ID {prefix} KHÔNG liên tục (thiếu trong 1..{max}): {FormatIds(prefix, missing)}.");
        }
    }

    // INV-2 (KEYSTONE RULE tự động): mọi AD trong 01 PHẢI xuất hiện trong bảng guard 05-anti-drift.md.
    // → không thể thêm quyết định mà quên khai trạng thái guard.
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
            "KEYSTONE RULE: các AD sau thiếu bản ghi guard trong 05-anti-drift.md "
            + $"(mỗi quyết định phải khai trạng thái guard): {FormatIds("AD", missing)}.");
    }

    // INV-3: mọi tham chiếu AD/DV/TO/N-### trong journal PHẢI trỏ tới một bản ghi CÓ THẬT (chống dangling).
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
                    dangling.Add($"{file}: {prefix}-{id:D3}");
                }
            }
        }

        Assert.True(
            dangling.Count == 0,
            $"Tham chiếu tới bản ghi KHÔNG tồn tại (dangling): {string.Join("; ", dangling.Distinct())}.");
    }

    // INV-4: mỗi bản ghi AD & DV có đủ trường 'Status:' + 'Provenance/Evidence:' (chống bịa — phải có bằng chứng).
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

    // INV-5: mọi tham chiếu CP## trong journal nằm trong 1..15 (CP1–CP15 là tập đóng — N-011).
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
                if (id is < 1 or > 15)
                {
                    offenders.Add($"{file}: CP{id}");
                }
            }
        }

        Assert.True(offenders.Count == 0, $"Tham chiếu CP ngoài phạm vi 1..15: {string.Join("; ", offenders.Distinct())}.");
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
                yield return ($"{prefix}-{ParseInt(head.Groups[2].Value):D3}", chunk);
            }
        }
    }

    private static int ParseInt(string value) => int.Parse(value, CultureInfo.InvariantCulture);

    private static string FormatIds(string prefix, IEnumerable<int> ids) =>
        string.Join(", ", ids.Select(x => $"{prefix}-{x:D3}"));

    private static string RequireJournalDir()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, ".kiro", "specs", "platform-base", "journal");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        // FAIL rõ ràng (không skip): cổng anti-drift KHÔNG được tự tắt âm thầm. Test project là dev-time,
        // không ship theo product → "không tìm thấy journal" nghĩa là layout repo governance đã vỡ = drift thật.
        Assert.Fail(
            "Không tìm thấy .kiro/specs/platform-base/journal khi đi lên từ "
            + $"'{AppContext.BaseDirectory}'. Cổng journal-consistency (L4/AD-030) không thể chạy — "
            + "kiểm tra lại layout repo (journal phải ở tổ tiên của thư mục test).");
        return null!; // unreachable — Assert.Fail ném XunitException.
    }
}
