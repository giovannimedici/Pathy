using System.Security.Cryptography;
using Pathy.Domain.Validation;

namespace Pathy.Domain.Services;

public static class Base62SlugGenerator
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    public static string Generate()
    {
        var length = RandomNumberGenerator.GetInt32(SlugRules.MinLength, SlugRules.MaxLength + 1);
        var slug = new char[length];
        var randomBytes = new byte[length];

        RandomNumberGenerator.Fill(randomBytes);

        for (var i = 0; i < length; i++)
        {
            slug[i] = Alphabet[randomBytes[i] % Alphabet.Length];
        }

        return new string(slug);
    }
}
