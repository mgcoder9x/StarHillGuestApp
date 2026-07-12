using RabbitMQ.Client;

namespace Adapters.Messaging.RabbitMq;

/// <summary>
/// Nguồn DUY NHẤT dựng <see cref="ConnectionFactory"/> từ <see cref="RabbitMqOptions"/> — publisher, consumer VÀ
/// health-check đều dùng chung. Trước đây mỗi nơi tự <c>new ConnectionFactory { ... }</c> (trùng 2 chỗ, sắp 3) →
/// dễ drift khi đổi credential/TLS/endpoint. Gom một chỗ: đổi cách kết nối chỉ sửa TẠI ĐÂY (fix tận gốc DRY).
/// </summary>
internal static class RabbitMqConnectionFactory
{
    public static ConnectionFactory Create(RabbitMqOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new ConnectionFactory
        {
            HostName = options.HostName,
            Port = options.Port,
            UserName = options.UserName,
            Password = options.Password,
            VirtualHost = options.VirtualHost,
        };
    }
}
