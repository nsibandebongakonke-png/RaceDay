namespace RaceDay.Web.Services;

/// <summary>The outcome of one call to the RaceDay API: success, or a friendly error message ready to show the user.</summary>
public class ApiResult
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public string Error { get; init; } = string.Empty;

    public bool IsUnauthorized => StatusCode == 401;

    public static ApiResult Failure(int statusCode, string error) => new() { Success = false, StatusCode = statusCode, Error = error };
}

public class ApiResult<T> : ApiResult
{
    public T? Data { get; init; }

    public static ApiResult<T> Ok(T? data, int statusCode = 200) => new() { Success = true, StatusCode = statusCode, Data = data };

    public new static ApiResult<T> Failure(int statusCode, string error) => new() { Success = false, StatusCode = statusCode, Error = error };
}

public class ApiSettings
{
    public string BaseUrl { get; set; } = "http://localhost:5100";
}
