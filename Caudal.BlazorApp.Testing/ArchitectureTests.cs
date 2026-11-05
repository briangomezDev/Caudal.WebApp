using System.Reflection;
using Caudal.BlazorApp.Modules.Account.Register;
using NetArchTest.Rules;
using Xunit;

namespace Caudal.BlazorApp.Testing;

public class ArchitectureTests
{
    private static readonly string[] ProtectedLayers = ["Caudal.Application", "Caudal.Domain", "Caudal.Infraestructure"];
    private static readonly Assembly Presentation = typeof(RegisterFacade).Assembly;

    // Bueno: solo las Facade cruzan hacia las capas internas.

    [Fact]
    public void Presentation_OutsideFacades_DoesNotDependOnInnerLayers()
    {
        // Act
        var result = Types.InAssembly(Presentation)
            .That().ResideInNamespace("Caudal.BlazorApp")
            .And().DoNotHaveNameEndingWith("Facade")
            .ShouldNot().HaveDependencyOnAny(ProtectedLayers)
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Facades_PublicMethods_DoNotExposeInnerLayerTypes()
    {
        // Arrange
        var facades = Presentation.GetTypes().Where(type => type.IsPublic && type.Name.EndsWith("Facade"));

        // Act
        var leaks = facades
            .SelectMany(facade => facade.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(method => Signature(method).Any(IsInnerLayerType))
            .Select(method => $"{method.DeclaringType!.Name}.{method.Name}")
            .ToList();

        // Assert
        Assert.NotEmpty(facades);
        Assert.Empty(leaks);
    }

    private static IEnumerable<Type> Signature(MethodInfo method) =>
        method.GetParameters().Select(parameter => parameter.ParameterType).Append(method.ReturnType);

    private static bool IsInnerLayerType(Type type) =>
        ProtectedLayers.Any(layer => type.Namespace?.StartsWith(layer) == true)
        || type.GetGenericArguments().Any(IsInnerLayerType);
}
