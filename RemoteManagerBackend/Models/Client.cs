using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RemoteManagerBackend.Models
{
    public class Client
    {
        public int id { get; set; }
        public string ip { get; set; }
        public int jobCount { get; set; }
        public string name { get; set; }


    }
}
