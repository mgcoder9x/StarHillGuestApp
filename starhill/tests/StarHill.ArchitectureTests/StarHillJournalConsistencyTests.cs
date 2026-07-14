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
/// Enforce 6 bất biến (INV-1..5 mirror base + INV-6 traceability QR-only):
/// INV-1 ID mỗi loại (QR-AD/DV/TO/N) duy nhất + liên tục 1..N; INV-2 (keystone) mọi QR-AD xuất hiện trong
/// 05-anti-drift.md; INV-3 không tham chiếu QR-*-### dangling; INV-4 QR-AD & QR-DV có Status + Provenance;
/// INV-5 tham chiếu CP## nằm trong 1..15 (QR có đúng 15 Correctness Property — design.md);
/// INV-6 (QR-AD-029) mọi QR-AD Status chứa "Implemented" phải khai '- Guard-Tests:' và mọi test class liệt kê
/// PHẢI tồn tại thật ('class Name') trong starhill/tests/**/*.cs — nối "quyết định đã xong" với guard test có thật.
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

    // INV-6: dòng Status + Guard-Tests trong một bản ghi AD, và mẫu khai báo class trong source test. NEO dòng
    // (^...$ + Multiline) để CHỈ khớp dòng field THẬT ở đầu dòng — tránh khớp nhầm khi các token này xuất hiện
    // trong prose (vd QR-AD-029 mô tả chính cơ chế `- Guard-Tests:` bên trong câu văn).
    private static readonly Regex StatusLineRegex =
        new(@"^-\s*Status:\s*(.+)$", RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex GuardTestsLineRegex =
        new(@"^-\s*Guard-Tests:\s*(.+)$", RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex ClassDeclRegex =
        new(@"\bclass\s+([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);

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

    // INV-6 (traceability, QR-AD-029): mọi QR-AD Status chứa "Implemented" PHẢI khai '- Guard-Tests:' ≥1 test
    // class; và MỌI test class được liệt kê (bất kỳ AD nào) PHẢI tồn tại thật ('class Name') trong
    // starhill/tests/**/*.cs. Đóng lớp drift design↔code mà INV-1..5 bỏ sót (chỉ kiểm nội bộ journal).
    [Fact]
    public void INV6_implemented_decisions_declare_existing_guard_tests()
    {
        var journal = RequireJournalDir();
        var declaredTestClasses = DeclaredTestClassNames();

        var text = File.ReadAllText(Path.Combine(journal, "01-decisions.md"));
        var offenders = new List<string>();

        foreach (var (id, body) in RecordSections(text, "AD"))
        {
            var statusMatch = StatusLineRegex.Match(body);
            var status = statusMatch.Success ? statusMatch.Groups[1].Value : string.Empty;
            var isImplemented = status.Contains("Implemented", StringComparison.OrdinalIgnoreCase);

            var guardMatch = GuardTestsLineRegex.Match(body);
            var guardNames = guardMatch.Success
                ? guardMatch.Groups[1].Value
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => s.Trim('`', ' '))
                    .Where(s => s.Length > 0)
                    .ToList()
                : [];

            if (isImplemented && guardNames.Count == 0)
            {
                offenders.Add($"{id}: Status 'Implemented' nhưng THIẾU '- Guard-Tests:' (hoặc rỗng).");
            }

            // Mọi tên liệt kê phải tồn tại thật → chống stale/đổi tên/xóa guard.
            foreach (var name in guardNames.Where(name => !declaredTestClasses.Contains(name)))
            {
                offenders.Add($"{id}: Guard-Test '{name}' KHÔNG tồn tại (class) trong starhill/tests/**/*.cs.");
            }
        }

        Assert.True(
            offenders.Count == 0,
            "INV-6 traceability vi phạm (quyết định-đã-Implemented ↔ guard test có thật):\n" + string.Join("\n", offenders));
    }

    private static List<int> HeadingIds(string text, string prefix) =>
        HeadingRegex.Matches(text)
            .Where(m => string.Equals(m.Groups[1].Value, prefix, StringComparison.Ordinal))
            .Select(m => ParseInt(m.Groups[2].Value))
            .ToList();

    // Thu tên MỌI class khai báo trong source test starhill/tests (bỏ bin/obj). Source-scan (không reflection
    // cross-assembly) — cùng triết lý parse-file của cổng, robust và không cần ref chéo test project (QR-TO-009).
    private static HashSet<string> DeclaredTestClassNames()
    {
        var testsDir = RequireTestsDir();
        var names = new HashSet<string>(StringComparer.Ordinal);
        var binSegment = $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}";
        var objSegment = $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}";

        foreach (var file in Directory.EnumerateFiles(testsDir, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains(binSegment, StringComparison.Ordinal) || file.Contains(objSegment, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (Match match in ClassDeclRegex.Matches(File.ReadAllText(file)))
            {
                names.Add(match.Groups[1].Value);
            }
        }

        Assert.True(names.Count > 0, $"Không thu được class test nào từ '{testsDir}' — INV-6 không thể chạy.");
        return names;
    }

    private static string RequireTestsDir()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "starhill", "tests");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        Assert.Fail(
            $"Không tìm thấy starhill/tests khi đi lên từ '{AppContext.BaseDirectory}' — cổng INV-6 không thể chạy.");
        return null!; // unreachable.
    }

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
