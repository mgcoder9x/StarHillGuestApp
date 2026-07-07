using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace FresherDev.HMS.Common;

public static class HttpResponseExtensions
{
    public static IActionResult ToActionResult(this IHttpResponse response)
    {
        switch (response.StatusCode)
        {
            case HttpStatusCode.OK:
                return ResponseBuilder.Ok(response);

            case HttpStatusCode.Created:
                return ResponseBuilder.Created(response);

            case HttpStatusCode.NotFound:
                return ResponseBuilder.NotFound(response);

            case HttpStatusCode.BadRequest:
                return ResponseBuilder.BadRequest(response);

            default:
                throw new Exception($"Invalid status code: {response.StatusCode}");
        }
    }

    private class ResponseBuilder
    {
        public static OkObjectResult Ok<T>(T response)
        {
            return new OkObjectResult(response);
        }

        public static NotFoundObjectResult NotFound<T>(T response)
        {
            return new NotFoundObjectResult(response);
        }

        public static BadRequestObjectResult BadRequest<T>(T response)
        {
            return new BadRequestObjectResult(response);
        }

        public static CreatedResult Created<T>(T response, string? path = null)
        {
            return new CreatedResult(path, response);
        }
    }
}

