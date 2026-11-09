using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Services;

/// <summary>Every data operation in the MVC app goes through this interface - one method per API endpoint.</summary>
public interface IRaceDayApiClient
{
    // Auth
    Task<ApiResult> RegisterAsync(object request);
    Task<ApiResult<LoginResult>> LoginAsync(string email, string password);
    Task<ApiResult> LogoutAsync();

    // Profile
    Task<ApiResult<UserDto>> GetProfileAsync();
    Task<ApiResult<UserDto>> UpdateProfileAsync(string firstName, string lastName, string? phone);
    Task<ApiResult<UserDto>> UploadProfilePictureAsync(IFormFile file);

    // Events
    Task<ApiResult<List<EventSummaryDto>>> GetEventsAsync(EventFilterModel filter);
    Task<ApiResult<List<EventSummaryDto>>> GetMyEventsAsync();
    Task<ApiResult<EventDetailDto>> GetEventAsync(int id);
    Task<ApiResult<EventDetailDto>> CreateEventAsync(object request);
    Task<ApiResult<EventDetailDto>> UpdateEventAsync(int id, object request);
    Task<ApiResult> DeleteEventAsync(int id);
    Task<ApiResult<EventDetailDto>> UploadEventBannerAsync(int id, IFormFile file);

    // Categories
    Task<ApiResult<List<CategoryDto>>> GetCategoriesAsync(int eventId);
    Task<ApiResult<CategoryDto>> CreateCategoryAsync(int eventId, object request);
    Task<ApiResult<CategoryDto>> UpdateCategoryAsync(int categoryId, object request);
    Task<ApiResult> DeleteCategoryAsync(int categoryId);

    // Enrolments
    Task<ApiResult<MyEnrolmentDto>> EnrolAsync(int eventId, int categoryId);
    Task<ApiResult<List<MyEnrolmentDto>>> GetMyEnrolmentsAsync();
    Task<ApiResult> CancelEnrolmentAsync(int enrolmentId);
    Task<ApiResult<List<EnrolmentDto>>> GetEventEnrolmentsAsync(int eventId);
    Task<ApiResult> UpdateEnrolmentStatusAsync(int enrolmentId, string status);

    // Results
    Task<ApiResult<List<ResultRowDto>>> GetEventResultsAsync(int eventId);
    Task<ApiResult> CreateResultAsync(int enrolmentId, string? finishTime, int? position, bool published);
    Task<ApiResult> UpdateResultAsync(int resultId, string? finishTime, int? position, bool published);
    Task<ApiResult<List<MyResultDto>>> GetMyResultsAsync();

    // Files (served from Azure Blob Storage by the API)
    Task<ApiResult<DownloadedFile>> GetFileAsync(string blobName);
}
