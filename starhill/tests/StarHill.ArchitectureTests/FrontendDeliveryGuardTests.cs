using Xunit;

namespace StarHill.ArchitectureTests;

/// <summary>Guards the StarHill frontend/browser gate from silently disappearing from CI.</summary>
public sealed class FrontendDeliveryGuardTests
{
    [Fact]
    public void QR_AD051_frontend_build_and_browser_gate_are_present_in_ci()
    {
        var root = FindRepositoryRoot();
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "starhill-ci.yml"));
        var verify = File.ReadAllText(Path.Combine(root, "starhill", "tools", "verify.ps1"));

        Assert.Contains("frontend:", workflow);
        Assert.Contains("run: pnpm build", workflow);
        Assert.Contains("playwright install --with-deps chromium", workflow);
        Assert.Contains("run: pnpm e2e", workflow);
        Assert.Contains("run: python tests/validate_ci.py", workflow);
        Assert.Contains("$ErrorActionPreference = 'Stop'", verify);
    }

    [Fact]
    public void QR_AD052_rules_preview_slice_has_real_route_and_browser_coverage()
    {
        var root = FindRepositoryRoot();
        var router = File.ReadAllText(Path.Combine(root, "starhill", "web", "apps", "admin-web", "src", "router", "index.ts"));
        var view = File.ReadAllText(Path.Combine(root, "starhill", "web", "apps", "admin-web", "src", "views", "RulesView.vue"));
        var e2e = File.ReadAllText(Path.Combine(root, "starhill", "web", "e2e", "tests", "rules.spec.ts"));

        Assert.Contains("name: 'rules'", router);
        Assert.Contains("getRuleDraftPreview", view);
        Assert.Contains("getRulePublicationHistory", view);
        Assert.Contains("rules preview renders sanitized HTML", e2e);
        Assert.Contains("no horizontal overflow", e2e);
    }

    [Fact]
    public void QR_AD054_rules_and_faq_editors_have_real_routes_contracts_and_browser_coverage()
    {
        var root = FindRepositoryRoot();
        var router = File.ReadAllText(Path.Combine(root, "starhill", "web", "apps", "admin-web", "src", "router", "index.ts"));
        var nav = File.ReadAllText(Path.Combine(root, "starhill", "web", "apps", "admin-web", "src", "components", "NavList.vue"));
        var rulesView = File.ReadAllText(Path.Combine(root, "starhill", "web", "apps", "admin-web", "src", "views", "RulesView.vue"));
        var rulesEditor = File.ReadAllText(Path.Combine(root, "starhill", "web", "apps", "admin-web", "src", "components", "RuleEditorPanel.vue"));
        var faqView = File.ReadAllText(Path.Combine(root, "starhill", "web", "apps", "admin-web", "src", "views", "FaqView.vue"));
        var client = File.ReadAllText(Path.Combine(root, "starhill", "web", "apps", "admin-web", "src", "api", "client.ts"));
        var rulesE2e = File.ReadAllText(Path.Combine(root, "starhill", "web", "e2e", "tests", "rules.spec.ts"));
        var faqE2e = File.ReadAllText(Path.Combine(root, "starhill", "web", "e2e", "tests", "faq.spec.ts"));

        Assert.Contains("name: 'rules'", router);
        Assert.Contains("name: 'faq'", router);
        Assert.Contains("{ key: 'faq', icon: 'pi-question-circle', to: '/faq' }", nav);
        Assert.Contains("getRuleAdminDraft", rulesView);
        Assert.Contains("updateRuleSection", rulesEditor);
        Assert.Contains("getFaqAdminTree", client);
        Assert.Contains("reorderFaqItems", client);
        Assert.Contains("getFaqAdminTree", faqView);
        Assert.Contains("editor sends row versions", rulesE2e);
        Assert.Contains("item update, missing translation", faqE2e);
        Assert.Contains("no horizontal overflow", faqE2e);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, ".github", "workflows", "starhill-ci.yml")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Repository root could not be located.");
    }
}
