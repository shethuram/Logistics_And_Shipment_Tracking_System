namespace Logistics.Api.Exceptions;

public abstract class AppException : Exception
{
    public abstract int StatusCode { get; }
    public abstract string Error { get; }

    protected AppException(string message) : base(message) { }
}

public class NotFoundException : AppException
{
    public override int StatusCode => StatusCodes.Status404NotFound;
    public override string Error => "Not Found";

    public NotFoundException(string message) : base(message) { }
}

public class ConflictException : AppException
{
    public override int StatusCode => StatusCodes.Status409Conflict;
    public override string Error => "Conflict";

    public ConflictException(string message) : base(message) { }
}

public class ValidationException : AppException
{
    public override int StatusCode => StatusCodes.Status400BadRequest;
    public override string Error => "Bad Request";

    public ValidationException(string message) : base(message) { }
}

public class BusinessRuleException : AppException
{
    public override int StatusCode => StatusCodes.Status422UnprocessableEntity;
    public override string Error => "Unprocessable Entity";

    public BusinessRuleException(string message) : base(message) { }
}

public class ForbiddenException : AppException
{
    public override int StatusCode => StatusCodes.Status403Forbidden;
    public override string Error => "Forbidden";

    public ForbiddenException(string message) : base(message) { }
}


public class TooManyRequestsException : AppException
{
    public override int StatusCode => StatusCodes.Status429TooManyRequests;
    public override string Error => "Too Many Requests";

    public TooManyRequestsException(string message) : base(message) { }
}

