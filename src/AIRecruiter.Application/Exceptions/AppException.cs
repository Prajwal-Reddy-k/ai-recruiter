namespace AIRecruiter.Application.Exceptions;

public abstract class AppException : Exception
{
    protected AppException(int statusCode, string errorCode, string message) : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }

    public int StatusCode { get; }
    public string ErrorCode { get; }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message, string errorCode = "NOT_FOUND")
        : base(StatusCodes.Status404NotFound, errorCode, message) { }
}

public class ForbiddenException : AppException
{
    public ForbiddenException(string message, string errorCode = "FORBIDDEN")
        : base(StatusCodes.Status403Forbidden, errorCode, message) { }
}

public class ConflictException : AppException
{
    public ConflictException(string errorCode, string message)
        : base(StatusCodes.Status409Conflict, errorCode, message) { }
}

public class ValidationException : AppException
{
    public ValidationException(string message, string errorCode = "VALIDATION_ERROR")
        : base(StatusCodes.Status400BadRequest, errorCode, message) { }
}

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message, string errorCode = "UNAUTHORIZED")
        : base(StatusCodes.Status401Unauthorized, errorCode, message) { }
}

public class ExternalServiceUnavailableException : AppException
{
    public ExternalServiceUnavailableException(string message, int? retryAfterSeconds = null)
        : base(StatusCodes.Status503ServiceUnavailable, "SERVICE_UNAVAILABLE", message)
    {
        RetryAfterSeconds = retryAfterSeconds;
    }

    public int? RetryAfterSeconds { get; }
}

public class RateLimitedException : AppException
{
    public RateLimitedException(string message, int? retryAfterSeconds = null)
        : base(StatusCodes.Status429TooManyRequests, "RATE_LIMITED", message)
    {
        RetryAfterSeconds = retryAfterSeconds;
    }

    public int? RetryAfterSeconds { get; }
}

file static class StatusCodes
{
    public const int Status400BadRequest = 400;
    public const int Status401Unauthorized = 401;
    public const int Status403Forbidden = 403;
    public const int Status404NotFound = 404;
    public const int Status409Conflict = 409;
    public const int Status429TooManyRequests = 429;
    public const int Status503ServiceUnavailable = 503;
}
