using BlogSite.Assets;

namespace BlogSite.Exceptions;

public class RouterException : Exception
{
    public  RouterException() : base() { }
    public  RouterException(string? message) : base(message) { }
    public  RouterException(string? message, Exception? innerException) : base(message, innerException) { }
}