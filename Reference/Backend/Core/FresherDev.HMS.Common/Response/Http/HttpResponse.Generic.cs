using System.Net;

namespace FresherDev.HMS.Common;

public class HttpResponse<T> : Response<T>, IHttpResponse<T>
{
    public HttpStatusCode StatusCode { get; set; }

    public int? Code { get; set; }

    public static HttpResponse<T> Ok(T data, string? message = null, int? code = 200)
    {
        return new HttpResponse<T>()
        {
            IsSuccess = true,
            StatusCode = HttpStatusCode.OK,
            Code = code,
            Data = data,
            Message = message,
        };
    }

    public static HttpResponse<T> Created(T data, string? message = null, int? code = 201)
    {
        return new HttpResponse<T>()
        {
            IsSuccess = true,
            StatusCode = HttpStatusCode.Created,
            Code = code,
            Data = data,
            Message = message,
        };
    }

    public static HttpResponse<T> BadRequest(T data, string? message, int? code = 400)
    {
        return new HttpResponse<T>()
        {
            IsSuccess = false,
            StatusCode = HttpStatusCode.BadRequest,
            Code = code,
            Data = data,
            Message = message,
        };
    }

    public static HttpResponse<T> Unauthorized(string? message = null, int? code = 401)
    {
        return new HttpResponse<T>()
        {
            IsSuccess = false,
            StatusCode = HttpStatusCode.Unauthorized,
            Code = code,
            Message = message,
        };
    }

    public static HttpResponse<T> NotFound(T data, string? message = null, int? code = 404)
    {
        return new HttpResponse<T>()
        {
            IsSuccess = false,
            StatusCode = HttpStatusCode.NotFound,
            Code = code,
            Data = data,
            Message = message,
        };
    }
}
