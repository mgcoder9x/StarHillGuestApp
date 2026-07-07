namespace ResortQr.SharedKernel.DependencyInjection;

/// <summary>Đánh dấu service đăng ký lifetime Scoped (DI convention qua Scrutor).</summary>
public interface IScopedService;

/// <summary>Đánh dấu service đăng ký lifetime Singleton.</summary>
public interface ISingletonService;

/// <summary>Đánh dấu service đăng ký lifetime Transient.</summary>
public interface ITransientService;
