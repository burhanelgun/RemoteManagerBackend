using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RemoteManagerBackend.Models
{
    public class Client
    {
        public string ipAddress { get; set; }
        public int jobCount { get; set; }

        public string name { get; set; }

        public Client(string ipAddress, string name)
        {
            this.ipAddress = ipAddress;
            this.jobCount = 0;
            this.name = name;
        }

    }
}
