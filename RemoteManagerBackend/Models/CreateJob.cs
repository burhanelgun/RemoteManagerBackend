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
        public FormFile commandFile { get; set; }
        public FormFile parametersFile { get; set; }
        public FormFile executableFile { get; set; }

    }
}
