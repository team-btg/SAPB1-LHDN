using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http;
using System;
using Microsoft.Extensions.Configuration; // Required for IConfigurationBuilder
using Microsoft.Extensions.Logging;     // Required for ILoggingBuilder
using System.IO;                        // Required for Directory.GetCurrentDirectory()
using SAP_LHDN.Models;

namespace SAP_LHDN
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
        new HostBuilder()
            .ConfigureHostConfiguration(configHost =>
            {
                // NOTE: Requires Microsoft.Extensions.Configuration.EnvironmentVariables
                configHost.AddEnvironmentVariables(prefix: "DOTNET_");
            })
            .ConfigureAppConfiguration((hostContext, config) =>
            {
                config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);

                config.AddJsonFile($"{AppDomain.CurrentDomain.BaseDirectory}/appsettings.json", optional: true); 
            })
            .ConfigureLogging((hostContext, configLogging) =>
            {
                // FIX: Simplifying logging setup. Requires Microsoft.Extensions.Logging.Console/Debug/Configuration
                // In .NET 4.6.1, manually adding configuration/providers is necessary.
                configLogging.AddDebug();
                configLogging.AddConsole();

                // If you want appsettings.json to control log levels, you must configure it explicitly:
                var loggingSection = hostContext.Configuration.GetSection("Logging");
                if (loggingSection.Exists())
                {
                    configLogging.AddConfiguration(loggingSection);
                }
            })
            // MANDATORY for deployment as a Windows Service
            .UseWindowsService()
            .ConfigureServices((hostContext, services) =>
            {
                var configuration = hostContext.Configuration;
                var workerSettings = new WorkerSettings();

                string hanaConnStr = configuration.GetConnectionString("Hana"); 
                configuration.GetSection("WorkerSettings").Bind(workerSettings);

                string apiBaseUrl = workerSettings.ApiBaseUrl;

                if (string.IsNullOrEmpty(hanaConnStr))
                { 
                    throw new InvalidOperationException(
                        "The 'Hana' connection string is missing from appsettings.json or is empty. Cannot initialize HanaService.");
                }
                services.AddSingleton<HanaService>(provider =>
                { 
                    return new HanaService(hanaConnStr);
                });

                // Register HttpClient for dependency injection
                // We use AddSingleton since the HttpClient should be reused for performance
                services.AddSingleton<HttpClient>();

                // Register the core service (Updated from SalesInvoiceService)
                services.AddTransient<EInvoiceService>(provider =>
                {
                    // 1. Resolve ILogger dependency (required by EInvoiceService constructor)
                    var logger = provider.GetRequiredService<ILogger<EInvoiceService>>();

                    // 2. Resolve HttpClient dependency (required by EInvoiceService constructor)
                    var httpClient = provider.GetRequiredService<HttpClient>();

                    // 3. Instantiate EInvoiceService, passing the string variable (apiBaseUrl)
                    return new EInvoiceService(httpClient, logger, apiBaseUrl);
                });

                // Register the main worker loop as a hosted service
                services.AddHostedService<InvoicePollingWorker>();
            });
    }

}
