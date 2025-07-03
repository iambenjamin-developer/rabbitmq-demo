using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestsController : ControllerBase
    {


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
