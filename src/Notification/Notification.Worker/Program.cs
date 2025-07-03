namespace Notification.Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            //Email Configuration 
            builder.Services.Configure<EmailSettings>(
                builder.Configuration.GetSection("EmailSettings"));

            builder.Services.AddTransient<IEmailService, EmailService>();


            builder.Services.AddHostedService<Worker>();

            var host = builder.Build();
            host.Run();
        }
    }
}