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
            return "Backend";
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
            List<Job> patentJobsWithOutProgress = nameGroupJobs.Values.ToList();
            List<ParentJob> patentJobsWithProgress = new List<ParentJob>();

            for (int i=0;i< patentJobsWithOutProgress.Count;i++)
            {
                int progress = calculateProgressFor(patentJobsWithOutProgress[i]);
                patentJobsWithProgress.Add(new ParentJob(progress, patentJobsWithOutProgress[i]));
            }

            return JsonConvert.SerializeObject(patentJobsWithProgress.ToList());

        }

        private int calculateProgressFor(Job job)
        {
            
            int doneCount = 0;
            int totalCount = 0;
            int failCount = 0;
            string parentJobName = job.name.Split("-")[1];


            List<Job> managerJobs = _context.Jobs.Where(v => v.managerName == job.managerName).ToList();
            for (int i = 0; i < managerJobs.Count; i++)
            {

                String[] jobNameArr = managerJobs[i].name.Split("-");
                String localParentJobName = jobNameArr[1];
                if (localParentJobName == parentJobName)
                {
                    totalCount++;
                    if (managerJobs[i].status == "finish")
                    {
                        doneCount++;
                    }
                    else if (managerJobs[i].status == "fail")
                    {
                        failCount++;
                    }
                }
                
            }
            if(job.type!="Single Job")
            {
                //all jobs failed
                if (failCount == totalCount)
                {
                    return -1;
                }

                return 100 * doneCount / totalCount;


            }
            else
            {
                return 999;
            }
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
                IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress ipAddr = ipHost.AddressList[0];
                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), 8888);

                Socket sender = new Socket(IPAddress.Parse(ipAddress).AddressFamily,
                           SocketType.Stream, ProtocolType.Tcp);

                try
                {

                    sender.Connect(localEndPoint);

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



            //create a Job object to store in the Jobs table

            Job newJob = new Job();
            newJob.type = typeJob;
            newJob.status = "working";
            newJob.managerName = managerName;
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

            List<string> jobList = new List<string>();
            jobList.Add(baseStoragePath + "|" + selectedClient.name + "|" + managerName + "|" + jobName + "|" + newJob.type + "\n");

            executeClient2(selectedClient, jobList);


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
                if (!Directory.Exists(jobPath))
                {
                    Debug.WriteLine("jobPath is not exist");

                    Directory.CreateDirectory(jobPath);

                    Debug.WriteLine("jobPath is created");


                }

                //create a Job object to store in the Jobs table
                Job newJob = new Job();
                newJob.type = typeJob;
                newJob.status = "working";
                newJob.managerName = managerName;
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


                if (!Directory.Exists(jobPath))
                {
                    Debug.WriteLine("jobPath is not exist");
                    Directory.CreateDirectory(jobPath);
                    Debug.WriteLine("jobPath is created");
                }

                //create a Job object to store in the Jobs table

                Job newJob = new Job();
                newJob.type = typeJob;
                newJob.status = "working";
                newJob.managerName = managerName;
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

                IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress ipAddr = ipHost.AddressList[0];
                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), 8888);
                Socket sender = new Socket(IPAddress.Parse(ipAddress).AddressFamily,
                           SocketType.Stream, ProtocolType.Tcp);
                sender.Connect(localEndPoint);

                for (int i = 0; i < jobs.Count; i++)
                {

                    String message = jobs[i];
                    string[] tokens = null;

                    try
                    {

                        Console.WriteLine("Socket connected to -> {0} ",
                                      sender.RemoteEndPoint.ToString());

                        byte[] messageSent = Encoding.ASCII.GetBytes(message);
                        int byteSent = sender.Send(messageSent);


                        byte[] messageReceived = new byte[1024];

                        int byteRecv = sender.Receive(messageReceived);

                        Debug.WriteLine("Message from Server -> {111203}",
                              Encoding.ASCII.GetString(messageReceived,
                                                         0, byteRecv));

                        lock (_object)
                        {
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


                                Job job = _context.Jobs.First(v => v.managerName == tokens[2] && v.name == tokens[1]);
                                job.status = "finish";
                                job.path = doneJobPath.Split("*")[0];

                            
                                _context.SaveChanges();
                            


                            }
                      
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
                for(int i = 0; i < jobs.Count; i++)
                {
                    string managerName = jobs[i].Split("\\")[4].Split("|")[2];
                    string jobName = jobs[i].Split("\\")[4].Split("|")[3];
                    Job job = _context.Jobs.First(v => v.managerName == managerName && v.name == jobName);
                    job.status = "fail";
                    job.description = e.Message.ToString();

                    
                }

                _context.SaveChanges();
                Console.WriteLine(e.ToString());

            }
        }


        public static string getInputGroup(string inputFolder)
        {
            string groupNum = inputFolder.Split('/')[1]; ;
            return groupNum;

        }










    }
}
