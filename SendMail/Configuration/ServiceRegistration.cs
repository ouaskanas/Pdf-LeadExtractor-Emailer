using SendMail.Services.Impl;
using SendMail.Services.IServices;

namespace SendMail.Configuration
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddServiceRegistration(this IServiceCollection services)
        {
            services.AddScoped<IEmailSending, EmailSending>();
            services.AddScoped<IEmailValidator, EmailValidator>();
            services.AddScoped<IPdfSerializer, PdfParserService>();
            return services;
        }
    }
}
