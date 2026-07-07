using System.Net;

namespace FresherDev.HMS.Common;

public class HttpResponse : Response, IHttpResponse
{
    public HttpStatusCode StatusCode { get; set; }

    public int? Code { get; set; }

    public static HttpResponse Ok(string? message = null, int? code = 200)
    {
        return new HttpResponse()
        {
            IsSuccess = true,
            StatusCode = HttpStatusCode.OK,
            Code = code,
            Message = message,
        };
    }

    public static HttpResponse Created(string? message = null, int? code = 201)
    {
        return new HttpResponse()
        {
            IsSuccess = true,
            StatusCode = HttpStatusCode.Created,
            Code = code,
            Message = message,
        };
    }

    public static HttpResponse BadRequest(string? message, int? code = 400)
    {
        return new HttpResponse()
        {
            IsSuccess = false,
            StatusCode = HttpStatusCode.BadRequest,
            Code = code,
            Message = message,
        };
    }

    public static HttpResponse Unauthorized(string? message = null, int? code = 401)
    {
        return new HttpResponse()
        {
            IsSuccess = false,
            StatusCode = HttpStatusCode.Unauthorized,
            Code = code,
            Message = message,
        };
    }

    public static HttpResponse NotFound(string? message = null, int? code = 404)
    {
        return new HttpResponse()
        {
            StatusCode = HttpStatusCode.NotFound,
            Code = code,
            IsSuccess = false,
            Message = message,
        };
    }
}