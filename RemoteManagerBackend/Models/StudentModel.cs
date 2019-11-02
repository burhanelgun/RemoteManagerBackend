using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RemoteManagerBackend.Models
{
    public class StudentModel
    {
        public string Name { get; set; }
        public IFormFile Image { get; set; }
    }
}
