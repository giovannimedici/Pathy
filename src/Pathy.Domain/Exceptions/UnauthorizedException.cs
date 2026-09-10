namespace Pathy.Domain.Exceptions;

/// <summary>
/// Exception thrown when an operation requires authentication.
/// </summary>
public sealed class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message)
    {
    }
}
