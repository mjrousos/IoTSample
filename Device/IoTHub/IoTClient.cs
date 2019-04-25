using IoTSample.Common.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Device.IoTHub
{
    public class IoTClient
    {
        private const string IoTHubConnectionStringName = "IoTHubConnection";
        private readonly ILogger<IoTClient> _logger;
        private readonly IServiceProvider _serviceProvider;
        private DeviceClient _deviceClient;

        public IoTClient(IConfiguration configuration, ILogger<IoTClient> logger, IServiceProvider serviceProvider)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _deviceClient = DeviceClient.CreateFromConnectionString(configuration.GetConnectionString(IoTHubConnectionStringName));

            _logger.LogInformation("Connected to IoT hub");
        }

        public void RegisterMethodHandlers()
        {
            // Register method handlers
            _deviceClient.SetMethodHandlerAsync("GetUsers", GetUsers, null);
            _deviceClient.SetMethodHandlerAsync("AddUser", AddUser, null);
            _deviceClient.SetMethodHandlerAsync("DeleteUser", DeleteUser, null);
            _deviceClient.SetDesiredPropertyUpdateCallbackAsync(UpdateDeviceConfiguration, null);
            _logger.LogInformation("IoT Hub method handlers registered");
        }

        private async Task UpdateDeviceConfiguration(TwinCollection desiredProperties, object userContext)
        {
            //
        }

        private Task<MethodResponse> GetUsers(MethodRequest methodRequest, object userContext)
        {
            _logger.LogInformation("IoT Hub GetUsers method called");

            // UserManager is scoped and this service will be a singleton, so create a scope for UserManager use
            using (var requestScope = _serviceProvider.CreateScope())
            {
                var userManager = requestScope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                var users = userManager.Users.ToDictionary(u => u.Id, u => u.UserName);

                _logger.LogInformation("Returning {userCount} users to IoT Hub", users.Count);
                return Task.FromResult(CreateMethodResponse(HttpStatusCode.OK, users));
            }
        }

        private async Task<MethodResponse> AddUser(MethodRequest methodRequest, object userContext)
        {
            _logger.LogInformation("IoT Hub AddUser method called");
            var userInfo = JsonConvert.DeserializeObject<AddUserRequest>(methodRequest.DataAsJson);
            if (string.IsNullOrEmpty(userInfo?.UserName) || string.IsNullOrEmpty(userInfo?.Password))
            {
                _logger.LogWarning("Invalid user info passed to AddUser");
                return CreateMethodResponse(HttpStatusCode.BadRequest);
            }

            // UserManager is scoped and this service will be a singleton, so create a scope for UserManager use
            using (var requestScope = _serviceProvider.CreateScope())
            {
                var userManager = requestScope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                var createResult = await userManager.CreateAsync(new IdentityUser(userInfo.UserName), userInfo.Password);

                if (!createResult.Succeeded)
                {
                    _logger.LogWarning("Failed to create user {userName}: {@errors}", userInfo.UserName, createResult.Errors);
                    return CreateMethodResponse(HttpStatusCode.BadRequest, createResult.Errors);
                }
                else
                {
                    var user = await userManager.FindByNameAsync(userInfo.UserName);
                    _logger.LogInformation("Created user {userName} with ID {userId}", userInfo.UserName, user.Id);
                    return CreateMethodResponse(HttpStatusCode.Created, user.Id);
                }
            }
        }

        private async Task<MethodResponse> DeleteUser(MethodRequest methodRequest, object userContext)
        {
            _logger.LogInformation("IoT Hub DeleteUser method called");
            var userId = JsonConvert.DeserializeObject<string>(methodRequest.DataAsJson);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Invalid user ID passed to DeleteUser");
                return CreateMethodResponse(HttpStatusCode.BadRequest);
            }

            // UserManager is scoped and this service will be a singleton, so create a scope for UserManager use
            using (var requestScope = _serviceProvider.CreateScope())
            {
                var userManager = requestScope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                var user = await userManager.FindByIdAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning("User {userId} not found", userId);
                    return CreateMethodResponse(HttpStatusCode.NotFound);
                }

                var deleteResult = await userManager.DeleteAsync(user);
                if (!deleteResult.Succeeded)
                {
                    _logger.LogWarning("Failed to delete user {userId}: {@errors}", user.Id, deleteResult.Errors);
                    return CreateMethodResponse(HttpStatusCode.BadRequest, deleteResult.Errors);
                }
                else
                {
                    _logger.LogInformation("Deleted user {userId}", user.Id);
                    return CreateMethodResponse(HttpStatusCode.OK);
                }
            }
        }

        private static MethodResponse CreateMethodResponse(HttpStatusCode status, object responseBody = null)
        {
            return responseBody == null ?
                new MethodResponse((int)status) :
                new MethodResponse(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(responseBody)), (int)status);
        }
    }
}
