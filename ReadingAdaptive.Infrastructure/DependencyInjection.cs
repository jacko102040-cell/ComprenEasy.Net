using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReadingAdaptive.Application.AcademicContent.Interfaces;
using ReadingAdaptive.Application.AcademicFlow.Interfaces;
using ReadingAdaptive.Application.Auth.Interfaces;
using ReadingAdaptive.Application.Adaptive.Interfaces;
using ReadingAdaptive.Application.Catalogs.Interfaces;
using ReadingAdaptive.Application.Evaluations.Interfaces;
using ReadingAdaptive.Application.Readings.Interfaces;
using ReadingAdaptive.Infrastructure.Configuration;
using ReadingAdaptive.Application.TeacherPanel.Interfaces;
using ReadingAdaptive.ML.Options;
using ReadingAdaptive.ML.Services;
using ReadingAdaptive.Infrastructure.Persistence;
using ReadingAdaptive.Infrastructure.Security;
using ReadingAdaptive.Infrastructure.Services;

namespace ReadingAdaptive.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
        }

        services.AddDbContext<ReadingAdaptiveDbContext>(options =>
            options.UseSqlServer(connectionString));
        services.Configure<AcademicFlowOptions>(configuration.GetSection(AcademicFlowOptions.SectionName));
        services.Configure<AdaptiveMlOptions>(configuration.GetSection(AdaptiveMlOptions.SectionName));

        services.AddScoped<IPasswordHashService, Pbkdf2PasswordHashService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAcademicContentService, AcademicContentService>();
        services.AddScoped<IAcademicFlowService, AcademicFlowService>();
        services.AddSingleton<IAdaptiveRecommendationPredictionService, AdaptiveRecommendationPredictionService>();
        services.AddScoped<IAdaptiveRecommendationService, AdaptiveRecommendationService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IEvaluationService, EvaluationService>();
        services.AddScoped<IReadingService, ReadingService>();
        services.AddScoped<ITeacherPanelService, TeacherPanelService>();

        return services;
    }
}
