using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RemoteManagerBackend.Models
{
    public class ParentJob:Job
    {
        public ParentJob(int progress,Job job)
        {
            this.progress = progress;
            this.clientName = job.clientName;
            this.id = job.id;
            this.managerName = job.managerName;
            this.name = job.name;
            this.path = job.path;
            this.status = job.status;
            this.type = job.type;
        }
        public int progress { get; set; }

    }
}


