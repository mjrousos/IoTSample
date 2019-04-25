using IoTSample.Common.Models;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.IoTHub
{
    public class IoTClient
    {
        private const string IoTHubConnectionStringName = "IoTHubConnection";
        private readonly ILogger<IoTClient> _logger;
        private ServiceClient _iotHubClient;
        public RegistryManager RegistryManager { get; }

        public IoTClient(IConfiguration configuration, ILogger<IoTClient> logger)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _iotHubClient = ServiceClient.CreateFromConnectionString(configuration.GetConnectionString(IoTHubConnectionStringName));
            RegistryManager = RegistryManager.CreateFromConnectionString(configuration.GetConnectionString(IoTHubConnectionStringName));

            _logger.LogInformation("Connected to IoT hub");
        }

        public async Task<Dictionary<string, string>> GetDeviceUsersAsync(string deviceId)
        {
            _logger.LogInformation("Getting users from device {deviceId}", deviceId);
            var methodRequest = new CloudToDeviceMethod("GetUsers", TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5));
            try
            {
                var response = await _iotHubClient.InvokeDeviceMethodAsync(deviceId, methodRequest);

                if (response.Status / 100 == 2)
                {
                    var users = JsonConvert.DeserializeObject<Dictionary<string, string>>(response.GetPayloadAsJson());
                    _logger.LogInformation("Retrieved {userCount} users from device {deviceId}", users.Count, deviceId);
                    return users;
                }
                else
                {
                    _logger.LogWarning("Failed to retrieve users from {deviceId}. Status code: {status}. Error: {error}", deviceId, response.Status, response.GetPayloadAsJson());
                    return null;
                }
            }
            catch (DeviceNotFoundException)
            {
                _logger.LogInformation("Device {deviceId} not online", deviceId);
                return null;
            }
        }

        public async Task<string> AddUserAsync(string deviceId, AddUserRequest addUserRequest)
        {
            _logger.LogInformation("Adding user to device {deviceId}", deviceId);
            var methodRequest = new CloudToDeviceMethod("AddUser", TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5));
            methodRequest.SetPayloadJson(JsonConvert.SerializeObject(addUserRequest));

            try
            {
                var response = await _iotHubClient.InvokeDeviceMethodAsync(deviceId, methodRequest);

                if (response.Status / 100 == 2)
                {
                    var userId = JsonConvert.DeserializeObject<string>(response.GetPayloadAsJson());
                    _logger.LogInformation("User {userName} created with ID {userId}", addUserRequest.UserName, userId);
                    return userId;
                }
                else
                {
                    _logger.LogWarning("Failed add user to {deviceId}. Status code: {status}. Error: {error}", deviceId, response.Status, response.GetPayloadAsJson());
                    return null;
                }
            }
            catch (DeviceNotFoundException)
            {
                _logger.LogInformation("Device {deviceId} not online", deviceId);
                return null;
            }
        }

        public async Task<bool> DeleteUserAsync(string deviceId, string userId)
        {
            _logger.LogInformation("Deleting user {userId} from device {deviceId}", userId, deviceId);
            var methodRequest = new CloudToDeviceMethod("DeleteUser", TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5));
            methodRequest.SetPayloadJson(JsonConvert.SerializeObject(userId));

            try
            {
                var response = await _iotHubClient.InvokeDeviceMethodAsync(deviceId, methodRequest);

                if (response.Status / 100 == 2)
                {
                    _logger.LogInformation("User {userId} deleted", userId, userId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Failed delete {userId}. Status code: {status}. Error: {error}", userId, response.Status, response.GetPayloadAsJson());
                    return false;
                }
            }
            catch (DeviceNotFoundException)
            {
                _logger.LogInformation("Device {deviceId} not online", deviceId);
                return false;
            }
        }
    }
}
