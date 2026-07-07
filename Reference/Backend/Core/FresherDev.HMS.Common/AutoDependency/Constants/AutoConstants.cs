namespace FresherDev.HMS.Common.AutoDependency;

public class AutoConstants
{
    /// <summary>
    /// Pattern dùng để xác định Interface có được đặt đúng chuẩn quy định hay không. 
    /// Từ đó xác định có thực hiện tự động Add dependency cho interface hay không
    /// </summary>
    public const string InterfacePattern = @"^I[A-Za-z0-9]*(Service|Repository|UseCase|Manager)$";

    public const string AppNamespace = "FresherDev.HMS";

    public static readonly List<string> IgnoreAssemblyNames = new List<string>
    {
        $"{AppNamespace}.EntityFramework",
        $"{AppNamespace}.Infrastructure",
        $"{AppNamespace}.JwtBearer",
        $"{AppNamespace}.MongoDb",
        $"{AppNamespace}.Common",
        $"{AppNamespace}.Auth",
    };
}