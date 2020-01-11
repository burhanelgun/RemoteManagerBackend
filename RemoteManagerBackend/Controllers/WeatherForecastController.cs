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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        String baseStoragePath = @"\\"+"192.168.1.39"+"\\cloudStorage\\";
        static readonly object _object = new object();

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
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), baseStoragePath, file.FileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

        }


        [HttpGet("my-jobs/{managerName}")]
        public string Get2(string managerName)
        {

            List<Job> managerJobs = _context.Jobs.Where(v => v.managerName == "Manager-"+managerName).ToList();

            return JsonConvert.SerializeObject(managerJobs);

        }

        [HttpGet("my-job-list/{managerName}")]
        public string myJobList(string managerName)
        {

            List<Job> managerJobs = _context.Jobs.Where(v => v.managerName == "Manager-" + managerName).ToList();

            for (int i=0;i<managerJobs.Count;i++)
            {
                if(managerJobs[i].type!="Single Job")
                {
                    String[] jobNameArr = managerJobs[i].name.Split("-");
                    managerJobs[i].name = jobNameArr[0]+"-"+jobNameArr[1];
                }
            }

            Dictionary<string, Job> nameGroupJobs = new Dictionary<string, Job>();


            for (int i = 0; i < managerJobs.Count; i++)
            {
                if (!nameGroupJobs.ContainsKey((managerJobs[i].name))){
                    nameGroupJobs.Add(managerJobs[i].name, managerJobs[i]);
                }

            }




            return JsonConvert.SerializeObject(nameGroupJobs.Values.ToList());

        }

        [HttpGet("my-subjob-list/{managerName}/{parentJobName}")]
        public string getSubJobs(string managerName, string parentJobName)
        {

            List<Job> managerJobs = _context.Jobs.Where(v => v.managerName == "Manager-" + managerName).ToList();
            if (parentJobName != "undefined")
            {
                parentJobName = parentJobName.Split("-")[1];

            }

            List<Job> subJobs = new List<Job>();

            for(int i =0; i < managerJobs.Count; i++)
            {
                String[] jobNameArr = managerJobs[i].name.Split("-");
                String localParentJobName = jobNameArr[1];
                if (localParentJobName == parentJobName)
                {
                    subJobs.Add(managerJobs[i]);
                }          

            }



            return JsonConvert.SerializeObject(subJobs);

        }

        [HttpGet("{clients}")]
        public string Get3()
        {
            try{
                List<Client> clients = _context.Clients.ToList();

                return JsonConvert.SerializeObject(clients);
            }
            catch(Exception e)
            {
                List<Client> clients = new List<Client>();

                return JsonConvert.SerializeObject(clients);
            }


        }

        [HttpPost("client/add")]
        public void addClient(Client client)
        {
            String ipAddress = client.ip;
            String message = "SendInfo\n";


            string[] tokens = null;
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


                    //Jobs table content should like this.
                    Debug.WriteLine("Message from Server -> {111203}",
                          Encoding.ASCII.GetString(messageReceived,
                                                     0, byteRecv));

                    client.coreCount = Int32.Parse(Encoding.ASCII.GetString(messageReceived,
                                                     0, byteRecv).Split(':')[1]);
                    client.jobCount = 0;
                    _context.Clients.AddAsync(client);
                    _context.SaveChanges();



                    messageSent = Encoding.ASCII.GetBytes("bye");
                    byteSent = sender.Send(messageSent);


                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();
                }

                // Manage of Socket's Exceptions 
                catch (ArgumentNullException ane)
                {

                    Job job = _context.Jobs.First(v => v.managerName == tokens[2] && v.name == tokens[1]);
                    job.status = "fail";
                    _context.SaveChanges();
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }

                catch (SocketException se)
                {


                    Job job = _context.Jobs.First(v => v.managerName == tokens[2] && v.name == tokens[1]);
                    job.status = "fail";
                    _context.SaveChanges();
                    Debug.WriteLine("SocketException : {0}", se.ToString());
                }

                catch (Exception e)
                {

                    Job job = _context.Jobs.First(v => v.managerName == tokens[2] && v.name == tokens[1]);
                    job.status = "fail";
                    _context.SaveChanges();

                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }
            }

            catch (Exception e)
            {
                /*Job job = _context.Jobs.First(v => v.managerName == tokens[2] && v.name == tokens[1]);
                job.status = "fail";
                _context.SaveChanges();*/
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
            //--string zipPath = baseStoragePath + job.clientName+"\\"+email+"\\done\\"+ jobName+".zip";
            string zipPath = baseStoragePath + job.clientName + "\\" + email + "\\" + jobName + ".zip";
            if (!System.IO.File.Exists(zipPath))
            {
                ZipFile.CreateFromDirectory(startPath, zipPath);
            }

            FileStream fileStream = new FileStream(zipPath, FileMode.Open, FileAccess.Read);

            return fileStream;
        }

        [HttpPost("client/delete")]
        public async Task removeClientAsync(Client client)
        {
            Client clientActual = _context.Clients.FirstOrDefault(v => v.name == client.name && v.ip == client.ip);
            _context.Clients.Attach(clientActual);
            _context.Clients.Remove(clientActual);
            await _context.SaveChangesAsync();
        }

        [HttpPost("client/update")]
        public void updateClient([FromForm] string clientBeforeName, [FromForm] string clientBeforeIP, [FromForm] string clientAfterName, [FromForm] string clientAfterIP)
        {

            Client clientBefore = _context.Clients.FirstOrDefault(v => v.name == clientBeforeName && v.ip == clientBeforeIP);

            clientBefore.name = clientAfterName;
            clientBefore.ip = clientAfterIP;

            _context.SaveChanges();
        }





        public static string getInputGroup(string inputFolder)
        {
            string groupNum = inputFolder.Split('/')[1]; ;
            return groupNum;

        }








        [HttpPost("createsinglejob")]
        public async Task CreateSingleJob([FromForm] string email, [FromForm] string name, [FromForm] IFormFile pythonScriptFile, [FromForm] IFormFile parametersFile, [FromForm] IFormFile[] executableFiles, [FromForm] IFormFile[] inputFiles, [FromForm] string jobType)
        {

            Client selectedClient = _context.Clients.ToList().OrderBy(o => o.jobCount).ToList()[0];


            if (selectedClient != null)
            {
                bool saveFailed;
                do
                {
                    saveFailed = false;
                    ++selectedClient.jobCount;

                    try
                    {
                        _context.SaveChanges();
                    }
                    catch (DbUpdateConcurrencyException e)
                    {
                        saveFailed = true;
                        e.Entries.Single().Reload();
                    }
                } while (saveFailed);
            }

            //specify the job path(in the newtwork storage)
            String jobPath = baseStoragePath + selectedClient.name + "\\Manager-" + email + "\\Job-" + name + "\\";

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


       
            Debug.WriteLine("***typeJob:" + typeJob);

            //create a Job object to store in the Jobs table

            //if job type executable, create Executablejob,else if job type is archiver, create ArchiverJob
            Job newJob = new Job();
            //Job Type should came from the front end, I choose Executable type for trying.
            newJob.type = typeJob;
            newJob.status = "working";
            newJob.managerName = managerName;
            //newJob.clientName = _context.clients[index].name;
            newJob.clientName = selectedClient.name;

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
            String pythonScriptFilePath = jobPath + pythonScriptFile.FileName;
            var pythonScriptFilePathVar = Path.Combine(Directory.GetCurrentDirectory(), pythonScriptFilePath);
            using (var fileStream = new FileStream(pythonScriptFilePathVar, FileMode.Create))
            {
                await pythonScriptFile.CopyToAsync(fileStream);
            }





            String parametersFilePath = null;
            if (parametersFile != null)
            {
                parametersFilePath = jobPath + parametersFile.FileName;

                var parametersFilePathVar = Path.Combine(Directory.GetCurrentDirectory(), parametersFilePath);
                using (var fileStream = new FileStream(parametersFilePathVar, FileMode.Create))
                {
                    await parametersFile.CopyToAsync(fileStream);
                }
            }





            String[] executableFilesPaths = null;
            if (executableFiles != null)
            {
                executableFilesPaths = new String[executableFiles.Length];
                for (int i = 0; i < executableFiles.Length; i++)
                {
                    executableFilesPaths[i] = jobPath + executableFiles[i].FileName;
                }

                for (int i = 0; i < executableFiles.Length; i++)
                {
                    var executableFilePathVar = Path.Combine(Directory.GetCurrentDirectory(), executableFilesPaths[i]);
                    using (var fileStream = new FileStream(executableFilePathVar, FileMode.Create))
                    {
                        await executableFiles[i].CopyToAsync(fileStream);
                    }
                }
            }




            String[] inputFilesPaths = null;
            if (inputFiles != null)
            {
                inputFilesPaths = new String[inputFiles.Length];
                for (int i = 0; i < inputFiles.Length; i++)
                {
                    inputFilesPaths[i] = jobPath + inputFiles[i].FileName;
                }

                for (int i = 0; i < inputFiles.Length; i++)
                {
                    var executableFilePathVar = Path.Combine(Directory.GetCurrentDirectory(), inputFilesPaths[i]);
                    using (var fileStream = new FileStream(executableFilePathVar, FileMode.Create))
                    {
                        await inputFiles[i].CopyToAsync(fileStream);
                    }
                }
            }





            //store the Job to Jobs table
            await _context.Jobs.AddAsync(newJob);
            _context.SaveChanges();







            executeClient(selectedClient.ip, baseStoragePath + "|" + selectedClient.name + "|" + managerName + "|" + jobName + "|" + newJob.type + "\n");

            Client findSelectedClientAgain = _context.Clients.First(v => v.name == selectedClient.name && v.ip == selectedClient.ip);

            if (findSelectedClientAgain != null)
            {
                bool saveFailed;
                do
                {
                    saveFailed = false;
                    --findSelectedClientAgain.jobCount;

                    try
                    {
                        _context.SaveChanges();
                    }
                    catch (DbUpdateConcurrencyException e)
                    {
                        saveFailed = true;
                        e.Entries.Single().Reload();
                    }
                } while (saveFailed);
            }


        }

        [HttpPost("createexecutablejob")]
        public async Task Post4([FromForm] string email, [FromForm] string name, [FromForm] IFormFile commandFile, [FromForm] IFormFile parametersFile, [FromForm] IFormFile executableFile, [FromForm] string jobType)
        {


            Client selectedClient = _context.Clients.ToList().OrderBy(o => o.jobCount).ToList()[0];


            if (selectedClient != null)
            {
                bool saveFailed;
                do
                {
                    saveFailed = false;
                    ++selectedClient.jobCount;

                    try
                    {
                        _context.SaveChanges();
                    }
                    catch (DbUpdateConcurrencyException e)
                    {
                        saveFailed = true;
                        e.Entries.Single().Reload();
                    }
                } while (saveFailed);
            }

            //specify the job path(in the newtwork storage)
                String jobPath = baseStoragePath + selectedClient.name +        "\\Manager-" + email + "\\Job-" + name + "\\";
            //--String jobPath = baseStoragePath + selectedClient.name + "\\queue\\Manager-" + email + "\\Job-" + name + "\\";
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


           /*-- string doneDirPath = baseStoragePath + selectedClient.name + "\\Manager-" + email + "\\done" + "\\";

            //create done directory

            Debug.WriteLine("doneDirPath:", doneDirPath);

            if (!Directory.Exists(doneDirPath))
            {
                Debug.WriteLine("doneDirPath is not exist");

                Directory.CreateDirectory(doneDirPath);

                Debug.WriteLine("doneDirPath is created");

            }
            --*/
            Debug.WriteLine("***typeJob:" + typeJob);

            //create a Job object to store in the Jobs table

            //if job type executable, create Executablejob,else if job type is archiver, create ArchiverJob
            Job newJob = new Job();
            //Job Type should came from the front end, I choose Executable type for trying.
            newJob.type = typeJob;
            newJob.status = "working";
            newJob.managerName = managerName;
            //newJob.clientName = _context.clients[index].name;
            newJob.clientName = selectedClient.name;

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

            executeClient(selectedClient.ip, baseStoragePath + "|" + selectedClient.name + "|" + managerName + "|" + jobName + "|" + newJob.type + "\n");

            Client findSelectedClientAgain = _context.Clients.First(v => v.name == selectedClient.name && v.ip == selectedClient.ip);

            if (findSelectedClientAgain != null)
            {
                bool saveFailed;
                do
                {
                    saveFailed = false;
                    --findSelectedClientAgain.jobCount;

                    try
                    {
                        _context.SaveChanges();
                    }
                    catch (DbUpdateConcurrencyException e)
                    {
                        saveFailed = true;
                        e.Entries.Single().Reload();
                    }
                } while (saveFailed);
            }


        }










































        [HttpPost("createdifferentparamexecutables")]
        public async Task Post5([FromForm] string email, [FromForm] string name, [FromForm] string command, [FromForm] IFormFile executableFile, [FromForm] string jobType, [FromForm] string[] parameterSets)
        {

            for(int i = 0; i < parameterSets.Length; i++)
            {


                //parameters sets shared equally all clients
                Client selectedClient = _context.Clients.ToList()[i % (_context.Clients.ToList().Count)];


                if (selectedClient != null)
                {
                    bool saveFailed;
                    do
                    {
                        saveFailed = false;
                        ++selectedClient.jobCount;

                        try
                        {
                            _context.SaveChanges();
                        }
                        catch (DbUpdateConcurrencyException e)
                        {
                            saveFailed = true;
                            e.Entries.Single().Reload();
                        }
                    } while (saveFailed);
                }

                //specify the job path(in the newtwork storage)
                //--String jobPath = baseStoragePath + selectedClient.name + "\\Manager-" + email + "\\queue\\Job-" + name + "-"+(i+1) + "\\";
                    String jobPath = baseStoragePath + selectedClient.name + "\\Manager-" + email +        "\\Job-" + name + "-"+(i+1) + "\\";
                String jobName = "Job-" + name + "-"+ (i + 1);
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



               /*-- String doneDirPath = baseStoragePath + selectedClient.name + "\\Manager-" + email + "\\done" + "\\";

                //create done directory

                Debug.WriteLine("doneDirPath:", doneDirPath);

                if (!Directory.Exists(doneDirPath))
                {
                    Debug.WriteLine("doneDirPath is not exist");

                    Directory.CreateDirectory(doneDirPath);

                    Debug.WriteLine("doneDirPath is created");

                }
                --*/
                Debug.WriteLine("***typeJob:" + typeJob);

                //create a Job object to store in the Jobs table

                //if job type executable, create Executablejob,else if job type is archiver, create ArchiverJob
                Job newJob = new Job();
                //Job Type should came from the front end, I choose Executable type for trying.
                newJob.type = typeJob;
                newJob.status = "working";
                newJob.managerName = managerName;
                //newJob.clientName = _context.clients[index].name;
                newJob.clientName = selectedClient.name;

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



                String executableFilePath = jobPath + executableFile.FileName;



                //store the Job to Jobs table
                await _context.Jobs.AddAsync(newJob);
                _context.SaveChanges();



                //copy executable file of Job to the network storage
                var executableFilePathVar = Path.Combine(Directory.GetCurrentDirectory(), executableFilePath);
                using (var fileStream = new FileStream(executableFilePathVar, FileMode.Create))
                {
                    await executableFile.CopyToAsync(fileStream);
                }

                executeClient(selectedClient.ip, baseStoragePath + "|" + selectedClient.name + "|" + managerName + "|" + jobName + "|" + newJob.type + "|" + command + "|" + parameterSets[i]+ "|" + executableFile.FileName +"\n");

                Client findSelectedClientAgain = _context.Clients.First(v => v.name == selectedClient.name && v.ip == selectedClient.ip);

                if (findSelectedClientAgain != null)
                {
                    bool saveFailed;
                    do
                    {
                        saveFailed = false;
                        --findSelectedClientAgain.jobCount;

                        try
                        {
                            _context.SaveChanges();
                        }
                        catch (DbUpdateConcurrencyException e)
                        {
                            saveFailed = true;
                            e.Entries.Single().Reload();
                        }
                    } while (saveFailed);
                }


            }


        }








































        [HttpPost("createarchiverjob")]
        public async Task Post6([FromForm] string email, [FromForm] string name, [FromForm] string jobType, [FromForm] IFormFile[] folders)
        {

            //run only on client1 machine(normally we need to determine a client between clients)


            //select client
            Client selectedClient = _context.Clients.ToList().OrderBy(o => o.jobCount).ToList()[0];




            if (selectedClient != null)
            {
                bool saveFailed;
                do
                {
                    saveFailed = false;
                    ++selectedClient.jobCount;

                    try
                    {
                        _context.SaveChanges();
                    }
                    catch (DbUpdateConcurrencyException e)
                    {
                        saveFailed = true;
                        e.Entries.Single().Reload();
                    }
                } while (saveFailed);
            }



            //specify the job path(in the newtwork storage)
            
            //--String jobPath = baseStoragePath + selectedClient.name + "\\Manager-" + email + "\\queue\\Job-" + name + "\\";
                String jobPath = baseStoragePath + selectedClient.name + "\\Manager-" + email +         "\\Job-" + name + "\\";

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



            /*--string doneDirPath = baseStoragePath + selectedClient.name + "\\Manager-" + email + "\\done" + "\\";

            //create done directory

            Debug.WriteLine("doneDirPath:", doneDirPath);

            if (!Directory.Exists(doneDirPath))
            {
                Debug.WriteLine("doneDirPath is not exist");

                Directory.CreateDirectory(doneDirPath);

                Debug.WriteLine("doneDirPath is created");

            }
            --*/

            Debug.WriteLine("***typeJob:" + typeJob);

            //create a Job object to store in the Jobs table

            //if job type executable, create Executablejob,else if job type is archiver, create ArchiverJob
            Job newJob = new Job();
            //Job Type should came from the front end, I choose Executable type for trying.
            newJob.type = typeJob;
            newJob.status = "working";
            newJob.managerName = managerName;
            newJob.clientName = selectedClient.name;
            //newJob.clientName = "Client1";

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




            //store the Job to Jobs table
            await _context.Jobs.AddAsync(newJob);
            _context.SaveChanges();


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



            executeClient(selectedClient.ip, baseStoragePath + "|" + selectedClient.name + "|" + managerName + "|" + jobName+"|"+ newJob.type + "|" + mainFolderName + "\n");


            Client findSelectedClientAgain = _context.Clients.First(v => v.name == selectedClient.name && v.ip == selectedClient.ip);

            if (findSelectedClientAgain != null)
            {
                bool saveFailed;
                do
                {
                    saveFailed = false;
                    --findSelectedClientAgain.jobCount;

                    try
                    {
                        _context.SaveChanges();
                    }
                    catch (DbUpdateConcurrencyException e)
                    {
                        saveFailed = true;
                        e.Entries.Single().Reload();
                    }
                } while (saveFailed);
            }


        }



        [HttpPost("uploadCommandFile")]
        public async Task Post1(IFormFile file)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), baseStoragePath, file.FileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }



        }

        [HttpPost("uploadParametersFile")]
        public async Task Post2(IFormFile file)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), baseStoragePath, file.FileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
        }

        [HttpPost("uploadExecutableFile")]
        public async Task Post3(IFormFile file)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), baseStoragePath, file.FileName);
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




        [HttpPost("createinputsbatchjob")]
        public async Task CreateInputsBatchJob([FromForm] string email, [FromForm] string name, [FromForm] IFormFile pythonScriptFile, [FromForm] IFormFile parametersFile, [FromForm] IFormFile[] executableFiles, [FromForm] IFormFile[] inputFiles, [FromForm] string jobType)
        {

            Dictionary<string, List<IFormFile>> groupInputFiles = new Dictionary<string, List<IFormFile>>();
            Dictionary<Client, List<string>> clientAndJobs = new Dictionary<Client, List<string>>();


            for (int i = 0; i < inputFiles.Length; i++)
            {
                string groupNum = getInputGroup(inputFiles[i].FileName);


                if (groupInputFiles.ContainsKey(groupNum))
                {
                    List<IFormFile> test = new List<IFormFile>();
                    test = groupInputFiles[groupNum];
                    test.Add(inputFiles[i]);
                    groupInputFiles[groupNum] = test;
                }
                else
                {
                    List<IFormFile> test = new List<IFormFile>();
                    test.Add(inputFiles[i]);
                    groupInputFiles.Add(groupNum, test);
                }

            }









            for (int k = 0; k < groupInputFiles.Count; k++)
            {


                //parameters sets shared equally all clients
                Client selectedClient = _context.Clients.ToList()[k % (_context.Clients.ToList().Count)];


                if (selectedClient != null)
                {
                    bool saveFailed;
                    do
                    {
                        saveFailed = false;
                        ++selectedClient.jobCount;

                        try
                        {
                            _context.SaveChanges();
                        }
                        catch (DbUpdateConcurrencyException e)
                        {
                            saveFailed = true;
                            e.Entries.Single().Reload();
                        }
                    } while (saveFailed);
                }


                //specify the job path(in the newtwork storage)
                String jobPath = baseStoragePath + selectedClient.name + "\\Manager-" + email + "\\Job-" + name + "-" + (k + 1) + "\\";

                String jobName = "Job-" + name + "-" + (k + 1);
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



                Debug.WriteLine("***typeJob:" + typeJob);

                //create a Job object to store in the Jobs table

                //if job type executable, create Executablejob,else if job type is archiver, create ArchiverJob
                Job newJob = new Job();
                //Job Type should came from the front end, I choose Executable type for trying.
                newJob.type = typeJob;
                newJob.status = "working";
                newJob.managerName = managerName;
                //newJob.clientName = _context.clients[index].name;
                newJob.clientName = selectedClient.name;

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
                String pythonScriptFilePath = jobPath + pythonScriptFile.FileName;
                var pythonScriptFilePathVar = Path.Combine(Directory.GetCurrentDirectory(), pythonScriptFilePath);
                using (var fileStream = new FileStream(pythonScriptFilePathVar, FileMode.Create))
                {
                    await pythonScriptFile.CopyToAsync(fileStream);
                }





                String parametersFilePath = null;
                if (parametersFile != null)
                {
                    parametersFilePath = jobPath + parametersFile.FileName;

                    var parametersFilePathVar = Path.Combine(Directory.GetCurrentDirectory(), parametersFilePath);
                    using (var fileStream = new FileStream(parametersFilePathVar, FileMode.Create))
                    {
                        await parametersFile.CopyToAsync(fileStream);
                    }
                }





                String[] executableFilesPaths = null;
                if (executableFiles != null)
                {
                    executableFilesPaths = new String[executableFiles.Length];
                    for (int i = 0; i < executableFiles.Length; i++)
                    {
                        executableFilesPaths[i] = jobPath + executableFiles[i].FileName;
                    }

                    for (int i = 0; i < executableFiles.Length; i++)
                    {
                        var executableFilePathVar = Path.Combine(Directory.GetCurrentDirectory(), executableFilesPaths[i]);
                        using (var fileStream = new FileStream(executableFilePathVar, FileMode.Create))
                        {
                            await executableFiles[i].CopyToAsync(fileStream);
                        }
                    }
                }


                List<IFormFile> myInputFiles = new List<IFormFile>();
                string key = (k + 1) + "";
                myInputFiles = groupInputFiles[key];





                String[] inputFilesPaths = null;
                if (myInputFiles != null)
                {
                    inputFilesPaths = new String[myInputFiles.Count];
                    for (int i = 0; i < myInputFiles.Count; i++)
                    {
                        string[] tokens = myInputFiles[i].FileName.Split('/');

                        string fileName = tokens[tokens.Length - 1];

                        inputFilesPaths[i] = jobPath + fileName;
                    }

                    for (int i = 0; i < myInputFiles.Count; i++)
                    {
                        var executableFilePathVar = Path.Combine(Directory.GetCurrentDirectory(), inputFilesPaths[i]);
                        using (var fileStream = new FileStream(executableFilePathVar, FileMode.Create))
                        {
                            await myInputFiles[i].CopyToAsync(fileStream);
                        }
                    }
                }




                //sadece 1 tane dosya input olarak yeterliyse bu yol kullanılır
                /* if (inputFiles != null)
                  {

                      String inputFilePath = jobPath + inputFiles[k].FileName;

                      var inputFilePathVar = Path.Combine(Directory.GetCurrentDirectory(), inputFilePath);
                      using (var fileStream = new FileStream(inputFilePathVar, FileMode.Create))
                      {
                          await inputFiles[k].CopyToAsync(fileStream);
                      }

                  }
                  */





                //store the Job to Jobs table
                await _context.Jobs.AddAsync(newJob);
                _context.SaveChanges();





                
                
               


                if (clientAndJobs.ContainsKey(selectedClient))
                {
                    List<string> test = new List<string>();
                    test = clientAndJobs[selectedClient];
                    test.Add(baseStoragePath + "|" + selectedClient.name + "|" + managerName + "|" + jobName + "|" + newJob.type + "\n");
                    clientAndJobs[selectedClient] = test;
                }
                else
                {
                    List<string> test = new List<string>();
                    test.Add(baseStoragePath + "|" + selectedClient.name + "|" + managerName + "|" + jobName + "|" + newJob.type + "\n");
                    clientAndJobs.Add(selectedClient, test);
                }

                




                //executeClient(selectedClient.ip, baseStoragePath + "|" + selectedClient.name + "|" + managerName + "|" + jobName + "|" + newJob.type + "\n");

                /*Thread thread = new Thread(() => executeClient(selectedClient.ip, baseStoragePath + "|" + selectedClient.name + "|" + managerName + "|" + jobName + "|" + newJob.type + "\n"));
                 thread.Start();*/

            }

            Thread[] threadArrEachClient = new Thread[clientAndJobs.Count];
            int p = 0;
            foreach (KeyValuePair<Client, List<string>> entry in clientAndJobs)
            {

                threadArrEachClient[p] = new Thread(() => executeClient2(entry.Key,entry.Value));
                threadArrEachClient[p].Start();
                p++;

            }

            for (int i = 0; i < threadArrEachClient.Length; i++)
            {
                threadArrEachClient[i].Join();
            }


        }






        [HttpPost("createparametersbatchjob")]
        public async Task CreateParametersBatchJob([FromForm] string email, [FromForm] string name, [FromForm] IFormFile pythonScriptFile, [FromForm] IFormFile parametersFile, [FromForm] IFormFile[] executableFiles, [FromForm] IFormFile[] inputFiles, [FromForm] string jobType)
        {

            List<string> parameterSetsList = new List<string>();
            Dictionary<Client, List<string>> clientAndJobs = new Dictionary<Client, List<string>>();


            using (var reader = new StreamReader(parametersFile.OpenReadStream()))
            {
                int j = 0;
                while (reader.Peek() >= 0)
                {
                    parameterSetsList.Add(reader.ReadLine());
                    j++;
                }

            }
            string[] parameterSets = parameterSetsList.ToArray();








            for (int k = 0; k < parameterSets.Length; k++)
            {


                //parameters sets shared equally all clients
                Client selectedClient = _context.Clients.ToList()[k % (_context.Clients.ToList().Count)];


                if (selectedClient != null)
                {
                    bool saveFailed;
                    do
                    {
                        saveFailed = false;
                        ++selectedClient.jobCount;

                        try
                        {
                            _context.SaveChanges();
                        }
                        catch (DbUpdateConcurrencyException e)
                        {
                            saveFailed = true;
                            e.Entries.Single().Reload();
                        }
                    } while (saveFailed);
                }


                //specify the job path(in the newtwork storage)
                String jobPath = baseStoragePath + selectedClient.name + "\\Manager-" + email + "\\Job-" + name + "-" + (k + 1) + "\\";

                String jobName = "Job-" + name + "-" + (k + 1);
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



                Debug.WriteLine("***typeJob:" + typeJob);

                //create a Job object to store in the Jobs table

                //if job type executable, create Executablejob,else if job type is archiver, create ArchiverJob
                Job newJob = new Job();
                //Job Type should came from the front end, I choose Executable type for trying.
                newJob.type = typeJob;
                newJob.status = "working";
                newJob.managerName = managerName;
                //newJob.clientName = _context.clients[index].name;
                newJob.clientName = selectedClient.name;

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
                String pythonScriptFilePath = jobPath + pythonScriptFile.FileName;
                var pythonScriptFilePathVar = Path.Combine(Directory.GetCurrentDirectory(), pythonScriptFilePath);
                using (var fileStream = new FileStream(pythonScriptFilePathVar, FileMode.Create))
                {
                    await pythonScriptFile.CopyToAsync(fileStream);
                }





                String parametersFilePath = null;
                if (parametersFile != null)
                {
                    parametersFilePath = jobPath + parametersFile.FileName;

                    var parametersFilePathVar = Path.Combine(Directory.GetCurrentDirectory(), parametersFilePath);
                    using (var fileStream = new FileStream(parametersFilePathVar, FileMode.Create))
                    {
                        await parametersFile.CopyToAsync(fileStream);
                    }
                }





                String[] executableFilesPaths = null;
                if (executableFiles != null)
                {
                    executableFilesPaths = new String[executableFiles.Length];
                    for (int i = 0; i < executableFiles.Length; i++)
                    {
                        executableFilesPaths[i] = jobPath + executableFiles[i].FileName;
                    }

                    for (int i = 0; i < executableFiles.Length; i++)
                    {
                        var executableFilePathVar = Path.Combine(Directory.GetCurrentDirectory(), executableFilesPaths[i]);
                        using (var fileStream = new FileStream(executableFilePathVar, FileMode.Create))
                        {
                            await executableFiles[i].CopyToAsync(fileStream);
                        }
                    }
                }




                String[] inputFilesPaths = null;
                if (inputFiles != null)
                {
                    inputFilesPaths = new String[inputFiles.Length];
                    for (int i = 0; i < inputFiles.Length; i++)
                    {
                        inputFilesPaths[i] = jobPath + inputFiles[i].FileName;
                    }

                    for (int i = 0; i < inputFiles.Length; i++)
                    {
                        var executableFilePathVar = Path.Combine(Directory.GetCurrentDirectory(), inputFilesPaths[i]);
                        using (var fileStream = new FileStream(executableFilePathVar, FileMode.Create))
                        {
                            await inputFiles[i].CopyToAsync(fileStream);
                        }
                    }
                }





                //store the Job to Jobs table
                await _context.Jobs.AddAsync(newJob);
                _context.SaveChanges();







                //executeClient(selectedClient.ip, baseStoragePath + "|" + selectedClient.name + "|" + managerName + "|" + jobName + "|" + newJob.type + "|" + parameterSets[k] + "\n");


                if (clientAndJobs.ContainsKey(selectedClient))
                {
                    List<string> test = new List<string>();
                    test = clientAndJobs[selectedClient];
                    test.Add(baseStoragePath + "|" + selectedClient.name + "|" + managerName + "|" + jobName + "|" + newJob.type + "|" + parameterSets[k] + "\n");
                    clientAndJobs[selectedClient] = test;
                }
                else
                {
                    List<string> test = new List<string>();
                    test.Add(baseStoragePath + "|" + selectedClient.name + "|" + managerName + "|" + jobName + "|" + newJob.type + "|" + parameterSets[k] + "\n");
                    clientAndJobs.Add(selectedClient, test);
                }






              

            }

            Thread[] threadArrEachClient = new Thread[clientAndJobs.Count];
            int p = 0;
            foreach (KeyValuePair<Client, List<string>> entry in clientAndJobs)
            {

                threadArrEachClient[p] = new Thread(() => executeClient2(entry.Key, entry.Value));
                threadArrEachClient[p].Start();
                p++;

            }

            for (int i = 0; i < threadArrEachClient.Length; i++)
            {
                threadArrEachClient[i].Join();
            }

        }







        void executeClient2(Client client, List<string> jobs)
        {
            try
            {
                String ipAddress = client.ip;

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
                sender.Connect(localEndPoint);

                for (int i = 0; i < jobs.Count; i++)
                {

                    String message = jobs[i];





                    string[] tokens = null;



                    try
                    {

                        // Connect Socket to the remote  
                        // endpoint using method Connect() 

                        // We print EndPoint information  
                        // that we are connected 
                        Console.WriteLine("Socket connected to -> {0} ",
                                      sender.RemoteEndPoint.ToString());

                        byte[] messageSent = Encoding.ASCII.GetBytes(message);
                        int byteSent = sender.Send(messageSent);


                        byte[] messageReceived = new byte[1024];

                        int byteRecv = sender.Receive(messageReceived);


                        //Jobs table content should like this.
                        Debug.WriteLine("Message from Server -> {111203}",
                              Encoding.ASCII.GetString(messageReceived,
                                                         0, byteRecv));

                        lock (_object)
                        {
                            Debug.WriteLine("hello111");
                            if (Encoding.ASCII.GetString(messageReceived, 0, byteRecv) != "error")
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


                                tokens = doneJobPath.Split('*');

                                // linux client 
                                tokens[2] = Regex.Replace(tokens[2], @"\t|\n|\r", "");
                                // windows client tokens[2] = tokens[2].Substring(0, tokens[2].Length - 2);

                                Debug.WriteLine("hello222");

                                Job job = _context.Jobs.First(v => v.managerName == tokens[2] && v.name == tokens[1]);
                                job.status = "finish";
                                job.path = doneJobPath.Split("*")[0];
                                Debug.WriteLine("hello25252525");

                            
                                _context.SaveChanges();
                            
                                Debug.WriteLine("hello333");


                            }







                            Debug.WriteLine("hello444");

                      
                            Client findSelectedClientAgain = _context.Clients.First(v => v.ip == client.ip);
                            if (findSelectedClientAgain != null)
                            {
                                bool saveFailed;
                                do
                                {
                                    saveFailed = false;
                                    --findSelectedClientAgain.jobCount;

                                    try
                                    {
                                         _context.SaveChanges();
                                    }
                                    catch (DbUpdateConcurrencyException e)
                                    {
                                        saveFailed = true;
                                        e.Entries.Single().Reload();
                                    }
                                } while (saveFailed);
                            }
                            Debug.WriteLine("hello555");

                        }





                    }

                    // Manage of Socket's Exceptions 
                    catch (ArgumentNullException ane)
                    {

                        Job job = _context.Jobs.First(v => v.managerName == tokens[2] && v.name == tokens[1]);
                        job.status = "fail";
                        _context.SaveChanges();
                        Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                    }

                    catch (SocketException se)
                    {


                        Job job = _context.Jobs.First(v => v.managerName == tokens[2] && v.name == tokens[1]);
                        job.status = "fail";
                        _context.SaveChanges();
                        Debug.WriteLine("SocketException : {0}", se.ToString());
                    }

                    catch (Exception e)
                    {

                        Job job = _context.Jobs.First(v => v.managerName == tokens[2] && v.name == tokens[1]);
                        job.status = "fail";
                        _context.SaveChanges();

                        Console.WriteLine("Unexpected exception : {0}", e.ToString());
                    }

                }

                byte[] byeSent = Encoding.ASCII.GetBytes("bye");
                int byteByeSent = sender.Send(byeSent);


                sender.Shutdown(SocketShutdown.Both);
                sender.Close();

            }


            catch (Exception e)
            {
                /*Job job = _context.Jobs.First(v => v.managerName == tokens[2] && v.name == tokens[1]);
                job.status = "fail";
                _context.SaveChanges();*/
                Console.WriteLine(e.ToString());

            }


    







        }


        void executeClient(String ipAddress,String message)
        {
            string[] tokens=null;
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


                    //Jobs table content should like this.
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


                        tokens = doneJobPath.Split('*');

                        // linux client 
                        tokens[2] = Regex.Replace(tokens[2], @"\t|\n|\r", "");
                        // windows client tokens[2] = tokens[2].Substring(0, tokens[2].Length - 2);


                        Job job = _context.Jobs.First(v => v.managerName == tokens[2] && v.name == tokens[1]);
                        job.status = "finish";
                        job.path = doneJobPath.Split("*")[0];

                        _context.SaveChanges();

                    }



                    messageSent = Encoding.ASCII.GetBytes("bye");
                    byteSent = sender.Send(messageSent);


                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();





                    Client findSelectedClientAgain = _context.Clients.First(v => v.ip == ipAddress);

                    if (findSelectedClientAgain != null)
                    {
                        bool saveFailed;
                        do
                        {
                            saveFailed = false;
                            --findSelectedClientAgain.jobCount;

                            try
                            {
                                _context.SaveChanges();
                            }
                            catch (DbUpdateConcurrencyException e)
                            {
                                saveFailed = true;
                                e.Entries.Single().Reload();
                            }
                        } while (saveFailed);
                    }
                



















                }

                // Manage of Socket's Exceptions 
                catch (ArgumentNullException ane)
                {

                    Job job = _context.Jobs.First(v => v.managerName == tokens[2] && v.name == tokens[1]);
                    job.status = "fail";
                    _context.SaveChanges();
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }

                catch (SocketException se)
                {


                    Job job = _context.Jobs.First(v => v.managerName == tokens[2] && v.name == tokens[1]);
                    job.status = "fail";
                    _context.SaveChanges();
                    Debug.WriteLine("SocketException : {0}", se.ToString());
                }

                catch (Exception e)
                {

                    Job job = _context.Jobs.First(v => v.managerName == tokens[2] && v.name == tokens[1]);
                    job.status = "fail";
                    _context.SaveChanges();

                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }
            }

            catch (Exception e)
            {
                /*Job job = _context.Jobs.First(v => v.managerName == tokens[2] && v.name == tokens[1]);
                job.status = "fail";
                _context.SaveChanges();*/
                Console.WriteLine(e.ToString());
            }
        }




    }







}
