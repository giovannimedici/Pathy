namespace Pathy.Domain.Exceptions;

/// <summary>
/// Exception thrown when a resource existed but is no longer available (e.g., expired link).
/// </summary>
public sealed class GoneException : Exception
{
    public GoneException(string message) : base(message)
    {
    }
}
