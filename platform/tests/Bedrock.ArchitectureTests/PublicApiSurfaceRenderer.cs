using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Bedrock.ArchitectureTests;

/// <summary>
/// Renders the source- and binary-relevant compiled API surface without relying on source syntax. Public and
/// inheritable protected members are included; signatures retain nullability, modifiers, defaults, constraints,
/// accessors, inheritance, interface implementation and constant values.
/// </summary>
internal static partial class PublicApiSurfaceRenderer
{
    private const BindingFlags DeclaredMembers = BindingFlags.Public
        | BindingFlags.NonPublic
        | BindingFlags.Instance
        | BindingFlags.Static
        | BindingFlags.DeclaredOnly;

    private static readonly NullabilityInfoContext Nullability = new();

    public static List<string> RenderAssembly(Assembly assembly) =>
        assembly.GetTypes()
            .Where(IsApiVisible)
            .Where(type => !type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
            .OrderBy(FriendlyTypeName, StringComparer.Ordinal)
            .SelectMany(RenderType)
            .ToList();

    internal static IReadOnlyList<string> RenderType(Type type)
    {
        var typeName = FriendlyTypeName(type);
        var lines = new List<string>
        {
            $"T:{Visibility(type)}:{TypeKind(type)}:{TypeModifiers(type)}:{typeName}{GenericConstraints(type.GetGenericArguments())}",
        };

        if (type.BaseType is { } baseType && baseType != typeof(object))
        {
            lines.Add($"B:{typeName}:{FriendlyTypeName(baseType)}");
        }

        lines.AddRange(type.GetInterfaces()
            .Select(FriendlyTypeName)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .Select(name => $"I:{typeName}:{name}"));
        AddContractAttributes(lines, $"T:{typeName}", type);

        foreach (var constructor in type.GetConstructors(DeclaredMembers)
                     .Where(IsApiVisible)
                     .OrderBy(ConstructorSortKey, StringComparer.Ordinal))
        {
            var target = $"C:{typeName}({RenderParameters(constructor.GetParameters())})";
            lines.Add($"{target}:{Visibility(constructor)}");
            AddContractAttributes(lines, target, constructor);
        }

        foreach (var method in type.GetMethods(DeclaredMembers)
                     .Where(IsApiVisible)
                     .Where(IsSourceMethod)
                     .OrderBy(MethodSortKey, StringComparer.Ordinal))
        {
            var genericSuffix = method.IsGenericMethodDefinition
                ? $"`{method.GetGenericArguments().Length}"
                : string.Empty;
            var target = $"M:{typeName}.{method.Name}{genericSuffix}({RenderParameters(method.GetParameters())})";
            lines.Add(
                $"{target}:{RenderReturn(method)}:{Visibility(method)}:{MethodModifiers(method)}"
                + GenericConstraints(method.GetGenericArguments()));
            AddContractAttributes(lines, target, method);
        }

        foreach (var property in type.GetProperties(DeclaredMembers)
                     .Where(property => IsApiVisible(property.GetMethod) || IsApiVisible(property.SetMethod))
                     .OrderBy(PropertySortKey, StringComparer.Ordinal))
        {
            var index = property.GetIndexParameters();
            var indexSuffix = index.Length == 0 ? string.Empty : $"({RenderParameters(index)})";
            var target = $"P:{typeName}.{property.Name}{indexSuffix}";
            var setterKind = IsInitOnly(property.SetMethod) ? "init" : "set";
            lines.Add(
                $"{target}:{FriendlyTypeName(property.PropertyType, Nullability.Create(property))}"
                + $":get={VisibilityOrNone(property.GetMethod)}:{setterKind}={VisibilityOrNone(property.SetMethod)}");
            AddContractAttributes(lines, target, property);
        }

        foreach (var field in type.GetFields(DeclaredMembers)
                     .Where(IsApiVisible)
                     .Where(field => !field.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
                     .OrderBy(field => field.Name, StringComparer.Ordinal))
        {
            var target = $"F:{typeName}.{field.Name}";
            var value = field.IsLiteral ? $":value={FormatLiteral(field.GetRawConstantValue())}" : string.Empty;
            lines.Add(
                $"{target}:{FriendlyTypeName(field.FieldType, Nullability.Create(field))}"
                + $":{Visibility(field)}:{FieldModifiers(field)}{value}");
            AddContractAttributes(lines, target, field);
        }

        foreach (var @event in type.GetEvents(DeclaredMembers)
                     .Where(item => IsApiVisible(item.AddMethod) || IsApiVisible(item.RemoveMethod))
                     .OrderBy(item => item.Name, StringComparer.Ordinal))
        {
            var target = $"E:{typeName}.{@event.Name}";
            lines.Add(
                $"{target}:{FriendlyTypeName(@event.EventHandlerType!, Nullability.Create(@event))}"
                + $":add={VisibilityOrNone(@event.AddMethod)}:remove={VisibilityOrNone(@event.RemoveMethod)}");
            AddContractAttributes(lines, target, @event);
        }

        return lines;
    }

    private static bool IsApiVisible(Type type)
    {
        if (!type.IsNested)
        {
            return type.IsPublic;
        }

        return type.DeclaringType is not null
            && IsApiVisible(type.DeclaringType)
            && (type.IsNestedPublic || type.IsNestedFamily || type.IsNestedFamORAssem);
    }

    private static bool IsApiVisible(MethodBase? method) => method is not null
        && (method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly);

    private static bool IsApiVisible(FieldInfo field) =>
        field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly;

    private static bool IsSourceMethod(MethodInfo method) =>
        (!method.IsSpecialName || method.Name.StartsWith("op_", StringComparison.Ordinal))
        && !method.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false);

    private static string ConstructorSortKey(ConstructorInfo constructor) =>
        $"{constructor.Name}({RenderParameters(constructor.GetParameters())})";

    private static string MethodSortKey(MethodInfo method) =>
        $"{method.Name}`{method.GetGenericArguments().Length}({RenderParameters(method.GetParameters())}):{RenderReturn(method)}";

    private static string PropertySortKey(PropertyInfo property) =>
        $"{property.Name}({RenderParameters(property.GetIndexParameters())})";

    private static string RenderParameters(IEnumerable<ParameterInfo> parameters) =>
        string.Join(",", parameters.Select(RenderParameter));

    private static string RenderParameter(ParameterInfo parameter)
    {
        var info = Nullability.Create(parameter);
        var type = parameter.ParameterType;
        var modifier = string.Empty;
        if (parameter.IsDefined(typeof(ParamArrayAttribute), inherit: false))
        {
            modifier = "params ";
        }
        else if (type.IsByRef)
        {
            modifier = parameter.IsOut ? "out " : parameter.IsIn ? "in " : "ref ";
            type = type.GetElementType()!;
            info = info.ElementType ?? info;
        }

        var defaultValue = parameter.HasDefaultValue
            ? $"={FormatLiteral(parameter.DefaultValue)}"
            : string.Empty;
        return $"{modifier}{FriendlyTypeName(type, info)} {parameter.Name}{defaultValue}";
    }

    private static string RenderReturn(MethodInfo method)
    {
        var type = method.ReturnType;
        var info = Nullability.Create(method.ReturnParameter);
        if (!type.IsByRef)
        {
            return FriendlyTypeName(type, info);
        }

        type = type.GetElementType()!;
        info = info.ElementType ?? info;
        var modifier = method.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsReadOnlyAttribute))
            ? "ref readonly "
            : "ref ";
        return modifier + FriendlyTypeName(type, info);
    }

    private static string FriendlyTypeName(Type type) => FriendlyTypeName(type, null);

    private static string FriendlyTypeName(Type type, NullabilityInfo? nullability)
    {
        if (type.IsByRef)
        {
            return FriendlyTypeName(type.GetElementType()!, nullability?.ElementType) + "&";
        }

        if (type.IsPointer)
        {
            return FriendlyTypeName(type.GetElementType()!, nullability?.ElementType) + "*";
        }

        if (type.IsArray)
        {
            var commas = new string(',', type.GetArrayRank() - 1);
            var rendered = $"{FriendlyTypeName(type.GetElementType()!, nullability?.ElementType)}[{commas}]";
            return rendered + NullableSuffix(type, nullability);
        }

        if (type.IsGenericParameter)
        {
            var prefix = type.DeclaringMethod is null ? "!" : "!!";
            return prefix + type.GenericParameterPosition + NullableSuffix(type, nullability);
        }

        if (type.IsGenericType)
        {
            var definitionName = MetadataArity().Replace(type.GetGenericTypeDefinition().FullName ?? type.Name, string.Empty)
                .Replace('+', '.');
            var nullabilityArguments = nullability?.GenericTypeArguments ?? [];
            var arguments = type.GetGenericArguments()
                .Select((argument, index) => FriendlyTypeName(
                    argument,
                    index < nullabilityArguments.Length ? nullabilityArguments[index] : null))
                .ToArray();
            return $"{definitionName}<{string.Join(",", arguments)}>{NullableSuffix(type, nullability)}";
        }

        return (type.FullName ?? type.Name).Replace('+', '.') + NullableSuffix(type, nullability);
    }

    private static string NullableSuffix(Type type, NullabilityInfo? nullability) =>
        !type.IsValueType && nullability?.ReadState == NullabilityState.Nullable ? "?" : string.Empty;

    private static string GenericConstraints(Type[] arguments)
    {
        var constraints = arguments
            .Where(argument => argument.IsGenericParameter)
            .Select(RenderGenericConstraint)
            .Where(value => value.Length > 0)
            .ToArray();
        return constraints.Length == 0 ? string.Empty : $":where[{string.Join(";", constraints)}]";
    }

    private static string RenderGenericConstraint(Type argument)
    {
        var values = new List<string>();
        var attributes = argument.GenericParameterAttributes;
        var variance = attributes & GenericParameterAttributes.VarianceMask;
        if (variance == GenericParameterAttributes.Covariant)
        {
            values.Add("out");
        }
        else if (variance == GenericParameterAttributes.Contravariant)
        {
            values.Add("in");
        }

        var special = attributes & GenericParameterAttributes.SpecialConstraintMask;
        if ((special & GenericParameterAttributes.ReferenceTypeConstraint) != 0)
        {
            values.Add("class");
        }
        if ((special & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
        {
            values.Add("struct");
        }

        values.AddRange(argument.GetGenericParameterConstraints().Select(FriendlyTypeName));
        if ((special & GenericParameterAttributes.DefaultConstructorConstraint) != 0
            && (special & GenericParameterAttributes.NotNullableValueTypeConstraint) == 0)
        {
            values.Add("new()");
        }

        return values.Count == 0 ? string.Empty : $"{FriendlyTypeName(argument)}:{string.Join("&", values)}";
    }

    private static string Visibility(Type type) => type.IsPublic || type.IsNestedPublic
        ? "public"
        : type.IsNestedFamORAssem ? "protected-internal" : "protected";

    private static string Visibility(MethodBase method) => method.IsPublic
        ? "public"
        : method.IsFamilyOrAssembly ? "protected-internal" : "protected";

    private static string Visibility(FieldInfo field) => field.IsPublic
        ? "public"
        : field.IsFamilyOrAssembly ? "protected-internal" : "protected";

    private static string VisibilityOrNone(MethodBase? method) =>
        IsApiVisible(method) ? Visibility(method!) : "none";

    private static string TypeKind(Type type)
    {
        if (type.IsInterface) return "interface";
        if (type.IsEnum) return "enum";
        if (typeof(MulticastDelegate).IsAssignableFrom(type.BaseType)) return "delegate";
        if (type.IsValueType) return "struct";
        return "class";
    }

    private static string TypeModifiers(Type type)
    {
        var values = new List<string>();
        if (type.IsAbstract && type.IsSealed) values.Add("static");
        else
        {
            if (type.IsAbstract) values.Add("abstract");
            if (type.IsSealed && !type.IsValueType) values.Add("sealed");
        }
        if (type.IsByRefLike) values.Add("ref-like");
        if (type.IsValueType && type.IsDefined(typeof(IsReadOnlyAttribute), inherit: false)) values.Add("readonly");
        return values.Count == 0 ? "none" : string.Join('+', values);
    }

    private static string MethodModifiers(MethodInfo method)
    {
        var values = new List<string>();
        if (method.IsStatic) values.Add("static");
        if (method.IsAbstract) values.Add("abstract");
        if (method.IsVirtual) values.Add("virtual");
        if (method.IsFinal) values.Add("final");
        if ((method.Attributes & MethodAttributes.NewSlot) != 0) values.Add("newslot");
        if (method.IsVirtual && method.GetBaseDefinition() != method) values.Add("override");
        if (method.IsDefined(typeof(ExtensionAttribute), inherit: false)) values.Add("extension");
        return values.Count == 0 ? "none" : string.Join('+', values);
    }

    private static string FieldModifiers(FieldInfo field)
    {
        var values = new List<string>();
        if (field.IsLiteral) values.Add("const");
        else
        {
            if (field.IsStatic) values.Add("static");
            if (field.IsInitOnly) values.Add("readonly");
        }
        return values.Count == 0 ? "none" : string.Join('+', values);
    }

    private static bool IsInitOnly(MethodInfo? setter) => setter?.ReturnParameter
        .GetRequiredCustomModifiers()
        .Contains(typeof(IsExternalInit)) == true;

    private static string FormatLiteral(object? value)
    {
        if (value is null || value == DBNull.Value || value == Type.Missing)
        {
            return "null";
        }

        return value switch
        {
            string text => JsonSerializer.Serialize(text),
            char character => JsonSerializer.Serialize(character.ToString()),
            bool boolean => boolean ? "true" : "false",
            float single => single.ToString("R", CultureInfo.InvariantCulture),
            double @double => @double.ToString("R", CultureInfo.InvariantCulture),
            decimal @decimal => @decimal.ToString(CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? value.ToString() ?? string.Empty,
        };
    }

    private static void AddContractAttributes(
        List<string> lines,
        string target,
        MemberInfo member)
    {
        if (member.GetCustomAttribute<ObsoleteAttribute>(inherit: false) is { } obsolete)
        {
            lines.Add(
                $"A:{target}:Obsolete(error={obsolete.IsError.ToString().ToLowerInvariant()},"
                + $"message={FormatLiteral(obsolete.Message)},diagnostic={FormatLiteral(obsolete.DiagnosticId)},"
                + $"url={FormatLiteral(obsolete.UrlFormat)})");
        }

        if (member is Type type && type.IsDefined(typeof(FlagsAttribute), inherit: false))
        {
            lines.Add($"A:{target}:Flags");
        }

        if (member.IsDefined(typeof(RequiredMemberAttribute), inherit: false))
        {
            lines.Add($"A:{target}:RequiredMember");
        }

        if (member.IsDefined(typeof(SetsRequiredMembersAttribute), inherit: false))
        {
            lines.Add($"A:{target}:SetsRequiredMembers");
        }
    }

    [GeneratedRegex("`[0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex MetadataArity();
}
