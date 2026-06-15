namespace FsonMdm.Application.Common.Exceptions;

/// <summary>Authentication failed or the supplied credentials/keys are invalid.</summary>
public class AuthException : Exception
{
    public AuthException(string message) : base(message) { }
}
