namespace SyntheticGrassClientFinder.Exception.ExceptionsBase;

public abstract class SyntheticGrassException : SystemException
{
    protected SyntheticGrassException(string message) : base(message) { }
    
    public abstract int StatusCode { get; }
    public abstract string GetErrorMessage();
}