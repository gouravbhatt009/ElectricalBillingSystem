using ElectricalBilling.Services;
using Microsoft.EntityFrameworkCore;

namespace ElectricalBilling.Data
{
  
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var authService = services.GetRequiredService<IAuthService>();
            var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();

            await context.Database.MigrateAsync();

            if (!await context.Users.AnyAsync())
            {

                var seedUsername = configuration["SeedAdmin:Username"] ?? "lalitbhatt";
                var seedEmail = configuration["SeedAdmin:Email"] ?? "liladharashu378@gmail.com";
                var seedPassword = configuration["SeedAdmin:Password"] ?? "lalitbhatt@123";

                await authService.CreateUserAsync("Leeladhar Bhatt", seedUsername, seedEmail, seedPassword);

                logger.LogWarning(
                    "Seeded initial admin account '{Username}'. Please log in and change the password immediately.",
                    seedUsername);
            }
        }
    }
}
