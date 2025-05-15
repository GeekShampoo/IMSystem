using IMSystem.Protocol.Common;
using IMSystem.Protocol.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FluentValidation;
using IMSystem.Server.Domain.Exceptions;
using System.Linq;
using IMSystem.Server.Web.Common; // ApiErrorFactory is here, but we'll use ProblemDetails directly for DomainException
using Microsoft.AspNetCore.Mvc; // For ProblemDetails
 
namespace IMSystem.Server.Web.Middleware
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
        private readonly IHostEnvironment _env;
        // private readonly ApiErrorFactory _apiErrorFactory; // Assuming it's injected or static. For this change, we might not need it for DomainException.

        public GlobalExceptionHandlerMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlerMiddleware> logger,
            IHostEnvironment env)
            // ApiErrorFactory apiErrorFactory) // If it were injected
        {
            _next = next;
            _logger = logger;
            _env = env;
            // _apiErrorFactory = apiErrorFactory;
        }
 
        public async Task InvokeAsync(HttpContext httpContext) // Renamed context to httpContext for clarity
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception has occurred: {Message}", ex.Message);
 
                httpContext.Response.ContentType = "application/problem+json"; // Use application/problem+json for ProblemDetails
                // ApiErrorResponse response; // We will use ProblemDetails for DomainException
 
                switch (ex)
                {
                    case ValidationException validationException:
                        var validationErrors = new Dictionary<string, string[]>();
                        foreach (var error in validationException.Errors)
                        {
                            if (validationErrors.ContainsKey(error.PropertyName))
                            {
                                var errors = validationErrors[error.PropertyName].ToList();
                                errors.Add(error.ErrorMessage);
                                validationErrors[error.PropertyName] = errors.ToArray();
                            }
                            else
                            {
                                validationErrors[error.PropertyName] = new[] { error.ErrorMessage };
                            }
                        }
                        // Still using ApiErrorFactory for ValidationException as per existing structure
                        var validationResponse = ApiErrorFactory.Create(ApiErrorCode.ValidationFailed,
                            detail: "One or more validation errors occurred",
                            traceId: httpContext.TraceIdentifier,
                            instance: httpContext.Request.Path);
                        validationResponse.Errors = validationErrors;
                        httpContext.Response.StatusCode = validationResponse.StatusCode;
                        await httpContext.Response.WriteAsJsonAsync(validationResponse, cancellationToken: httpContext.RequestAborted);
                        break;
                    
                    case EntityNotFoundException entityNotFoundException:
                         var notFoundResponse = ApiErrorFactory.Create(ApiErrorCode.ResourceNotFound,
                            detail: entityNotFoundException.Message,
                            traceId: httpContext.TraceIdentifier,
                            instance: httpContext.Request.Path);
                        httpContext.Response.StatusCode = notFoundResponse.StatusCode;
                        await httpContext.Response.WriteAsJsonAsync(notFoundResponse, cancellationToken: httpContext.RequestAborted);
                        break;
 
                    case DomainException domainEx: // Changed variable name to domainEx
                        _logger.LogWarning(domainEx, "Domain exception occurred: {Message}", domainEx.Message);

                        var problemDetails = new ProblemDetails
                        {
                            Instance = httpContext.Request.Path,
                            Extensions = { { "traceId", httpContext.TraceIdentifier } }
                        };
                        
                        // Use ApiErrorCode from the exception
                        var errorCode = domainEx.ErrorCode;

                        switch (errorCode)
                        {
                            case ApiErrorCode.AccessDenied:
                                problemDetails.Status = StatusCodes.Status403Forbidden;
                                problemDetails.Title = "Access Denied.";
                                break;
                            case ApiErrorCode.ResourceNotFound:
                                problemDetails.Status = StatusCodes.Status404NotFound;
                                problemDetails.Title = "Resource Not Found.";
                                break;
                            case ApiErrorCode.ValidationFailed: // For domain-level validation not caught by FluentValidation
                                problemDetails.Status = StatusCodes.Status422UnprocessableEntity;
                                problemDetails.Title = "Validation Failed.";
                                break;
                            case ApiErrorCode.BusinessRuleViolation:
                            case ApiErrorCode.DomainRuleViolated: // Catch-all for general domain rule violations
                                problemDetails.Status = StatusCodes.Status400BadRequest; // Or 409 Conflict
                                problemDetails.Title = "Business Rule Violation.";
                                break;
                            case ApiErrorCode.AlreadyFriends: // Corrected from FriendshipAlreadyExists
                                problemDetails.Status = StatusCodes.Status409Conflict;
                                problemDetails.Title = "Friendship already exists.";
                                break;
                            case ApiErrorCode.UserAlreadyBlocked:
                                problemDetails.Status = StatusCodes.Status409Conflict;
                                problemDetails.Title = "User is already blocked.";
                                break;
                            case ApiErrorCode.CannotBlockSelf:
                                problemDetails.Status = StatusCodes.Status400BadRequest;
                                problemDetails.Title = "Cannot block yourself.";
                                break;
                             case ApiErrorCode.GroupMemberLimitReached: // GroupIsFull
                                problemDetails.Status = StatusCodes.Status409Conflict;
                                problemDetails.Title = "Group is full.";
                                break;
                            case ApiErrorCode.InvalidOperation:
                                problemDetails.Status = StatusCodes.Status400BadRequest;
                                problemDetails.Title = "Invalid Operation.";
                                break;
                            // Add more cases for other specific ApiErrorCodes as needed
                            default:
                                problemDetails.Status = StatusCodes.Status400BadRequest;
                                problemDetails.Title = "A domain error occurred.";
                                break;
                        }
                        problemDetails.Detail = domainEx.Message; // Keep original message as detail
                        problemDetails.Extensions["errorCode"] = errorCode.ToString();

                        httpContext.Response.StatusCode = problemDetails.Status.Value;
                        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken: httpContext.RequestAborted);
                        break;
                        
                    case UnauthorizedAccessException unauthorizedEx:
                        var unauthorizedResponse = ApiErrorFactory.Create(ApiErrorCode.AccessDenied,
                            detail: unauthorizedEx.Message,
                            traceId: httpContext.TraceIdentifier,
                            instance: httpContext.Request.Path);
                        httpContext.Response.StatusCode = unauthorizedResponse.StatusCode;
                        await httpContext.Response.WriteAsJsonAsync(unauthorizedResponse, cancellationToken: httpContext.RequestAborted);
                        break;
 
                    default:
                        var serverErrorResponse = ApiErrorFactory.Create(ApiErrorCode.ServerError,
                            traceId: httpContext.TraceIdentifier,
                            instance: httpContext.Request.Path);
                        if (_env.IsDevelopment())
                        {
                            serverErrorResponse.Detail = ex.ToString();
                        }
                        else
                        {
                            serverErrorResponse.Detail = "An unexpected error occurred. Please try again later.";
                        }
                        httpContext.Response.StatusCode = serverErrorResponse.StatusCode;
                        await httpContext.Response.WriteAsJsonAsync(serverErrorResponse, cancellationToken: httpContext.RequestAborted);
                        break;
                }
 
                // Removed generic serialization block as each case now handles its own response writing
                // var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
                // {
                //     PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                //     DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                //     WriteIndented = _env.IsDevelopment()
                // });
                
                // await httpContext.Response.WriteAsync(jsonResponse);
            }
        }
    }
}