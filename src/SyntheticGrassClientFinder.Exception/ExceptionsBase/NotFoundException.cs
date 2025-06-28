using System.Net;

namespace SyntheticGrassClientFinder.Exception.ExceptionsBase;

public class NotFoundException : SyntheticGrassException
{
    public NotFoundException(string message) : base(message) { }

    public override int StatusCode => (int)HttpStatusCode.NotFound;

    public override string GetErrorMessage() => Message;
}