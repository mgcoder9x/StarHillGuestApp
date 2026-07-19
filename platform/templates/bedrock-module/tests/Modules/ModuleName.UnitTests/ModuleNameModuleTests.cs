using ModuleName.Contracts;
using Xunit;

namespace ModuleName.UnitTests;

public sealed class ModuleNameModuleTests
{
    [Fact]
    public void Scaffold_exposes_stable_module_identity()
    {
        Assert.Equal(nameof(ModuleNameModule), ModuleNameModule.Name);
        Assert.Equal("MODULE_KEY", ModuleNameModule.PersistenceKey);
    }
}
