using Mapster;
using Microsoft.AspNetCore.Authentication.Cookies;
using Tibr.Infrastructure;

namespace Tibr.MVC
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddInfrastructure(builder.Configuration);

            //// Add MediatR for CQRS pattern
            //builder.Services.AddMediatR(cfg =>
            //    cfg.RegisterServicesFromAssembly(typeof(Tibr.Application.Services.Auth.RegisterCommand).Assembly));


            //builder.Services.AddApplicationServices();

            TypeAdapterConfig.GlobalSettings.Scan(
                        typeof(Tibr.Application.Mappers.ProductMappingConfig).Assembly,
                        typeof(Tibr.MVC.Mapping.DashboardMappingConfig).Assembly);

            // Add HttpClientFactory
            builder.Services.AddHttpClient();

            // Add Cookie Authentication
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/AdminAuth/Login";
                    options.LogoutPath = "/AdminAuth/Logout";
                    options.AccessDeniedPath = "/AdminAuth/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromDays(1);
                    options.SlidingExpiration = true;
                });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=AdminAuth}/{action=Login}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
