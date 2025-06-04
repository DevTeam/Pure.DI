/*// ReSharper disable UnusedVariable
namespace Pure.DI.Tests;

using System.Collections.Immutable;
using Core;
using Core.Code;
using Core.Code.v2;
using Core.Models;
using Microsoft.CodeAnalysis;
using Moq;
using static Lifetime;
using VarsMap=Core.Code.v2.VarsMap;

public class VarsMapTests
{
    private readonly Mock<IVariableNameProvider> _variableNameProvider = new ();

    [Theory]
    [InlineData(Singleton, true)]
    [InlineData(Scoped, true)]
    [InlineData(PerResolve, true)]
    [InlineData(PerBlock, true)]
    [InlineData(Transient, false)]
    internal void ShouldHandleConsequentially(Lifetime lifetime, bool isCreated)
    {
        // Given
        var classMap = CreateInstance();
        var rootMap = classMap.CreateRootMap();

        // When
        var id1Var = rootMap.GetVar(CreateInjection("int"), CreateNode(0, "id", lifetime));
        id1Var.Var.IsCreated = true;

        var id2Var = rootMap.GetVar(CreateInjection("int"), CreateNode(0, "id", lifetime));
        var id3Var = rootMap.GetVar(CreateInjection("int"), CreateNode(0, "id", lifetime));

        // Then
        id2Var.Var.IsCreated.ShouldBe(isCreated);
        id3Var.Var.IsCreated.ShouldBe(isCreated);
    }

    [Theory]
    [InlineData(Singleton, true)]
    [InlineData(Scoped, true)]
    [InlineData(PerResolve, true)]
    [InlineData(PerBlock, true)]
    [InlineData(Transient, false)]
    internal void ShouldHandleConsequentiallyInBlock(Lifetime lifetime, bool isCreated)
    {
        // Given
        var classMap = CreateInstance();
        var rootMap = classMap.CreateRootMap();
        var myVar = rootMap.GetVar(CreateInjection("string"), CreateNode(1, "my", Singleton));
        var blockMap = rootMap.CreateBlockMap();

        // When
        var id1Var = blockMap.GetVar(CreateInjection("int"), CreateNode(0, "id", lifetime));
        id1Var.Var.IsCreated = true;

        var id2Var = blockMap.GetVar(CreateInjection("int"), CreateNode(0, "id", lifetime));
        var id3Var = blockMap.GetVar(CreateInjection("int"), CreateNode(0, "id", lifetime));

        // Then
        id2Var.Var.IsCreated.ShouldBe(isCreated);
        id3Var.Var.IsCreated.ShouldBe(isCreated);
    }

    [Theory]
    [InlineData(Singleton, true)]
    [InlineData(Scoped, true)]
    [InlineData(PerResolve, true)]
    [InlineData(PerBlock, false)]
    [InlineData(Transient, false)]
    internal void ShouldHandleAfterBlock(Lifetime lifetime, bool isCreated)
    {
        // Given
        var classMap = CreateInstance();
        var rootMap = classMap.CreateRootMap();

        // When
        var myVar = rootMap.GetVar(CreateInjection("string"), CreateNode(1, "my", Singleton));
        var blockMap = rootMap.CreateBlockMap();

        var id1Var = blockMap.GetVar(CreateInjection("int"), CreateNode(0, "id", lifetime));
        id1Var.Var.IsCreated = true;

        var id2Var = rootMap.GetVar(CreateInjection("int"), CreateNode(0, "id", lifetime));

        // Then
        id2Var.Var.IsCreated.ShouldBe(isCreated);
    }

    [Theory]
    [InlineData(Singleton, false)]
    [InlineData(Scoped, false)]
    [InlineData(PerResolve, false)]
    [InlineData(PerBlock, false)]
    [InlineData(Transient, false)]
    internal void ShouldHandleAfterLazy(Lifetime lifetime, bool isCreated)
    {
        // Given
        var classMap = CreateInstance();
        var rootMap = classMap.CreateRootMap();

        // When
        var myVar = rootMap.GetVar(CreateInjection("string"), CreateNode(1, "my", Singleton));
        var blockMap = rootMap.CreateLazyMap();

        var id1Var = blockMap.GetVar(CreateInjection("int"), CreateNode(0, "id", lifetime));
        id1Var.Var.IsCreated = true;

        var id2Var = rootMap.GetVar(CreateInjection("int"), CreateNode(0, "id", lifetime));

        // Then
        id2Var.Var.IsCreated.ShouldBe(isCreated);
    }

    [Theory]
    [InlineData(Singleton)]
    [InlineData(Scoped)]
    internal void ShouldCreateTransientWithinSingletonBlock(Lifetime lifetime)
    {
        // Given
        var classMap = CreateInstance();
        var rootMap = classMap.CreateRootMap();

        // When
        var idVar = rootMap.GetVar(CreateInjection("int"), CreateNode(0, "id", Transient));
        var carVar = rootMap.GetVar(CreateInjection("ICar"), CreateNode(1, "myCar", lifetime));
        var carBlockMap = rootMap.CreateBlockMap();
        var carIdVar = carBlockMap.GetVar(CreateInjection("int"), CreateNode(0, "id", Transient));

        // Then
        carIdVar.Var.Declaration.ShouldNotBe(idVar.Var.Declaration);
        classMap.GetSingletons().ShouldBe([carVar.Var.Declaration]);
    }

    private VarsMap CreateInstance() => new(_variableNameProvider.Object, new IdGenerator());

    private static Injection CreateInjection(string contractTypeName, object? tag = null) =>
        new(InjectionKind.Parameter, CreateType(contractTypeName), tag, ImmutableArray<Location>.Empty);

    private static ITypeSymbol CreateType(string typeName)
    {
        var type = new Mock<ITypeSymbol>();
        type.SetupGet(i => i.Name).Returns(typeName);
        type.Setup(i => i.ToString()).Returns(typeName);
 #pragma warning disable RS1024
        type.Setup(i => i.GetHashCode()).Returns(typeName.GetHashCode);
        type.Setup(i => i.Equals(It.IsAny<ITypeSymbol>())).Returns<ITypeSymbol>(other => typeName.Equals(other.Name));
 #pragma warning restore RS1024
        return type.Object;
    }

    private IDependencyNode CreateNode(int bindingId, string name, Lifetime lifetime)
    {
        var node = new Mock<IDependencyNode>();
        node.SetupGet(i => i.BindingId).Returns(bindingId);
        node.SetupGet(i => i.Lifetime).Returns(lifetime);
        node.Setup(i => i.ToString()).Returns($"{name} as {lifetime}");
        _variableNameProvider.Setup(i => i.GetVariableName(node.Object, It.IsAny<int>())).Returns(name);
        return node.Object;
    }
}*/