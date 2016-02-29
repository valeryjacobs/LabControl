using System;
using System.Web;
using Microsoft.AspNet.SignalR;
using System.IO;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common.Exceptions;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text;
using Microsoft.Azure.Devices.Client;

namespace SignalRChat
{
    public class LabHub : Hub
    {
        static Dictionary<string, string> DeviceKeys;
        static RegistryManager registryManager;
        static string connectionString = "HostName=labcontrol.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=+qFuHlgcmOoLlG2TWGJFpaaoYfmq35b3+NL6wM3b7A0=";
        public void Send(string name, string message)
        {
            // Call the broadcastMessage method to update clients.
            Clients.All.broadcastMessage(name, message);
        }

        public LabHub()
        {
            DeviceKeys = new Dictionary<string, string>();
            registryManager = RegistryManager.CreateFromConnectionString(connectionString);
        }

        public void Init(string content)
        {
            string s = "";
            var url = "https://labcontrolcontent.blob.core.windows.net/content/LabContent.md";
            using (WebClient client = new WebClient())
            {
                s = client.DownloadString(url);
            }
            Clients.All.broadcastMessage("Host", s);
        }

        public async Task<string> GetContent()
        {
            var url = "https://labcontrolcontent.blob.core.windows.net/content/LabContent.md";
            using (WebClient client = new WebClient())
            {
                return client.DownloadString(url);
            }
        }

        public bool UpdatePos(string clientId, string clientName, int maxHeight, int pos)
        {
            HandlePos(clientId, clientName, maxHeight, pos);
            Clients.All.showPos(clientId, clientName, maxHeight, pos);

            return true;
        }

        private async Task<bool> HandlePos(string clientId, string clientName, int maxHeight, int pos)
        {
            if (!DeviceKeys.ContainsKey(clientId))
            {
                AddDeviceAsync(clientId).Wait();
            }

            var messageContent = new { ClientId = clientId, ClientName = clientName, MaxHeight = maxHeight, Pos = pos };

            var messageString = JsonConvert.SerializeObject(messageContent);
            var message = new Microsoft.Azure.Devices.Client.Message(Encoding.ASCII.GetBytes(messageString));
            var deviceClient = DeviceClient.Create("labcontrol.azure-devices.net", new DeviceAuthenticationWithRegistrySymmetricKey(clientId, DeviceKeys[clientId]));

            try
            {
                deviceClient.SendEventAsync(message);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }

            Debug.WriteLine("ID: " + clientId + " Max:" + maxHeight + " pos:" + pos);
            return true;
        }



        private async Task<Device> AddDeviceAsync(string deviceId)
        {
            Device device;
            try
            {
                device = await registryManager.AddDeviceAsync(new Device(deviceId));
            }
            catch (DeviceAlreadyExistsException)
            {
                device = await registryManager.GetDeviceAsync(deviceId);
            }
            Debug.WriteLine("Generated device key: {0}", device.Authentication.SymmetricKey.PrimaryKey);
            DeviceKeys.Add(deviceId, device.Authentication.SymmetricKey.PrimaryKey);

            return device;
        }
    }
}