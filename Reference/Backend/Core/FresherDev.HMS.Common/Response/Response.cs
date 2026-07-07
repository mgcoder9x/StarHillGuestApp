namespace FresherDev.HMS.Common;

public class Response : IResponse
{
    public bool IsSuccess { get; set; }

    public string? Message { get; set; }
}

public class Response<T> : Response, IResponse<T>
{
    public T? Data { get; set; }
}