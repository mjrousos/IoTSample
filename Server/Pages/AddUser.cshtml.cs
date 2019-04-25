using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using IoTSample.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Server.IoTHub;

namespace Server.Pages
{
    public class AddUserModel : PageModel
    {
        private readonly ILogger<AddUserModel> _logger;
        private readonly IoTClient _iotClient;

        public AddUserModel(ILogger<AddUserModel> logger, IConfiguration configuration, IoTClient iotClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _iotClient = iotClient ?? throw new ArgumentNullException(nameof(iotClient));
        }

        [BindProperty]
        public InputModel Input { get; set; }

        [FromRoute]
        public string DeviceId { get; set; }

        public void OnGet()
        {
            
        }

        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation("Attempting to add user {userName}", Input.Username);

            var userId = await _iotClient.AddUserAsync(
                DeviceId,
                new AddUserRequest
                {
                    Password = Input.Password,
                    UserName = Input.Username
                });

            if (userId == null)
            {
                _logger.LogWarning("Failed to add user {userName}", Input.Username);
            }
            else
            {
                _logger.LogInformation("Added user {userName} with ID {userId} to device {deviceId}", Input.Username, userId, DeviceId);
            }

            return RedirectToPage("Index", new { DeviceId });
        }

        public async Task<IActionResult> OnDeleteAsync([FromQuery] string userId)
        {
            _logger.LogInformation("Deleting user {userId} from device {deviceId}", userId, DeviceId);
            var result = await _iotClient.DeleteUserAsync(DeviceId, userId);

            if (result)
            {
                _logger.LogInformation("Deleted user {userId}", userId);
            }
            else
            {
                _logger.LogWarning("Failed to delete user {userId}", userId);
                // TODO
            }
            return RedirectToPage("Index", new { DeviceId });
        }

        public class InputModel
        {
            [Required]
            [StringLength(15, MinimumLength = 3)]
            [Display(Name = "Username")]
            public string Username { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }
    }
}