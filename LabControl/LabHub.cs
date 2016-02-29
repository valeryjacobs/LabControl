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
using System.Web.Caching;
using StackExchange.Redis;

namespace SignalRChat
{
    public class LabHub : Hub
    {
        static RegistryManager registryManager;
        static string connectionString = "HostName=labcontrol.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=+qFuHlgcmOoLlG2TWGJFpaaoYfmq35b3+NL6wM3b7A0=";
        public void Send(string name, string message)
        {
            Clients.All.broadcastMessage(name, message);
        }

        public LabHub()
        {
            registryManager = RegistryManager.CreateFromConnectionString(connectionString);
        }

        private static Lazy<ConnectionMultiplexer> lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
        {
            return ConnectionMultiplexer.Connect("labcontrol.redis.cache.windows.net,abortConnect=false,ssl=true,password=w8CmcHlkBGv2wjZCHaj3ISvfI+tpBqpmH3StjRGf4gc=");
        });

        public static ConnectionMultiplexer Connection
        {
            get
            {
                return lazyConnection.Value;
            }
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

        public void UpdatePos(string clientId, string clientName, int maxHeight, int pos)
        {
            HandlePos(clientId, clientName, maxHeight, pos);
            Clients.All.showPos(clientId, clientName, maxHeight, pos);
        }

        private async Task HandlePos(string clientId, string clientName, int maxHeight, int pos)
        {
            IDatabase cache = Connection.GetDatabase();

            string key =cache.StringGet(clientId);
            if (key == null )
            {
                AddDeviceAsync(clientId);
            }

            var messageContent = new { ClientId = clientId, ClientName = clientName, MaxHeight = maxHeight, Pos = pos };

            var messageString = JsonConvert.SerializeObject(messageContent);
            var message = new Microsoft.Azure.Devices.Client.Message(Encoding.ASCII.GetBytes(messageString));
            var deviceClient = DeviceClient.Create("labcontrol.azure-devices.net", new DeviceAuthenticationWithRegistrySymmetricKey(clientId, cache.StringGet(clientId)));

            try
            {
                deviceClient.SendEventAsync(message);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }

            Debug.WriteLine("ID: " + clientId + " Max:" + maxHeight + " pos:" + pos);
        }

        private async Task AddDeviceAsync(string deviceId)
        {
            IDatabase cache = Connection.GetDatabase();

            Device device;
            try
            {
                device = await registryManager.AddDeviceAsync(new Device(deviceId));
            }
            catch (DeviceAlreadyExistsException)
            {
                device = await registryManager.GetDeviceAsync(deviceId);
            }

            Debug.WriteLine("Key: " + device.Authentication.SymmetricKey.PrimaryKey);
            cache.StringSet(deviceId, device.Authentication.SymmetricKey.PrimaryKey); 
        }
    }
}