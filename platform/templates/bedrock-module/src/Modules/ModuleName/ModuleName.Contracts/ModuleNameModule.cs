namespace ModuleName.Contracts;

/// <summary>Stable module identity used for keyed registrations and migrations.</summary>
public static class ModuleNameModule
{
    public const string Name = nameof(ModuleNameModule);

    public const string PersistenceKey = "MODULE_KEY";
}
