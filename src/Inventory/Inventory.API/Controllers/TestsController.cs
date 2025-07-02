using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestsController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public TestsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        [HttpGet("DateTime")]
        public IActionResult Get()
        {
            var result = new
            {
                HostOS = GetOperatingSystemInfo(),
                Utc = new
                {
                    Zone = "UTC",
                    Date = DateTime.UtcNow.ToLongDateString(),
                    Time = DateTime.UtcNow.ToLongTimeString()
                },
            };

            return Ok(result);
        }


        [HttpGet("EnvironmentVariables")]
        public IActionResult EnvironmentVariables()
        {
            var rabbitHost = _configuration["RabbitMQ:Host"];
            var rabbitUser = _configuration["RabbitMQ:Username"];
            var rabbitPass = _configuration["RabbitMQ:Password"];
            var SmtpServer = _configuration["EmailSettings:SmtpServer"];
            var Port = _configuration["EmailSettings:Port"];
            var SenderName = _configuration["EmailSettings:SenderName"];
            var SenderEmail = _configuration["EmailSettings:SenderEmail"];
            var To = _configuration["EmailSettings:To"];
            var Username = _configuration["EmailSettings:Username"];
            var Password = _configuration["EmailSettings:Password"];


            return Ok(new
            {
                rabbitHost,
                rabbitUser,
                rabbitPass,
                SmtpServer,
                Port,
                SenderName,
                SenderEmail,
                To,
                Username,
                Password,
            });
        }

        private string GetOperatingSystem()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "Windows";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "Linux";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "macOS";
            return "Desconocido";
        }

        private object GetOperatingSystemInfo()
        {
            return new
            {
                Platform = GetOperatingSystem(),
                Description = RuntimeInformation.OSDescription,
                Version = Environment.OSVersion.VersionString
            };
        }

    }
}
