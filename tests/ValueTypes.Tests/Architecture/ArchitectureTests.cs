using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace ValueTypes.Tests.Architecture;

/// <summary>
/// ValueTypes's architecture rules: the shared invariants from
/// <see cref="global::Architecture.Testing.ArchitectureTestsBase"/> plus the one contract this
/// package exists to enforce — that every <see cref="IValue{TSelf, TValue}"/> implementor is a
/// readonly record struct. Encode each project-specific invariant the first time it matters so drift
/// trips the build.
/// </summary>
public sealed class ArchitectureTests : global::Architecture.Testing.ArchitectureTestsBase
{
    // Matched by full name rather than a typeof, so the rule also holds for implementors compiled
    // against the package from another assembly if this test ever loads one.
    private const string IValueInterface = "ValueTypes.IValue`2";

    protected override Assembly TargetAssembly => typeof(ValueParser).Assembly;

    protected override string RootNamespace => "ValueTypes";

    /// <summary>
    /// Every <see cref="IValue{TSelf, TValue}"/> implementor must be a readonly record struct.
    /// The contract's static abstract members resolve through <c>TSelf</c> (CRTP), which only holds
    /// when <c>TSelf</c> is the struct itself; <c>readonly</c> is what makes the wrapper's value
    /// semantics real rather than conventional.
    /// </summary>
    /// <remarks>
    /// Scanned across both assemblies: the package declares the contract but implements it nowhere,
    /// so the only implementor in this repo is the reference <c>TestValue</c> in the test assembly.
    /// Without it the rule would pass vacuously and pin nothing.
    /// </remarks>
    [Fact]
    public void ValueWrappersAreReadonlyRecordStructs()
    {
        var violations = new[] { TargetAssembly, typeof(ArchitectureTests).Assembly }
            .SelectMany(assembly => assembly.GetTypes())
            .Where(ImplementsIValue)
            .Where(type => !IsReadonlyRecordStruct(type))
            .Select(type => type.FullName)
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"writing-csharp: IValue<TSelf, TValue> implementors must be readonly record structs. Violations: {string.Join(", ", violations)}");
    }

    private static bool ImplementsIValue(Type type) =>
        type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition().FullName == IValueInterface);

    // A record struct is identified by its compiler-generated PrintMembers; readonly by the attribute.
    private static bool IsReadonlyRecordStruct(Type type) =>
        type.IsValueType
        && type.IsDefined(typeof(IsReadOnlyAttribute), inherit: false)
        && type.GetMethod("PrintMembers", BindingFlags.Instance | BindingFlags.NonPublic, [typeof(StringBuilder)]) is not null;
}
