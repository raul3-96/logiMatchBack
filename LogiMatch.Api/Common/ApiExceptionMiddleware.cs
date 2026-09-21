using LogiMatch.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace LogiMatch.Api.Common;

public class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ApiExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            await WriteProblem(
                context,
                StatusCodes.Status404NotFound,
                "Resource not found.",
                ex.Message);
        }
        catch (ValidationException ex)
        {
            await WriteValidationProblem(
                context,
                ex.Message,
                ex.Errors);
        }
        catch (ConflictException ex)
        {
            await WriteProblem(
                context,
                StatusCodes.Status409Conflict,
                "Conflict.",
                ex.Message);
        }
        catch (UnauthorizedException ex)
        {
            await WriteProblem(
                context,
                StatusCodes.Status401Unauthorized,
                "Not Authorized.",
                ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            await WriteProblem(
                context,
                StatusCodes.Status400BadRequest,
                "Request could not be processed.",
                ex.Message);
        }
        catch (BadHttpRequestException ex)
        {
            await WriteProblem(
                context,
                StatusCodes.Status400BadRequest,
                "Invalid request.",
                ex.Message);
        }
        catch (Exception)
        {
            await WriteProblem(
                context,
                StatusCodes.Status500InternalServerError,
                "Unexpected error.",
                "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblem(
        HttpContext context,
        int statusCode,
        string title,
        string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.com/{statusCode}",
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;

        await context.Response.WriteAsJsonAsync(problem);
    }

    private static async Task WriteValidationProblem(
        HttpContext context,
        string detail,
        IReadOnlyDictionary<string, string[]> errors)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";

        var problem = new ValidationProblemDetails(
            errors.ToDictionary(x => x.Key, x => x.Value))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed.",
            Detail = detail,
            Type = "https://httpstatuses.com/400",
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;

        await context.Response.WriteAsJsonAsync(problem);
    }
}