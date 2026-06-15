namespace FsonMdm.Application.Common.Exceptions;

/// <summary>Caller-supplied data is invalid (bad request).</summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}
