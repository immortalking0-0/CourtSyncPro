using CourtSyncPro.Data;
using CourtSyncPro.Hubs;                          // ← ADD THIS
using CourtSyncPro.Models.AI.Services;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace CourtSyncPro
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.SetData("DataDirectory",
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory));

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();
            builder.Services.AddSignalR();            // ← ADD THIS

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(60);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddHttpClient<GeminiService>();
            builder.Services.AddScoped<GeminiService>();
            builder.Services.AddScoped<BookingAiService>();

            var app = builder.Build();

            var pkCulture = new CultureInfo("en-PK");
            pkCulture.NumberFormat.CurrencySymbol = "Rs";

            var localizationOptions = new RequestLocalizationOptions
            {
                DefaultRequestCulture =
                    new Microsoft.AspNetCore.Localization.RequestCulture(pkCulture),
                SupportedCultures = new List<CultureInfo> { pkCulture },
                SupportedUICultures = new List<CultureInfo> { pkCulture }
            };

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseSession();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapHub<BookingHub>("/bookingHub");    // ← ADD THIS

            app.Run();
        }
    }
}