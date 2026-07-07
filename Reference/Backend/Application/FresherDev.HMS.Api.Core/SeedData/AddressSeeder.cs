using FresherDev.HMS.EntityFramework;

namespace FresherDev.HMS.Api.Core.SeedData;

internal class AddressSeeder
{
    private readonly ApplicationDbContext context;

    private readonly bool isDevelopment;

    public AddressSeeder(ApplicationDbContext context, bool isDevelopment)
    {
        this.context = context;
        this.isDevelopment = isDevelopment;
    }

    public void SeedData()
    {
        if (!isDevelopment)
        {
            return;
        }

        AddNew("Ha Noi");
        AddNew("Hai Duong");
        AddNew("Hai Phong");
        AddNew("Hoa Binh");
        AddNew("Hung Yen");
        AddNew("Lai Chau");
        AddNew("Lao Cai");
        AddNew("Lang Son");

        this.context.SaveChanges();
    }

    private void AddNew(string name)
    {
        var address = new Address()
        {
            Name = name,
            Id = Guid.NewGuid(),
        };

        var existingAddress = this.context.Addresses.FirstOrDefault(p => p.Name == name);
        if (existingAddress == null)
        {
            this.context.Addresses.Add(address);
        }
    }
}