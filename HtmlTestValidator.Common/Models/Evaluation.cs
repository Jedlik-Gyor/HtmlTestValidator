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
    public class Evaluation
    {
        public string Name { get; private set; }
        public double[] StepPoints { get; private set; }
        public string[] StepErrors { get; private set; }
        public bool Presenced { get; private set; }

        public event EventHandler<string> LogEvent;

        private ChromeOptions headLessChromeOption;
        private string path;

        public Evaluation(string path, int numberOfSteps)
        {
            this.path = path.Replace('\\', '/').Replace("#", "%23");
            this.Name = Path.GetFileName(path);
            StepPoints = new double[numberOfSteps];
            StepErrors = new string[numberOfSteps];
            Presenced = Directory.GetFiles(path).Length + Directory.GetDirectories(path).Length > 0;

            headLessChromeOption = new ChromeOptions();
            //headLessChromeOption.BinaryLocation = @"C:\Program Files\Google\Chrome Beta\Application\chrome.exe";
            //headLessChromeOption.AddArguments("headless");
            //headLessChromeOption.AddArguments("--window-size=1017,973");
        }

        public void Evaluate(Project.Project project)
        {
            Log($"Dolgozat: {Path.GetFileName(this.path)}");
            this.CopyFilesToWebServer();

            //using (var driver = new ChromeDriver(headLessChromeOption))
            using (var driver = new RemoteWebDriver(new Uri("https://selenium-grid.jedlik.cloud/wd/hub"), headLessChromeOption.ToCapabilities()))
            {
                driver.Manage().Window.Size = new System.Drawing.Size(1017, 973);
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

                //driver.Navigate().GoToUrl($"file:///{this.path}/haz01.html");
                //var xx = driver.FindElement(By.XPath("//body"));
                //System.Windows.MessageBox.Show(xx.GetCssValue("width"));

                //element = driver.FindElement(By.XPath("//head/link[@rel='stylesheet'][2][contains(@href, 'style.css')]"));

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
                            driver.Navigate().GoToUrl($"https://selenium-test.jedlik.cloud/{condition.URL}");
                            if (condition.Assertion is AssertionCount)
                                passes += condition.Assertion.Assert(condition.Element.FindElements(driver)) ? 1 : 0;
                            else if (condition.Assertion is AssertionHtmlValidation)
                                passes += ((AssertionHtmlValidation)(condition.Assertion)).Assert($"{this.path}/{condition.URL}") ? 1 : 0;
                            else if (condition.Assertion is AssertionCssValidation)
                                passes += ((AssertionCssValidation)(condition.Assertion)).Assert($"{this.path}/{condition.URL}") ? 1 : 0;
                            else
                            {
                                var element = condition.Element.FindElement(driver);
                                foreach (var action in condition.Element.ActionsBefore)
                                    action.DoIt(driver, element);
                                passes += condition.Assertion.Assert(element) ? 1 : 0;
                            }
                        }
                        catch (Exception ex)
                        {
                            StepErrors[index] += $"{ex.Message}\r";
                        }
                        this.StepPoints[index] = passes >= project.Steps[index].ConditionsNumberHaveToPass ? project.Steps[index].Points : 0;
                    }
                }
            }
        }

        private void CopyFilesToWebServer()
        {
            using (var client = new SftpClient("selenium-grid.jedlik.cloud", 30022, "tester", "N@gyonT1tk0s"))
            {
                client.Connect();
                emptyDir(client, "");
                uploadDir(client, "");
            }
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

        private void Log(string message)
        {
            if (LogEvent is not null)
                LogEvent.Invoke(this, message);
        }
    }
}
