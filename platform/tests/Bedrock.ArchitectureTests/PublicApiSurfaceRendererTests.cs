using Xunit;

namespace Bedrock.ArchitectureTests;

public sealed class PublicApiSurfaceRendererTests
{
    [Fact]
    public void Renderer_captures_constraints_inheritance_accessors_nullability_defaults_and_constants()
    {
        var surface = PublicApiSurfaceRenderer.RenderType(typeof(ApiFixture<>));

        Assert.Contains(surface, line => line.StartsWith("T:public:class:abstract:", StringComparison.Ordinal)
            && line.Contains(":where[!0:class&new()]", StringComparison.Ordinal));
        Assert.Contains(surface, line => line.StartsWith("B:", StringComparison.Ordinal)
            && line.EndsWith(nameof(ApiFixtureBase), StringComparison.Ordinal));
        Assert.Contains(surface, line => line.StartsWith("I:", StringComparison.Ordinal)
            && line.Contains("IApiFixture<!0>", StringComparison.Ordinal));
        Assert.Contains(surface, line => line.Contains(".Name:System.String?:get=public:set=protected", StringComparison.Ordinal));
        Assert.Contains(surface, line => line.Contains(
            ".Items:System.Collections.Generic.IReadOnlyList<System.String?>?:get=public:init=public",
            StringComparison.Ordinal));
        Assert.Contains(surface, line => line.Contains("Transform`1(in !0 input,System.String? suffix=null)", StringComparison.Ordinal)
            && line.Contains(":where[!!0:class&new()]", StringComparison.Ordinal));
        Assert.Contains(surface, line => line.Contains(".Version:System.Int32:public:const:value=2", StringComparison.Ordinal));
        Assert.Contains(surface, line => line.Contains(
            ":Obsolete(error=true,message=\"Use Transform instead.\",diagnostic=\"BED001\"",
            StringComparison.Ordinal));

        var enumSurface = PublicApiSurfaceRenderer.RenderType(typeof(ApiFixtureMode));
        Assert.Contains(enumSurface, line => line.Contains(".Enabled:", StringComparison.Ordinal)
            && line.EndsWith(":value=4", StringComparison.Ordinal));
    }

    public abstract class ApiFixture<T> : ApiFixtureBase, IApiFixture<T>
        where T : class, new()
    {
        public const int Version = 2;

        public string? Name { get; protected set; }

        public IReadOnlyList<string?>? Items { get; init; }

        [Obsolete(
            "Use Transform instead.",
            error: true,
            DiagnosticId = "BED001",
            UrlFormat = "https://example.invalid/{0}")]
        public void Old()
        {
        }

        protected abstract TResult Transform<TResult>(in T input, string? suffix = null)
            where TResult : class, new();
    }

    public abstract class ApiFixtureBase
    {
    }

    public interface IApiFixture<in T>
    {
    }

    public enum ApiFixtureMode
    {
        None = 0,
        Enabled = 4,
    }
}
