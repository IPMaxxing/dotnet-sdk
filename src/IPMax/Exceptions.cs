using System.Net;

namespace IPMax;

public class IPMaxException : Exception
{
    public IPMaxException()
    {
    }

    public IPMaxException(string message)
        : base(message)
    {
    }

    public IPMaxException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public sealed class IPMaxConnectionException : IPMaxException
{
    public IPMaxConnectionException()
    {
    }

    public IPMaxConnectionException(string message)
        : base(message)
    {
    }

    public IPMaxConnectionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public sealed class IPMaxTimeoutException : IPMaxException
{
    public IPMaxTimeoutException()
    {
    }

    public IPMaxTimeoutException(string message)
        : base(message)
    {
    }

    public IPMaxTimeoutException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public class IPMaxApiException : IPMaxException
{
    internal IPMaxApiException(ApiFailure failure)
        : base(failure.Message)
    {
        StatusCode = failure.StatusCode;
        Code = failure.Code;
        RequestId = failure.RequestId;
        Retryable = failure.Retryable;
        RetryAfter = failure.RetryAfter;
    }

    public HttpStatusCode StatusCode { get; }

    public ErrorCode? Code { get; }

    public string? RequestId { get; }

    public bool Retryable { get; }

    public TimeSpan? RetryAfter { get; }

    internal static IPMaxApiException From(ApiFailure failure) => (int)failure.StatusCode switch
    {
        400 or 413 or 415 => new IPMaxInvalidRequestException(failure),
        401 => new IPMaxAuthenticationException(failure),
        402 => new IPMaxInsufficientBalanceException(failure),
        404 => new IPMaxNotFoundException(failure),
        409 => new IPMaxConflictException(failure),
        429 => new IPMaxRateLimitException(failure),
        >= 500 => new IPMaxServerException(failure),
        _ => new IPMaxApiException(failure),
    };
}

public sealed class IPMaxInvalidRequestException : IPMaxApiException
{
    internal IPMaxInvalidRequestException(ApiFailure failure)
        : base(failure)
    {
    }
}

public sealed class IPMaxAuthenticationException : IPMaxApiException
{
    internal IPMaxAuthenticationException(ApiFailure failure)
        : base(failure)
    {
    }
}

public sealed class IPMaxInsufficientBalanceException : IPMaxApiException
{
    internal IPMaxInsufficientBalanceException(ApiFailure failure)
        : base(failure)
    {
    }
}

public sealed class IPMaxNotFoundException : IPMaxApiException
{
    internal IPMaxNotFoundException(ApiFailure failure)
        : base(failure)
    {
    }
}

public sealed class IPMaxConflictException : IPMaxApiException
{
    internal IPMaxConflictException(ApiFailure failure)
        : base(failure)
    {
    }
}

public sealed class IPMaxRateLimitException : IPMaxApiException
{
    internal IPMaxRateLimitException(ApiFailure failure)
        : base(failure)
    {
    }
}

public sealed class IPMaxServerException : IPMaxApiException
{
    internal IPMaxServerException(ApiFailure failure)
        : base(failure)
    {
    }
}

internal readonly record struct ApiFailure(
    string Message,
    HttpStatusCode StatusCode,
    ErrorCode? Code,
    string? RequestId,
    bool Retryable,
    TimeSpan? RetryAfter);
