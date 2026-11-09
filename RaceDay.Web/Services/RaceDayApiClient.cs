using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Services;

/// <summary>
/// Typed HttpClient for the RaceDay API. It forwards the logged-in user's API session cookie
/// (kept in the MVC session) on every call, and turns every failure - validation, permissions,
/// API offline - into a short friendly message so raw errors never reach the user.
/// </summary>
public class RaceDayApiClient : IRaceDayApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;
    private readonly ILogger<RaceDayApiClient> _logger;

    public RaceDayApiClient(HttpClient http, IHttpContextAccessor accessor, ILogger<RaceDayApiClient> logger)
    {
        _http = http;
        _accessor = accessor;
        _logger = logger;
    }

    // ------------------------------------------------------------------ Auth
    public async Task<ApiResult> RegisterAsync(object request) =>
        await SendAsync<object>(HttpMethod.Post, "api/auth/register", JsonContent(request), readBody: false);

    public async Task<ApiResult<LoginResult>> LoginAsync(string email, string password)
    {
        string? cookie = null;
        var result = await SendAsync<UserDto>(HttpMethod.Post, "api/auth/login",
            JsonContent(new { email, password }), onResponse: response => cookie = ExtractCookie(response));

        if (!result.Success || result.Data is null || string.IsNullOrEmpty(cookie))
        {
            var error = result.Success ? "We could not start your session. Please try again." : result.Error;
            return ApiResult<LoginResult>.Failure(result.Success ? 500 : result.StatusCode, error);
        }

        return ApiResult<LoginResult>.Ok(new LoginResult(result.Data, cookie), result.StatusCode);
    }

    public async Task<ApiResult> LogoutAsync() =>
        await SendAsync<object>(HttpMethod.Post, "api/auth/logout", null, readBody: false);

    // ------------------------------------------------------------------ Profile
    public Task<ApiResult<UserDto>> GetProfileAsync() => SendAsync<UserDto>(HttpMethod.Get, "api/profile");

    public Task<ApiResult<UserDto>> UpdateProfileAsync(string firstName, string lastName, string? phone) =>
        SendAsync<UserDto>(HttpMethod.Put, "api/profile", JsonContent(new { firstName, lastName, phone }));

    public Task<ApiResult<UserDto>> UploadProfilePictureAsync(IFormFile file) =>
        SendAsync<UserDto>(HttpMethod.Post, "api/profile/picture", FileContent(file));

    // ------------------------------------------------------------------ Events
    public Task<ApiResult<List<EventSummaryDto>>> GetEventsAsync(EventFilterModel filter)
    {
        var query = new Dictionary<string, string?>
        {
            ["search"] = filter.Search,
            ["type"] = filter.Type,
            ["from"] = filter.From?.ToString("yyyy-MM-dd"),
            ["to"] = filter.To?.ToString("yyyy-MM-dd"),
            ["upcomingOnly"] = filter.IncludePast ? "false" : "true"
        };
        var url = QueryHelpers.AddQueryString("api/events", query.Where(kv => !string.IsNullOrWhiteSpace(kv.Value)).ToDictionary(kv => kv.Key, kv => kv.Value));
        return SendAsync<List<EventSummaryDto>>(HttpMethod.Get, url);
    }

    public Task<ApiResult<List<EventSummaryDto>>> GetMyEventsAsync() =>
        SendAsync<List<EventSummaryDto>>(HttpMethod.Get, "api/events/mine");

    public Task<ApiResult<EventDetailDto>> GetEventAsync(int id) =>
        SendAsync<EventDetailDto>(HttpMethod.Get, $"api/events/{id}");

    public Task<ApiResult<EventDetailDto>> CreateEventAsync(object request) =>
        SendAsync<EventDetailDto>(HttpMethod.Post, "api/events", JsonContent(request));

    public Task<ApiResult<EventDetailDto>> UpdateEventAsync(int id, object request) =>
        SendAsync<EventDetailDto>(HttpMethod.Put, $"api/events/{id}", JsonContent(request));

    public async Task<ApiResult> DeleteEventAsync(int id) =>
        await SendAsync<object>(HttpMethod.Delete, $"api/events/{id}", readBody: false);

    public Task<ApiResult<EventDetailDto>> UploadEventBannerAsync(int id, IFormFile file) =>
        SendAsync<EventDetailDto>(HttpMethod.Post, $"api/events/{id}/banner", FileContent(file));

    // ------------------------------------------------------------------ Categories
    public Task<ApiResult<List<CategoryDto>>> GetCategoriesAsync(int eventId) =>
        SendAsync<List<CategoryDto>>(HttpMethod.Get, $"api/events/{eventId}/categories");

    public Task<ApiResult<CategoryDto>> CreateCategoryAsync(int eventId, object request) =>
        SendAsync<CategoryDto>(HttpMethod.Post, $"api/events/{eventId}/categories", JsonContent(request));

    public Task<ApiResult<CategoryDto>> UpdateCategoryAsync(int categoryId, object request) =>
        SendAsync<CategoryDto>(HttpMethod.Put, $"api/categories/{categoryId}", JsonContent(request));

    public async Task<ApiResult> DeleteCategoryAsync(int categoryId) =>
        await SendAsync<object>(HttpMethod.Delete, $"api/categories/{categoryId}", readBody: false);

    // ------------------------------------------------------------------ Enrolments
    public Task<ApiResult<MyEnrolmentDto>> EnrolAsync(int eventId, int categoryId) =>
        SendAsync<MyEnrolmentDto>(HttpMethod.Post, "api/enrolments", JsonContent(new { eventId, categoryId }));

    public Task<ApiResult<List<MyEnrolmentDto>>> GetMyEnrolmentsAsync() =>
        SendAsync<List<MyEnrolmentDto>>(HttpMethod.Get, "api/enrolments/my");

    public async Task<ApiResult> CancelEnrolmentAsync(int enrolmentId) =>
        await SendAsync<object>(HttpMethod.Put, $"api/enrolments/{enrolmentId}/cancel", readBody: false);

    public Task<ApiResult<List<EnrolmentDto>>> GetEventEnrolmentsAsync(int eventId) =>
        SendAsync<List<EnrolmentDto>>(HttpMethod.Get, $"api/events/{eventId}/enrolments");

    public async Task<ApiResult> UpdateEnrolmentStatusAsync(int enrolmentId, string status) =>
        await SendAsync<object>(HttpMethod.Put, $"api/enrolments/{enrolmentId}/status", JsonContent(new { status }), readBody: false);

    // ------------------------------------------------------------------ Results
    public Task<ApiResult<List<ResultRowDto>>> GetEventResultsAsync(int eventId) =>
        SendAsync<List<ResultRowDto>>(HttpMethod.Get, $"api/events/{eventId}/results");

    public async Task<ApiResult> CreateResultAsync(int enrolmentId, string? finishTime, int? position, bool published) =>
        await SendAsync<object>(HttpMethod.Post, "api/results",
            JsonContent(new { enrolmentId, finishTime, position, published }), readBody: false);

    public async Task<ApiResult> UpdateResultAsync(int resultId, string? finishTime, int? position, bool published) =>
        await SendAsync<object>(HttpMethod.Put, $"api/results/{resultId}",
            JsonContent(new { finishTime, position, published }), readBody: false);

    public Task<ApiResult<List<MyResultDto>>> GetMyResultsAsync() =>
        SendAsync<List<MyResultDto>>(HttpMethod.Get, "api/results/my");

    // ------------------------------------------------------------------ Files
    public async Task<ApiResult<DownloadedFile>> GetFileAsync(string blobName)
    {
        try
        {
            using var request = BuildRequest(HttpMethod.Get, $"api/files/{blobName}", null);
            using var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return ApiResult<DownloadedFile>.Failure((int)response.StatusCode, await ReadErrorAsync(response));

            var file = new DownloadedFile
            {
                Content = await response.Content.ReadAsByteArrayAsync(),
                ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream"
            };
            return ApiResult<DownloadedFile>.Ok(file);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Could not fetch file {BlobName} from the API", blobName);
            return ApiResult<DownloadedFile>.Failure(503, UnavailableMessage);
        }
    }

    // ------------------------------------------------------------------ Plumbing
    private const string UnavailableMessage = "The RaceDay service is currently unavailable. Please try again in a moment.";

    private static StringContent JsonContent(object body) =>
        new(JsonSerializer.Serialize(body, JsonOptions), System.Text.Encoding.UTF8, "application/json");

    private static MultipartFormDataContent FileContent(IFormFile file)
    {
        var form = new MultipartFormDataContent();
        var stream = file.OpenReadStream();
        var part = new StreamContent(stream);
        part.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
        form.Add(part, "file", Path.GetFileName(file.FileName));
        return form;
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string url, HttpContent? content)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };

        // Forward the API session cookie that belongs to the logged-in MVC user.
        var cookie = _accessor.HttpContext?.Session.GetString(SessionKeys.ApiCookie);
        if (!string.IsNullOrEmpty(cookie))
            request.Headers.TryAddWithoutValidation("Cookie", cookie);

        return request;
    }

    private static string? ExtractCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values)) return null;

        // Keep only "name=value" from each Set-Cookie header (drop path, httponly, etc.).
        var pairs = values.Select(v => v.Split(';')[0].Trim()).Where(v => v.Contains('='));
        var joined = string.Join("; ", pairs);
        return string.IsNullOrEmpty(joined) ? null : joined;
    }

    private async Task<ApiResult<T>> SendAsync<T>(
        HttpMethod method, string url, HttpContent? content = null,
        bool readBody = true, Action<HttpResponseMessage>? onResponse = null)
    {
        try
        {
            using var request = BuildRequest(method, url, content);
            using var response = await _http.SendAsync(request);
            onResponse?.Invoke(response);

            if (!response.IsSuccessStatusCode)
            {
                var error = await ReadErrorAsync(response);
                return ApiResult<T>.Failure((int)response.StatusCode, error);
            }

            T? data = default;
            if (readBody)
                data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);

            return ApiResult<T>.Ok(data, (int)response.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "RaceDay API call failed: {Method} {Url}", method, url);
            return ApiResult<T>.Failure(503, UnavailableMessage);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "RaceDay API returned unreadable data: {Method} {Url}", method, url);
            return ApiResult<T>.Failure(502, "We received an unexpected response from the RaceDay service. Please try again.");
        }
        finally
        {
            content?.Dispose();
        }
    }

    /// <summary>Reads { "message": "..." } from the API; falls back to a safe generic sentence.</summary>
    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var text = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                using var doc = JsonDocument.Parse(text);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (doc.RootElement.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String
                        && !string.IsNullOrWhiteSpace(message.GetString()))
                        return message.GetString()!;

                    if (doc.RootElement.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String
                        && (int)response.StatusCode < 500 && !string.IsNullOrWhiteSpace(title.GetString()))
                        return title.GetString()!;
                }
            }
        }
        catch (JsonException) { /* not JSON - use the generic message below */ }

        return (int)response.StatusCode switch
        {
            401 => "Your session has expired. Please log in again.",
            403 => "You do not have permission to do that.",
            404 => "We could not find what you were looking for.",
            >= 500 => "The RaceDay service ran into a problem. Please try again shortly.",
            _ => "Your request could not be completed. Please check the details and try again."
        };
    }
}
