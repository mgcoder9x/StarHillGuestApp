using FresherDev.HMS.EntityFramework;

namespace FresherDev.HMS.Api.Core.SeedData;

internal class UserSeeder
{
    private readonly ApplicationDbContext context;
    private readonly bool isDevelopment;

    public UserSeeder(ApplicationDbContext context, bool isDevelopment)
    {
        this.context = context;
        this.isDevelopment = isDevelopment;
    }

    public void SeedData()
    {
        AddNew(User.MasterName);

        if (isDevelopment)
        {
            var address = this.context.Addresses.FirstOrDefault(p => p.Name == "Ha Noi");
            if (address == null)
            {
                throw new Exception("Not found address");
            }

            AddNew("luuquangict", address.Id);
            AddNew("user1", address.Id);
            AddNew("user2", address.Id);
            AddNew("user3", address.Id);
            AddNew("user4", address.Id);
        }

        this.context.SaveChanges();
    }

    private void AddNew(string username, Guid? addressId = null)
    {
        var user = new User()
        {
            Username = username,
            Email = $"{username}@example.com",
            AddressId = addressId,
            Password = "123456",
        };

        var existingUser = this.context.Users.FirstOrDefault(p => p.Username == username);
        if (existingUser == null)
        {
            this.context.Users.Add(user);
        }
    }
}