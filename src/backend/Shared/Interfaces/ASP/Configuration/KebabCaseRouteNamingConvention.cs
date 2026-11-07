using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace MineSenseSafety.Shared.Interfaces.ASP.Configuration;

/// <summary>
/// Turns controller names into kebab-case routes (FleetOperators -> fleet-operators).
/// </summary>
public partial class KebabCaseRouteNamingConvention : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        foreach (var selector in controller.Selectors)
        {
            if (selector.AttributeRouteModel?.Template is null) continue;
            selector.AttributeRouteModel.Template = selector.AttributeRouteModel.Template
                .Replace("[controller]", ToKebabCase(controller.ControllerName));
        }
    }

    private static string ToKebabCase(string value) =>
        UpperCaseBoundary().Replace(value, "$1-$2").ToLowerInvariant();

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex UpperCaseBoundary();
}
