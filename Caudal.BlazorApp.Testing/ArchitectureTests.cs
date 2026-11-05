using System.Reflection;
using Caudal.BlazorApp.Modules.Account.Register;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using NetArchTest.Rules;
using Xunit;

namespace Caudal.BlazorApp.Testing;

public class ArchitectureTests
{
    private static readonly string[] ProtectedLayers = ["Caudal.Application", "Caudal.Domain", "Caudal.Infraestructure"];
    private static readonly string[] PublicModules = ["Caudal.BlazorApp.Modules.Account", "Caudal.BlazorApp.Modules.Errors"];
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

    // Bueno: una página nueva queda protegida aunque nadie se acuerde de pedirlo.

    [Fact]
    public void Pages_OutsidePublicModules_RequireSession()
    {
        // Arrange
        var pages = Presentation.GetTypes().Where(type => type.IsDefined(typeof(RouteAttribute))).ToList();

        // Act
        var unprotected = pages
            .Where(page => !IsInPublicModule(page))
            .Where(page => page.GetCustomAttribute<AuthorizeAttribute>() is null || page.GetCustomAttribute<AllowAnonymousAttribute>() is not null)
            .Select(page => page.FullName)
            .ToList();

        // Assert
        Assert.NotEmpty(pages);
        Assert.Empty(unprotected);
    }

    [Fact]
    public void Pages_InPublicModules_AllowAnonymous()
    {
        // Act
        var blocked = Presentation.GetTypes()
            .Where(type => type.IsDefined(typeof(RouteAttribute)))
            .Where(IsInPublicModule)
            .Where(page => page.GetCustomAttribute<AllowAnonymousAttribute>() is null)
            .Select(page => page.FullName)
            .ToList();

        // Assert
        Assert.Empty(blocked);
    }

    private static bool IsInPublicModule(Type page) =>
        PublicModules.Any(module => page.Namespace?.StartsWith(module) == true);

    private static IEnumerable<Type> Signature(MethodInfo method) =>
        method.GetParameters().Select(parameter => parameter.ParameterType).Append(method.ReturnType);

    private static bool IsInnerLayerType(Type type) =>
        ProtectedLayers.Any(layer => type.Namespace?.StartsWith(layer) == true)
        || type.GetGenericArguments().Any(IsInnerLayerType);
}
