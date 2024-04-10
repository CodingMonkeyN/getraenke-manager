﻿using System.Text;
using BevMan.Application.Common.Interfaces;
using BevMan.Domain.Constants;
using BevMan.Infrastructure.Data;
using BevMan.Infrastructure.Data.Interceptors;
using BevMan.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("DefaultConnection");

        Guard.Against.Null(connectionString, message: "Connection string 'DefaultConnection' not found.");

        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();
        services.AddOptionsWithValidateOnStart<SupabaseOptions>("supabase");

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());

            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        services.AddSingleton(TimeProvider.System);

        services.AddSupabaseAuth();

        services.AddAuthorization(options =>
            options.AddPolicy(Policies.CanPurge, policy => policy.RequireRole(Roles.Administrator)));

        return services;
    }

    public static IServiceCollection AddSupabaseAuth(this IServiceCollection services)
    {
        services.AddOptions<SupabaseOptions>()
            .BindConfiguration("Supabase")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        SupabaseOptions supabaseOptions = serviceProvider.GetRequiredService<IOptions<SupabaseOptions>>().Value;
        SymmetricSecurityKey supasbaseSignatureKey = new(Encoding.UTF8.GetBytes(supabaseOptions.JwtSecret));
        string validIssuer = $"https://{supabaseOptions.ProjectName}.supabase.co/auth/v1";
        string[] validAudiences = { "authenticated" };

        services.AddAuthentication().AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = supasbaseSignatureKey,
                ValidAudiences = validAudiences,
                ValidIssuer = validIssuer
            };
        });
        return services;
    }
}
