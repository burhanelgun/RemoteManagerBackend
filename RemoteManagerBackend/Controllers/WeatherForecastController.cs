using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RemoteManagerBackend.Data;
using RemoteManagerBackend.Models;

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
        [HttpPost("file")]
        public async Task UploadFile(IFormFile file)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), @"\\UBUNTU-N55SL\\cloudStorage", file.FileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

        }

        /*
        [HttpPost("createjob")]
        public async Task Post4([FromForm]string job)
        {
            var tutorObj = JsonConvert.DeserializeObject<CreateJob>(job);

        }*/


        [HttpPost("createjob")]
        public async Task Post4([FromForm] string email, [FromForm] string name,[FromForm] IFormFile commandFile, [FromForm] IFormFile parametersFile, [FromForm] IFormFile executableFile)
        {

            //run only on client1 machine(normally we need to determine a client between clients)
            string clientName = "Client1";
            

            //specify the job path(in the newtwork storage)
            String jobPath = @"\\UBUNTU-N55SL\\cloudStorage\\"+ clientName + "\\Manager-" + email + "\\queue\\Job-"+name+"\\";

            //create the job path
            if (!Directory.Exists(jobPath));
                Directory.CreateDirectory(jobPath);


            string doneDirPath= @"\\UBUNTU-N55SL\\cloudStorage\\" + clientName + "\\Manager-" + email + "\\done"+ "\\";
            //create done directory
            if (!Directory.Exists(doneDirPath)) ;
                Directory.CreateDirectory(doneDirPath);


            //create a Job object to store in the Jobs table
            CreateJob createJob = new CreateJob();
            createJob.email = email;
            createJob.jobName = name;
            createJob.commandFilePath = jobPath + commandFile.FileName;
            createJob.parametersFilePath = jobPath + parametersFile.FileName;
            createJob.executableFilePath = jobPath + executableFile.FileName;


            //store the Job to Jobs table
            await _context.Jobs.AddAsync(createJob);
            _context.SaveChanges();


            //copy command file of Job to the network storage 
            var commandFilePath = Path.Combine(Directory.GetCurrentDirectory(), createJob.commandFilePath);
            using (var fileStream = new FileStream(commandFilePath, FileMode.Create))
            {
                await commandFile.CopyToAsync(fileStream);
            }

            //copy command file of Job to the network storage
            var parametersFilePath = Path.Combine(Directory.GetCurrentDirectory(), createJob.parametersFilePath);
            using (var fileStream = new FileStream(parametersFilePath, FileMode.Create))
            {
                await parametersFile.CopyToAsync(fileStream);
            }

            //copy command file of Job to the network storage
            var executableFilePath = Path.Combine(Directory.GetCurrentDirectory(), createJob.executableFilePath);
            using (var fileStream = new FileStream(executableFilePath, FileMode.Create))
            {
                await executableFile.CopyToAsync(fileStream);
            }


        }

        [HttpPost("uploadCommandFile")]
        public async Task Post1(IFormFile file)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), @"\\UBUNTU-N55SL\\cloudStorage", file.FileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
        }

        [HttpPost("uploadParametersFile")]
        public async Task Post2(IFormFile file)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), @"\\UBUNTU-N55SL\\cloudStorage", file.FileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
        }

        [HttpPost("uploadExecutableFile")]
        public async Task Post3(IFormFile file)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), @"\\UBUNTU-N55SL\\cloudStorage", file.FileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
        }


        [HttpPost("user/signin")]
        public IActionResult signIn(Manager value)
        {
            try
            {
                Manager man  = _context.Managers.FirstOrDefault(v => v.email == value.email);
                if (man.password == value.password)
                {
                   
                    return Ok(JsonConvert.SerializeObject("Signed in"));
                }
                else
                {
                    return Ok(JsonConvert.SerializeObject("Wrong password"));
                }

            }
            catch(NullReferenceException e)
            {
                return Ok(JsonConvert.SerializeObject("Wrong username"));
                //return Redirect("http://192.168.1.37:4200/usercantfounds");
            }

        }

        [HttpPost("user/signup")]
        public void signUp(Manager value)
        {
            _context.Managers.AddAsync(value);
            _context.SaveChanges();
        }
    }
}
