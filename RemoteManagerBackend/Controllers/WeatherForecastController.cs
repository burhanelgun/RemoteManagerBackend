using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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

        [HttpGet("user/{_email}")]
        public IActionResult GetManagerByEmail(string _email)
        {
            
            var value = _context.Managers.FirstOrDefault(v => v.email == _email);
            return Ok(JsonConvert.SerializeObject(value.password));
        }

        [HttpPost]
        public void Post(Models.Manager value)
        {
            _context.Managers.AddAsync(value);
            _context.SaveChanges();
        }
    }
}
