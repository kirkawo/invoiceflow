using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Application.Options;
using InvoiceFlow.Infrastructure.BackgroundJobs;
using InvoiceFlow.Infrastructure.Email;
using InvoiceFlow.Infrastructure.Persistence;
using InvoiceFlow.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));

        services.AddDbContext<InvoiceFlowDbContext>((sp, options) =>
        {
            var dbOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseNpgsql(dbOptions.ConnectionString);
        });
        services.Configure<ReminderOptions>(configuration.GetSection(ReminderOptions.SectionName));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<ReminderOptions>>().Value);

        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<EmailOptions>>().Value);

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentWorkspaceService, CurrentWorkspaceService>();

        services.AddScoped<IClientRepository, EfClientRepository>();
        services.AddScoped<IInvoiceRepository, EfInvoiceRepository>();
        services.AddScoped<IReminderRepository, EfReminderRepository>();
        services.AddScoped<IEmailSender>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<EmailOptions>>().Value;
            return string.IsNullOrWhiteSpace(options.SmtpHost)
                ? ActivatorUtilities.CreateInstance<ConsoleEmailSender>(sp)
                : ActivatorUtilities.CreateInstance<SmtpEmailSender>(sp);
        });
        services.AddHostedService<AutomaticReminderBackgroundService>();

        return services;
    }
}
