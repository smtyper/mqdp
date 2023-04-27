using System;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Workers;

internal static class InternalExtensions
{
    internal static bool IsAssignableToGenericType(this Type? givenType, Type? genericType)
    {
        while (true)
        {
            if (givenType is null || genericType is null || !genericType.IsGenericType)
                return false;

            if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
                return true;

            if (givenType.GetInterfaces()
                .Any(it => it.IsGenericType && it.GetGenericTypeDefinition() == genericType))
                return true;

            givenType = givenType.BaseType;
        }
    }

    internal static string TrimEnd(this string input, string trimString) =>
        input.EndsWith(trimString, StringComparison.InvariantCulture) ?
            input.Remove(input.LastIndexOf(trimString, StringComparison.Ordinal)) :
            input;

    internal static bool? GetIsWorkerEnabledValue(this IConfiguration configuration, string workerName) =>
        configuration.GetValue<bool?>($"Workers:{workerName.TrimEnd("Worker")}:IsEnabled");
}
