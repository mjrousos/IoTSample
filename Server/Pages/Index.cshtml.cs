using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Server.IoTHub;

namespace Server.Pages
{
    public class IndexModel : PageModel
    {
        private const string DeviceIDConfigSection = "IoTDevices";
        private readonly ILogger<IndexModel> _logger;
        private readonly IoTClient _iotClient;
        private readonly IConfiguration _configuration;

        [FromQuery]
        public string DeviceId { get; set; }

        public string[] AvailableDevices { get; set; }
        public Dictionary<string, string> DeviceUsers { get; set; }


        public IndexModel(ILogger<IndexModel> logger, IConfiguration configuration, IoTClient iotClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _iotClient = iotClient ?? throw new ArgumentNullException(nameof(iotClient));
        }

        public async Task OnGet()
        {
            // Really this ought to be a tag because it's not something that the server and client both have a say in.
            // And it ought to be compared againts a user's security groups rather than against their name.
            var deviceQuery = $"SELECT * FROM devices WHERE properties.desired.owners IN ['{User.Identity.Name}']";
            var devices = _iotClient.RegistryManager.CreateQuery(deviceQuery);
            AvailableDevices = (await devices.GetNextAsTwinAsync()).Select(device => device.DeviceId).ToArray();

            if (!string.IsNullOrWhiteSpace(DeviceId))
            {
                DeviceUsers = await _iotClient.GetDeviceUsersAsync(DeviceId);
            }
        }
    }
}
