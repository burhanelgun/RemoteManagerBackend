using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RemoteManagerBackend.Models
{
    public class CreateJob
    {
        public String jobName { get; set; }
        public IFormFile commandFile { get; set; }
        public IFormFile parametersFile { get; set; }
        public IFormFile executableFile { get; set; }

    }
}
