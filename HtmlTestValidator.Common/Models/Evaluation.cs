using Docker.DotNet;
using Docker.DotNet.Models;
using HtmlTestValidator.Models.Project;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtmlTestValidator.Models
{
    public class Evaluation: IDisposable
    {
        public string Name { get; private set; }
        public double[] StepPoints { get; private set; }
        public string[] StepErrors { get; private set; }
        public bool Presenced { get; private set; }
        private readonly string hubURL;
        private DockerClient dockerClient;
        private int evaluationIndex;

        public event EventHandler<string> LogEvent;

        private ChromeOptions headLessChromeOption;
        private string path;

        public Evaluation(string path, int numberOfSteps, string hubURL = "https://selenium-grid.jedlik.cloud/wd/hub", int index = 0)
        {
            this.path = path.Replace('\\', '/').Replace("#", "%23");
            this.Name = Path.GetFileName(path);
            StepPoints = new double[numberOfSteps];
            StepErrors = new string[numberOfSteps];
            Presenced = Directory.GetFiles(path).Length + Directory.GetDirectories(path).Length > 0;

            headLessChromeOption = new ChromeOptions();
            this.hubURL = hubURL;
            if (!hubURL.Contains("jedlik.cloud"))
                dockerClient = new DockerClientConfiguration().CreateClient();
            evaluationIndex = index;
        }

        public void Dispose()
        {
            dockerClient?.Dispose();
        }

        public void Evaluate(Project.Project project, string pageURL = null)
        {
            //Log($"Dolgozat: {Path.GetFileName(this.path)}");

            using (var driver = new RemoteWebDriver(new Uri(hubURL), headLessChromeOption.ToCapabilities()))
            {
                driver.Manage().Window.Size = new System.Drawing.Size(1017, 973);
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

                if (project.BeforeOperations != null)
                    foreach (var beforeOperation in project.BeforeOperations)
                        beforeOperation.DoIt(this.path);

                foreach (var (step, index) in project.Steps.Select((value, i) => (value, i)))
                {
                    Log($"\tEllenőrzés: {step.Description}");
                    var passes = 0;
                    foreach (var condition in step.Conditions)
                    {
                        try
                        {
                            if (this.dockerClient == null)
                                driver.Navigate().GoToUrl($"https://selenium-test.jedlik.cloud/{condition.URL}");
                            else
                                driver.Navigate().GoToUrl($"http://10.5.99.{100 + evaluationIndex}/{condition.URL}");

                            try
                            {
                                var elementPath = condition.Element is ElementByXPath ? ((ElementByXPath)condition.Element).XPath : ((ElementByCssSelector)condition.Element).CssSelector;
                                Log($"\t\tVizsgált elem: {elementPath}");
                            }
                            catch { }

                            if (condition.Assertion is AssertionCount)
                                passes += condition.Assertion.Assert(condition.Element.FindElements(driver), Log) ? 1 : 0;
                            else if (condition.Assertion is AssertionHtmlValidation)
                                passes += ((AssertionHtmlValidation)(condition.Assertion)).Assert($"{this.path}/{condition.URL}") ? 1 : 0;
                            else if (condition.Assertion is AssertionCssValidation)
                                passes += ((AssertionCssValidation)(condition.Assertion)).Assert($"{this.path}/{condition.URL}") ? 1 : 0;
                            else
                            {
                                var element = condition.Element.FindElement(driver);
                                foreach (var action in condition.Element.ActionsBefore)
                                    action.DoIt(driver, element);
                                passes += condition.Assertion.Assert(element, Log) ? 1 : 0;
                            }
                            Log($"\t\t\tEredmény: {passes} / {project.Steps[index].ConditionsNumberHaveToPass}");
                        }
                        catch (Exception ex)
                        {
                            StepErrors[index] += $"{ex.Message}\r";
                        }
                        this.StepPoints[index] = passes >= project.Steps[index].ConditionsNumberHaveToPass ? project.Steps[index].Points : 0;
                    }
                }
            }

            if (this.dockerClient != null)
            {
                try
                {
                    dockerClient.Containers.RemoveContainerAsync($"nginx-for-selenium-{evaluationIndex}", new ContainerRemoveParameters() { Force = true});
                }
                catch { }
            }
        }

        public void CopyFilesToWebServer()
        {
            using (var client = new SftpClient("selenium-grid.jedlik.cloud", 30022, "tester", "N@gyonT1tk0s"))
            {
                client.Connect();
                emptyDir(client, "");
                uploadDir(client, "");
            }
        }

        public async Task CreateNginxContainer()
        {
            await dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters()
            {
                Image = "nginx",
                ExposedPorts = new Dictionary<string, EmptyStruct>
                {
                   { "80", default(EmptyStruct) }
                },
                HostConfig = new HostConfig
                {
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                          { "80", new List<PortBinding> { new PortBinding { HostPort = $"{8001 + evaluationIndex}" } } }
                    },
                    Mounts = new List<Mount>
                    {
                         new Mount
                         {
                             Source = this.path,
                             Target = "/usr/share/nginx/html",
                             Type = "bind"
                         }
                    },
                    
                },
                Name = $"nginx-for-selenium-{evaluationIndex}",
                NetworkingConfig = new NetworkingConfig
                {
                    EndpointsConfig = new Dictionary<string, EndpointSettings>
                    {
                        {
                            "selenium-test_vpcbr",
                            new EndpointSettings
                            {
                                IPAMConfig = new EndpointIPAMConfig
                                {
                                    IPv4Address = $"10.5.99.{100 + evaluationIndex}"
                                }
                            }
                        }
                    },
                }
            });
            Thread.Sleep(500);
            await dockerClient.Containers.StartContainerAsync($"nginx-for-selenium-{evaluationIndex}", new ContainerStartParameters());
        }

        private void emptyDir(SftpClient client, string subDirName)
        {
            Log($"\tTávoli könyvtár törlése: {subDirName}");
            client.ChangeDirectory($"/files/{subDirName}");
            foreach (var f in client.ListDirectory("./").Where(n => !n.FullName.EndsWith("/..") && !n.FullName.EndsWith("/.")))
                if (f.IsDirectory)
                {
                    emptyDir(client, $"{subDirName}{f.Name}/");
                    client.DeleteDirectory(f.FullName);
                }

            client.ChangeDirectory($"/files/{subDirName}");
            foreach (var f in client.ListDirectory("./").Where(n => !n.FullName.EndsWith("/..") && !n.FullName.EndsWith("/.")))
                if (f.IsRegularFile)
                {
                    client.DeleteFile(f.FullName);
                }
        }

        private void uploadDir(SftpClient client, string subDirName)
        {
            Log($"\tFájlok feltöltése: {subDirName}");
            client.ChangeDirectory($"/files/{subDirName}");
            foreach (var file in Directory.GetFiles(Path.Combine(this.path, subDirName)))
                if (Path.GetExtension(file) != ".mkv" && Path.GetExtension(file) != ".avi")
                    using (var fileStream = new FileStream(file, FileMode.Open))
                    {
                        client.UploadFile(fileStream, Path.GetFileName(file));
                    }

            foreach (var dir in Directory.GetDirectories(Path.Combine(this.path, subDirName)))
            {
                client.ChangeDirectory($"/files/{subDirName}");
                client.CreateDirectory(Path.GetFileName(dir));
                uploadDir(client, $"{subDirName}{Path.GetFileName(dir)}/");
            }
        }

        public void Log(string message)
        {
            if (LogEvent is not null)
            {
                LogEvent.Invoke(this, message);
            }
        }
    }
}
