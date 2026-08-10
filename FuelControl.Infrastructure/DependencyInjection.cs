using FuelControl.Infrastructure.Identity;
using FuelControl.Infrastructure.Persistence;
using FuelControl.Infrastructure.Services;
using FuelControl.Infrastructure.Services.Interfaces;
using FuelControl.Omnicomm.Authentication;
using FuelControl.Omnicomm.Configuration;
using FuelControl.Omnicomm.Http;
using FuelControl.Omnicomm.Reports;
using FuelControl.Omnicomm.Vehicles;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FuelControl.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<FuelControlDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection")));

        services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<FuelControlDbContext>()
            .AddDefaultTokenProviders();

        services.AddOmnicomm(configuration);

        return services;
    }

    public static IServiceCollection AddOmnicomm(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<OmnicommOptions>(
            configuration.GetSection(OmnicommOptions.SectionName));

        services.AddHttpClient<IOmnicommAuthenticator, OmnicommAuthenticator>(
            (sp, client) =>
            {
                var options = configuration
                    .GetSection(OmnicommOptions.SectionName)
                    .Get<OmnicommOptions>() ?? new OmnicommOptions();

                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(60);
            });

        services.AddHttpClient<IOmnicommApiClient, OmnicommApiClient>(
            (sp, client) =>
            {
                var options = configuration
                    .GetSection(OmnicommOptions.SectionName)
                    .Get<OmnicommOptions>() ?? new OmnicommOptions();

                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(60);
            });

        // Credentials из конфига
        services.AddSingleton(sp =>
        {
            var options = configuration
                .GetSection(OmnicommOptions.SectionName)
                .Get<OmnicommOptions>() ?? new OmnicommOptions();

            return new OmnicommCredentials(options.Login, options.Password);
        });

        services.AddScoped<IOmnicommVehicleClient, OmnicommVehicleClient>();
        services.AddScoped<IOmnicommReportClient, OmnicommReportClient>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IVehicleService, VehicleService>();
        services.AddScoped<IBranchService, BranchService>();
        services.AddScoped<IOmnicommBranchService, OmnicommBranchService>();
        services.AddScoped<
            IFuelingRecordAuthorizationService,
            FuelingRecordAuthorizationService>();
        services.AddScoped<DirectoryService>();
        services.AddScoped<FuelingRecordService>();
        return services;
    }
}