using Ims.Api.Contracts;

namespace Ims.Api.Http;

public static class ServiceResultHttpExtensions
{
    public static IResult ToHttpResult(this ServiceResult result)
    {
        if (result.File is { } file)
            return Results.File(file.Contents, file.ContentType, file.FileName);
        if (result.Stream is { } stream)
            return Results.Stream(stream.Write, stream.ContentType, stream.FileName);
        if (result.Body is ApiError error && result.StatusCode >= 400)
            return new LocalizedErrorResult(result.StatusCode, error);
        return result.StatusCode switch
        {
            StatusCodes.Status200OK => Results.Ok(result.Body),
            StatusCodes.Status201Created => Results.Created(result.Location, result.Body),
            StatusCodes.Status204NoContent => Results.NoContent(),
            StatusCodes.Status400BadRequest => Results.BadRequest(result.Body),
            StatusCodes.Status404NotFound => Results.NotFound(result.Body),
            StatusCodes.Status409Conflict => Results.Conflict(result.Body),
            _ => Results.Json(result.Body, statusCode: result.StatusCode),
        };
    }
}
