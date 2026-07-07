using System.Net;

namespace FresherDev.HMS.Common;

public interface IHttpResponse : IResponse
{
    HttpStatusCode StatusCode { get; set; }

    int? Code { get; set; }
}

public interface IHttpResponse<T> : IHttpResponse, IResponse<T>
{
}
