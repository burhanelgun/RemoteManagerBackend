using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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
            return "deneme";
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


        [HttpGet("my-jobs/{managerName}")]
        public string Get2(string managerName)
        {

            List<Job> managerJobs = _context.Jobs.Where(v => v.managerName == "Manager-"+managerName).ToList();

            

            return JsonConvert.SerializeObject(managerJobs);

        }

        [HttpPost("createexecutablejob")]
        public async Task Post4([FromForm] string email, [FromForm] string name,[FromForm] IFormFile commandFile, [FromForm] IFormFile parametersFile, [FromForm] IFormFile executableFile, [FromForm] string jobType)
        {

            //run only on client1 machine(normally we need to determine a client between clients)
            string clientName = "Client1";
            

            //specify the job path(in the newtwork storage)
            String jobPath = @"\\UBUNTU-N55SL\\cloudStorage\\" + clientName + "\\Manager-" + email + "\\queue\\Job-"+name+"\\";
            String jobName = "Job-"+name;
            String managerName = "Manager-" + email;
            String typeJob = jobType;


            //create the job path
            Debug.WriteLine("jobPath:", jobPath);

            if (!Directory.Exists(jobPath))
            {
                Debug.WriteLine("jobPath is not exist");

                Directory.CreateDirectory(jobPath);

                Debug.WriteLine("jobPath is created");


            }

            //            string doneDirPath= @"\\UBUNTU-N55SL\\cloudStorage\\" + clientName + "\\Manager-" + email + "\\done"+ "\\";



            string doneDirPath = @"\\UBUNTU-N55SL\\cloudStorage\\" + clientName + "\\Manager-" + email + "\\done"+ "\\";
            //create done directory

            Debug.WriteLine("doneDirPath:", doneDirPath);

            if (!Directory.Exists(doneDirPath))
            {
                Debug.WriteLine("doneDirPath is not exist");

                Directory.CreateDirectory(doneDirPath);

                Debug.WriteLine("doneDirPath is created");

            }

            Debug.WriteLine("***typeJob:"+typeJob);

            //create a Job object to store in the Jobs table

            //if job type executable, create Executablejob,else if job type is archiver, create ArchiverJob
            Job newJob = new Job();
            //Job Type should came from the front end, I choose Executable type for trying.
            newJob.type = typeJob;
            newJob.isDone = false;
            newJob.managerName = managerName;
            newJob.clientName = clientName;
            newJob.name = jobName;

            jobPath = jobPath.Replace("/", "\\");
            jobPath = jobPath.Replace("//", "\\");
            jobPath = jobPath.Replace("\\\\\\\\", "\\");
            jobPath = jobPath.Replace("\\\\\\", "\\");
            jobPath = jobPath.Replace("\\\\", "\\");
            jobPath = jobPath.Replace("/", "\\");
            jobPath = jobPath.Replace("//", "\\");

            jobPath = "\\" + jobPath;


            newJob.path = jobPath;

            //for only Executable job(normally set to the ExecutableJob class datafields)
            String commandFilePath = jobPath + commandFile.FileName;
            String parametersFilePath = jobPath + parametersFile.FileName;
            String executableFilePath = jobPath + executableFile.FileName;




            //Jobs table content should like this.
            //JobName, Job Manager, Job Client, isDone, JobPath, JobType(if isDone is true then JobPath will contain done folder)




            //store the Job to Jobs table
            await _context.Jobs.AddAsync(newJob);
            _context.SaveChanges();


            //copy command file of Job to the network storage 
            var commandFilePathVar = Path.Combine(Directory.GetCurrentDirectory(), commandFilePath);
            using (var fileStream = new FileStream(commandFilePathVar, FileMode.Create))
            {
                await commandFile.CopyToAsync(fileStream);
            }

            //copy parameters file of Job to the network storage
            var parametersFilePathVar = Path.Combine(Directory.GetCurrentDirectory(), parametersFilePath);
            using (var fileStream = new FileStream(parametersFilePathVar, FileMode.Create))
            {
                await parametersFile.CopyToAsync(fileStream);
            }

            //copy executable file of Job to the network storage
            var executableFilePathVar = Path.Combine(Directory.GetCurrentDirectory(), executableFilePath);
            using (var fileStream = new FileStream(executableFilePathVar, FileMode.Create))
            {
                await executableFile.CopyToAsync(fileStream);
            }

            String client1IPAdress = "192.168.1.34";
            executeClient(client1IPAdress, managerName+ "|" + jobName + "|" + newJob.type + "\n");



        }






        [HttpPost("createarchiverjob")]
        public async Task Post6([FromForm] string email, [FromForm] string name, [FromForm] string jobType, [FromForm] IFormFile[] folders)
        {

            //run only on client1 machine(normally we need to determine a client between clients)
            string clientName = "Client1";


            //specify the job path(in the newtwork storage)
            String jobPath = @"\\UBUNTU-N55SL\\cloudStorage\\" + clientName + "\\Manager-" + email + "\\queue\\Job-" + name + "\\";
            String jobName = "Job-" + name;
            String managerName = "Manager-" + email;
            String typeJob = jobType;


            //create the job path
            Debug.WriteLine("jobPath:", jobPath);

            if (!Directory.Exists(jobPath))
            {
                Debug.WriteLine("jobPath is not exist");

                Directory.CreateDirectory(jobPath);

                Debug.WriteLine("jobPath is created");


            }











            string doneDirPath = @"\\UBUNTU-N55SL\\cloudStorage\\" + clientName + "\\Manager-" + email + "\\done" + "\\";
            //create done directory

            Debug.WriteLine("doneDirPath:", doneDirPath);

            if (!Directory.Exists(doneDirPath))
            {
                Debug.WriteLine("doneDirPath is not exist");

                Directory.CreateDirectory(doneDirPath);

                Debug.WriteLine("doneDirPath is created");

            }

            Debug.WriteLine("***typeJob:" + typeJob);

            //create a Job object to store in the Jobs table

            //if job type executable, create Executablejob,else if job type is archiver, create ArchiverJob
            Job newJob = new Job();
            //Job Type should came from the front end, I choose Executable type for trying.
            newJob.type = typeJob;
            newJob.isDone = false;
            newJob.managerName = managerName;
            newJob.clientName = clientName;
            newJob.name = jobName;

            jobPath = jobPath.Replace("/", "\\");
            jobPath = jobPath.Replace("//", "\\");
            jobPath = jobPath.Replace("\\\\\\\\", "\\");
            jobPath = jobPath.Replace("\\\\\\", "\\");
            jobPath = jobPath.Replace("\\\\", "\\");
            jobPath = jobPath.Replace("/", "\\");
            jobPath = jobPath.Replace("//", "\\");
            jobPath = "\\" + jobPath;

            newJob.path = jobPath;

            //Jobs table content should like this.
            //JobName, Job Manager, Job Client, isDone, JobPath, JobType(if isDone is true then JobPath will contain done folder)




            //store the Job to Jobs table
            await _context.Jobs.AddAsync(newJob);
            _context.SaveChanges();

            //String commandFilePath = jobPath + commandFile.FileName;

            //copy command file of Job to the network storage 
            /* var commandFilePathVar = Path.Combine(Directory.GetCurrentDirectory(), commandFilePath);
             using (var fileStream = new FileStream(commandFilePathVar, FileMode.Create))
             {
                 await commandFile.CopyToAsync(fileStream);
             }*/



            //string[] tokens = str.Split(',');

            string mainFolderName= folders[0].FileName.Split('/')[0]; ;

            for (int i = 0; i < folders.Length; i++)
            {
                string tempJobPath = jobPath;
                string[] tokens = folders[i].FileName.Split('/');

                for (int j = 0; j < (tokens.Length-1); ++j)
                {

                    string folderName = tokens[j];
                    tempJobPath = tempJobPath + "\\" + folderName;

                }

                string folderPath = tempJobPath;

                if (!Directory.Exists(folderPath))
                {
                    Debug.WriteLine("folderPath is not exist");

                    Directory.CreateDirectory(folderPath);

                    Debug.WriteLine("doneDirPath is created");

                }

                string filePath = folderPath + "/" + tokens[tokens.Length - 1];

                var filePathVar = Path.Combine(Directory.GetCurrentDirectory(), filePath);
                using (var fileStream = new FileStream(filePathVar, FileMode.Create))
                {
                    await folders[i].CopyToAsync(fileStream);
                }



            }

 

            String client1IPAdress = "192.168.1.34";
            executeClient(client1IPAdress, managerName + "|" + jobName+"|"+ newJob.type + "|" + mainFolderName + "\n");


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






         void executeClient(String ipAddress,String message)
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

                    byte[] messageSent = Encoding.ASCII.GetBytes(message);
                    int byteSent = sender.Send(messageSent);

                    byte[] messageReceived = new byte[1024];

                    int byteRecv = sender.Receive(messageReceived);


                    //take the messageReceived and then if job is in done folder 
                    //then make true isDone flag in Jobs folder and update JobPath in Jobs table

                    //Jobs table content should like this.
                    //JobName, Job Manager, Job Client, isDone, JobPath , JobType(if isDone is true then JobPath will contain done folder)
                    Debug.WriteLine("Message from Server -> {111203}",
                          Encoding.ASCII.GetString(messageReceived,
                                                     0, byteRecv));



                    if (Encoding.ASCII.GetString(messageReceived, 0, byteRecv)!= "error")
                    {
                        string doneJobPath = Encoding.ASCII.GetString(messageReceived, 0, byteRecv);

                        doneJobPath = doneJobPath.Replace("/", "\\");
                        doneJobPath = doneJobPath.Replace("//", "\\");
                        doneJobPath = doneJobPath.Replace("\\\\\\\\", "\\");
                        doneJobPath = doneJobPath.Replace("\\\\\\", "\\");
                        doneJobPath = doneJobPath.Replace("\\\\", "\\");
                        doneJobPath = doneJobPath.Replace("/", "\\");
                        doneJobPath = doneJobPath.Replace("//", "\\");
                        doneJobPath = "\\" + doneJobPath;


                        string[] tokens = doneJobPath.Split('*');
                        tokens[2] = tokens[2].Substring(0, tokens[2].Length - 2);

                        /*
                        await _context.Jobs.AddAsync(newJob);
                        _context.SaveChanges();
                        */



                        Job job = _context.Jobs.First(v => v.managerName == tokens[2] && v.name == tokens[1]);
                        job.isDone = true;
                        job.path = doneJobPath.Split("*")[0];

                        _context.SaveChanges();










                    }



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


        [HttpPost("downloadJob")]
        public FileStream DownloadFile([FromForm] string email, [FromForm] string jobName)
        {

            email = "Manager-" + email;
            jobName = jobName.Split(".")[0];


            Job job = _context.Jobs.FirstOrDefault(v => v.managerName == email && v.name == jobName);

            string startPath = job.path;
            string zipPath =  "\\\\UBUNTU-N55SL\\cloudStorage\\Client1\\"+email+"\\done\\"+ jobName+".zip";
            if (!System.IO.File.Exists(zipPath))
            {
                ZipFile.CreateFromDirectory(startPath, zipPath);
            }

            FileStream fileStream = new FileStream(zipPath, FileMode.Open, FileAccess.Read);

            return fileStream;
        }


    }







}
