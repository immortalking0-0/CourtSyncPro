using CourtSyncPro.Data;
using CourtSyncPro.Hubs;
using CourtSyncPro.Models.AI.Services;
using CourtSyncPro.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

using System.Globalization;
using System.Security.Claims;

namespace CourtSyncPro
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.SetData(
                "DataDirectory",
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory)
            );

            var builder = WebApplication.CreateBuilder(args);

            // =========================
            // SERVICES
            // =========================

            builder.Services.AddControllersWithViews();

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection")));

            // Session
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(2);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // SignalR
            builder.Services.AddSignalR();

            // AI Services
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddHttpClient<GeminiService>();
            builder.Services.AddSingleton<DynamicPricingService>();
            builder.Services.AddScoped<BookingAiService>();

            var app = builder.Build();

            // =========================
            // LOCALIZATION
            // =========================

            var pkCulture = new CultureInfo("en-PK");
            pkCulture.NumberFormat.CurrencySymbol = "Rs";

            var localizationOptions = new RequestLocalizationOptions
            {
                DefaultRequestCulture =
                    new RequestCulture(pkCulture),

                SupportedCultures =
                    new List<CultureInfo> { pkCulture },

                SupportedUICultures =
                    new List<CultureInfo> { pkCulture }
            };

            app.UseRequestLocalization(localizationOptions);

            // =========================
            // MIDDLEWARE
            // =========================

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseSession();

            app.UseAuthorization();

            // =========================
            // CHAT HUB SESSION USER
            // =========================

            _ = app.Use(async (context, next) =>
            {
                try
                {
                    if (context.Request.Path.StartsWithSegments("/chatHub"))
                    {
                        var userId = context.Session.GetInt32("UserId");

                        if (userId.HasValue)
                        {
                            var identity = new ClaimsIdentity();

                            identity.AddClaim(
                                new Claim("UserId", userId.Value.ToString()));

                            context.User =
                                new ClaimsPrincipal(identity);
                        }
                    }

                    await next();
                }
                catch (FormatException fex)
                {
                    Console.Error.WriteLine(
                        $"Formatting error in middleware: {fex.Message}");

                    context.Response.StatusCode = 400;

                    await context.Response.WriteAsync(
                        "Invalid request format.");
                }
            });

            // =========================
            // ROUTES
            // =========================

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // SignalR Hubs
            app.MapHub<BookingHub>("/bookingHub");

            app.MapHub<ChatHub>("/chatHub");

            app.Run();
        }
    }
}