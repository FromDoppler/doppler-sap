using Doppler.Sap.DopplerSecurity;
using Doppler.Sap.Factory;
using Doppler.Sap.Models;
using Doppler.Sap.Services;
using Doppler.Sap.Utils;
using Doppler.Sap.Validations.Billing;
using Doppler.Sap.Validations.BusinessPartner;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;

namespace Doppler.Sap
{
    [ExcludeFromCodeCoverage]
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers()
                .AddJsonOptions(options => { options.JsonSerializerOptions.IgnoreNullValues = true; });

            services.AddSwaggerGen(c =>
            {
                c.EnableAnnotations();
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Doppler SAP API",
                    Version = "v1",
                    Description = "API for Doppler SAP"
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme (Example: 'Bearer 12345abcdef')",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            services.AddDopplerSecurity();
            services.AddCors();

            services.AddHttpClient("", c => { })
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                    UseCookies = false
                });
            services.AddSapServices();

            var timezoneConfig = new TimeZoneConfigurations
            {
                InvoicesTimeZone = TimeZoneHelper.GetTimeZoneByOperativeSystem(Configuration["SapConfig:InvoicesTimeZone"])
            };
            services.AddSingleton(timezoneConfig);

            services.AddSingleton<IQueuingService, QueuingService>();
            services.AddTransient<ISapService, SapService>();

            services.Configure<SapConfig>(Configuration.GetSection(nameof(SapConfig)));

            services.AddTransient<SetCurrencyRateHandler>();
            services.AddTransient<BillingRequestHandler>();
            services.AddTransient<CreditNoteHandler>();
            services.AddTransient<CreateOrUpdateBusinessPartnerHandler>();
            services.AddTransient<ISapTaskFactory, SapTaskFactory>();
            services.AddTransient<ISapServiceSettingsFactory, SapServiceSettingsFactory>();
            services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
            services.AddTransient<ISlackService, SlackService>();
            services.AddTransient<ITestSapService, TestSapService>();

            //Create the MapperFactory and also initializes the mappers
            services.AddSapMappers();

            services.AddSapBillingItems();

            //Validators
            services.AddTransient<IBillingValidation, BillingForArValidation>();
            services.AddTransient<IBillingValidation, BillingForUsValidation>();
            services.AddTransient<IBusinessPartnerValidation, BusinessPartnerForArValidation>();
            services.AddTransient<IBusinessPartnerValidation, BusinessPartnerForUsValidation>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseStaticFiles();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseCors(policy => policy
                .SetIsOriginAllowed(isOriginAllowed: _ => true)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials());

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // Swagger is disabled for int QA and prod because need set up for a reverse proxy
            if (env.IsDevelopment())
            {
                app.UseSwagger();

                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("v1/swagger.json", "Doppler SAP API V1");
                });
            }
        }
    }
}
