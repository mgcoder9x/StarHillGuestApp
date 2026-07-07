namespace FresherDev.HMS.Common;

public interface IResponse
{
    bool IsSuccess { get; set; }

    string? Message { get; set; }
}

public interface IResponse<T> : IResponse
{
    T? Data { get; set; }
}