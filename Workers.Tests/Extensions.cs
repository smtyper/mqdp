using Reputation.Data.Processing;
using Reputation.Data.Processing.Models;

namespace Workers.Tests;

public static class Extensions
{
    public static T? WithHashId<T>(this T? input) where T : HashableObject
    {
        if (input is null)
            return null;

        if (input.HashId is null)
            input.GenerateHashId();

        return input;
    }
}
