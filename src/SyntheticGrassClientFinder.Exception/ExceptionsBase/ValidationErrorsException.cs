using System.Net;

namespace SyntheticGrassClientFinder.Exception.ExceptionsBase;

public class ValidationErrorsException : SyntheticGrassException
{
    public List<string> ErrorMessages { get; }

    public ValidationErrorsException(List<string> errorMessages) : base(string.Empty)
    {
        ErrorMessages = errorMessages;
    }

    public override int StatusCode => (int)HttpStatusCode.BadRequest;

    public override string GetErrorMessage()
    {
        return string.Join(" | ", ErrorMessages);
    }
}