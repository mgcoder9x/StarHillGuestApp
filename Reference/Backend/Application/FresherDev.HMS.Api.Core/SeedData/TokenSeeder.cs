using FresherDev.HMS.EntityFramework;

namespace FresherDev.HMS.Api.Core.SeedData;

internal class TokenSeeder
{
    private readonly ApplicationDbContext context;

    private readonly bool isDevelopment;

    public TokenSeeder(ApplicationDbContext context, bool isDevelopment)
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

        var user = this.context.Users.FirstOrDefault(p => p.Username == "luuquangict");
        if (user != null)
        {
            AddNew(user.Id);
            AddNew(user.Id);
            AddNew(user.Id);
        }

        this.context.SaveChanges();
    }

    private void AddNew(Guid userId)
    {
        var token = new Token()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Value = Guid.NewGuid().ToString("N"),
        };

        this.context.Tokens.Add(token);
    }
}