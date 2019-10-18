using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RemoteManagerBackend.Data;

namespace RemoteManagerBackend.Controllers
{
    [ApiController]
    [Route("/")]
    public class WeatherForecastController : ControllerBase
    {
        DataContext _context;


        public WeatherForecastController(DataContext context)
        {
            _context = context;
        }


        [HttpGet]
        public string Get()
        {
            return "hayırdır";
        }


        [HttpGet("{id}")]
        public string Get(int id)
        {
            return ""+id;
        }

        [HttpPost]
        public string Post(Models.Manager value)
        {
         
            return "TTO";
        }
    }
}
