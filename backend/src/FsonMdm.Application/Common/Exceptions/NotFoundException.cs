namespace FsonMdm.Application.Common.Exceptions;

/// <summary>Requested resource does not exist within the caller's tenant scope.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
