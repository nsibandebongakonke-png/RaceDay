using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Api.Dtos;
using RaceDay.Api.Services;

namespace RaceDay.Api.Infrastructure;

/// <summary>Turns blob-storage exceptions into clean JSON errors instead of 500 pages.</summary>
public class StorageExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case InvalidImageException ex:
                context.Result = new ObjectResult(new ApiError(ex.Message)) { StatusCode = StatusCodes.Status400BadRequest };
                context.ExceptionHandled = true;
                break;
            case StorageUnavailableException ex:
                context.Result = new ObjectResult(new ApiError(ex.Message)) { StatusCode = StatusCodes.Status503ServiceUnavailable };
                context.ExceptionHandled = true;
                break;
        }
    }
}
