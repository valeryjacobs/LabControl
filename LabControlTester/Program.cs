using Microsoft.Azure.Devices;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabControlTester
{
    class Program
    {
        static string connectionString = "HostName=labcontrol.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=+qFuHlgcmOoLlG2TWGJFpaaoYfmq35b3+NL6wM3b7A0=";

        static void Main(string[] args)
        {

           var conn = ConnectionMultiplexer.Connect("labcontrol.redis.cache.windows.net,abortConnect=false,,allowAdmin=true,ssl=true,password=w8CmcHlkBGv2wjZCHaj3ISvfI+tpBqpmH3StjRGf4gc=");
            IDatabase cache = conn.GetDatabase();
            var endpoints = conn.GetEndPoints(true);
            foreach (var endpoint in endpoints)
            {
                var server = conn.GetServer(endpoint);
                server.FlushAllDatabases();
            }
        }

       
    }
}
