using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
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
            String jobPath = @"C:\Users\b\Desktop\samplejob\" + clientName + "\\Manager-" + email + "\\queue\\Job-"+name+"\\";
            String jobName = "Job-"+name+"\n";
            String managerName = "Manager-" + email;


            //create the job path
            if (!Directory.Exists(jobPath));
                Directory.CreateDirectory(jobPath);

            //            string doneDirPath= @"\\UBUNTU-N55SL\\cloudStorage\\" + clientName + "\\Manager-" + email + "\\done"+ "\\";

            string doneDirPath = @"C:\Users\b\Desktop\samplejob\" + clientName + "\\Manager-" + email + "\\done"+ "\\";
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

            executeClient("192.168.1.38", managerName+ "|" + jobName);



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






        static void executeClient(String ipAddress,String message)
        {

            try
            {

                // Establish the remote endpoint  
                // for the socket. This example  
                // uses port 11111 on the local  
                // computer. 
                IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress ipAddr = ipHost.AddressList[0];
                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), 8888);

                // Creation TCP/IP Socket using  
                // Socket Class Costructor 
                Socket sender = new Socket(IPAddress.Parse(ipAddress).AddressFamily,
                           SocketType.Stream, ProtocolType.Tcp);

                try
                {

                    // Connect Socket to the remote  
                    // endpoint using method Connect() 
                    sender.Connect(localEndPoint);

                    // We print EndPoint information  
                    // that we are connected 
                    Console.WriteLine("Socket connected to -> {0} ",
                                  sender.RemoteEndPoint.ToString());

                    // Creation of messagge that 
                    // we will send to Server 
                    byte[] messageSent = Encoding.ASCII.GetBytes(message);
                    int byteSent = sender.Send(messageSent);

                    // Data buffer 
                    byte[] messageReceived = new byte[1024];

                    // We receive the messagge using  
                    // the method Receive(). This  
                    // method returns number of bytes 
                    // received, that we'll use to  
                    // convert them to string 
                    int byteRecv = sender.Receive(messageReceived);
                    Console.WriteLine("Message from Server -> {0}",
                          Encoding.ASCII.GetString(messageReceived,
                                                     0, byteRecv));

                    // Close Socket using  
                    // the method Close() 






                    messageSent = Encoding.ASCII.GetBytes("bye");
                    byteSent = sender.Send(messageSent);


                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();
                }

                // Manage of Socket's Exceptions 
                catch (ArgumentNullException ane)
                {

                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }

                catch (SocketException se)
                {

                    Debug.WriteLine("SocketException : {0}", se.ToString());
                }

                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }
            }

            catch (Exception e)
            {

                Console.WriteLine(e.ToString());
            }
        }





    }
}
