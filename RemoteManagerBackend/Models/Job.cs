using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RemoteManagerBackend.Models
{
    public class Job
    {
        public int id { get; set; }
        public String name { get; set; }
        public String path { get; set; }
        public String managerName { get; set; }
        public String clientName { get; set; }
        public String type { get; set; }
        public String status { get; set; }

    }
}


