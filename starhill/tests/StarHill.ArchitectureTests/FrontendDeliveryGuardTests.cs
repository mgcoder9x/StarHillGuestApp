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
        Assert.Contains("'/v1/identity/token/login'", client);
        Assert.DoesNotContain("request<LoginResult>('/v1/token/login'", client);
        Assert.Contains("getFaqAdminTree", client);
        Assert.Contains("reorderFaqItems", client);
        Assert.Contains("getFaqAdminTree", faqView);
        Assert.Contains("editor sends row versions", rulesE2e);
        Assert.Contains("item update, missing translation", faqE2e);
        Assert.Contains("no horizontal overflow", faqE2e);
    }

    [Fact]
    public void QR_AD055_guest_qr_entry_resolves_the_token_and_has_browser_coverage()
    {
        var root = FindRepositoryRoot();
        var router = File.ReadAllText(Path.Combine(root, "starhill", "web", "apps", "guest-web", "src", "router", "index.ts"));
        var entryView = File.ReadAllText(Path.Combine(root, "starhill", "web", "apps", "guest-web", "src", "views", "GuestResolveView.vue"));
        var client = File.ReadAllText(Path.Combine(root, "starhill", "web", "apps", "guest-web", "src", "api", "guestApi.ts"));
        var e2e = File.ReadAllText(Path.Combine(root, "starhill", "web", "e2e", "tests", "guest-resolve.spec.ts"));

        Assert.Contains("path: '/r/:token'", router);
        Assert.Contains("resolveGuestToken", entryView);
        Assert.Contains("'/v1/guest/resolve'", client);
        Assert.Contains("never renders a blank page", e2e);
        Assert.Contains("recovery message instead of a blank page", e2e);
    }

    [Fact]
    public void QR_AD056_public_guest_gateway_uses_a_pinned_reverse_proxy_and_blocks_admin_surfaces()
    {
        var root = FindRepositoryRoot();
        var template = File.ReadAllText(Path.Combine(root, "starhill", "deploy", "nginx", "guest-gateway.conf.template"));
        var startScript = File.ReadAllText(Path.Combine(root, "starhill", "scripts", "start-guest-gateway.ps1"));

        Assert.Contains("listen 127.0.0.1:__STARHILL_LISTEN_PORT__", template);
        Assert.Contains("location = /v1/guest/resolve", template);
        Assert.Contains("location ^~ /v1/", template);
        Assert.Contains("location ^~ /admin", template);
        Assert.Contains("access_log off", template);
        Assert.Contains("Referrer-Policy \"no-referrer\"", template);
        Assert.Contains("$nginxVersion = '1.28.3'", startScript);
        Assert.Contains("$nginxSha256 = 'aad7bf75d669ece7671688bfdf35f1093d6a30d2e62469405f4d55d8d82d5fd3'", startScript);
        Assert.Contains("Get-FileHash", startScript);
        Assert.Contains("-t -p", startScript);
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
