using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RemoteManagerBackend.Models
{
    public class CreateJob
    {
        public int id { get; set; }

        public String email { get; set; }
        public String jobName { get; set; }
        public String commandFilePath { get; set; }
        public String parametersFilePath { get; set; }
        public String executableFilePath { get; set; }

    }
}
