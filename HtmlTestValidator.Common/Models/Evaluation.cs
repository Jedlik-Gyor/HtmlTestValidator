using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2013.Drawing.ChartStyle;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Spreadsheet;
using HtmlTestValidator.Models.Project;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static HtmlTestValidator.Models.Project.Action;

namespace HtmlTestValidator.Models
{
    public class Evaluation
    {
        public string Name { get; private set; }
        public double[] StepPoints { get; private set; }
        public string[] StepErrors { get; private set; }
        public bool Presenced { get; private set; }

        private ChromeOptions headLessChromeOption;
        private FirefoxOptions firefoxOption;
        private string path;
        /*private static int startPort    = 6300;*/
        private int localhostPort       = 8080;
        private Process? npmProcess     = null;
        private bool useNpm = false;

        private StreamWriter errorFile;

        public Evaluation(string path, int numberOfSteps)
        {
            

            this.path           = path.Replace('\\', '/');
            this.Name           = Path.GetFileName(path);
            //this.localhostPort  = startPort++;
            StepPoints          = new double[numberOfSteps];
            StepErrors          = new string[numberOfSteps];
            Presenced           = Directory.GetFiles(path).Length + Directory.GetDirectories(path).Length > 0;

            this.errorFile = new StreamWriter("f:/" + this.Name + "-error.txt", false);
            LogHeaderWriteLine("Evaulation created...");

            this.firefoxOption = new FirefoxOptions();
            this.firefoxOption.AddArgument("--headless");
            this.firefoxOption.AddArgument("--ignore-certificate-errors");
            //this.firefoxOption.AddArgument("--remote-debugging-port=" + (this.localhostPort + 1500));

            this.headLessChromeOption = new ChromeOptions();
            this.headLessChromeOption.AddArguments("headless");
            this.headLessChromeOption.AddArgument("ignore-certificate-errors");
            //this.headLessChromeOption.AddArgument("remote-debugging-port="+ (this.localhostPort + 1500));
            //headLessChromeOption.AddArguments("--window-size=1017,973 ");
        }

        public void Evaluate(Project.Project project)
        {
            Thread.Sleep(new Random().Next(800, 2000));
            LogHeaderWriteLine("Evaulation start...");
            try
            {
                if (project.LocalStartDir != null && project.LocalStartDir != "") IfNodeModulesNotExists(project.LocalStartDir);
                if (project.NpmStartDir != null && project.NpmStartDir != "") IfNodeModulesNotExists(project.NpmStartDir);

                if (project.LocalStartDir != null && project.LocalStartDir == "")
                {
                    if (project.Steps.Count() > 0 && project.Steps.First().Conditions.Count() > 0)
                    {
                        IfNodeModulesNotExists(project.Steps.First().Conditions.First().URL);
                    }
                }
                LogHeaderWriteLine("npm install finished.");
            }
            catch (Exception ex)
            {
                ErrorWriteLine(ex.Message);
            }

            if (project.NpmStartDir != null)
            {
                
                Regex regex = new Regex("localhost:(\\d{4})", RegexOptions.IgnoreCase);

                var npm = new ProcessStartInfo
                {
                    FileName = "cmd",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding= Encoding.UTF8,
                    StandardErrorEncoding= Encoding.UTF8,
                    WorkingDirectory = Path.Combine(this.path, project.NpmStartDir)
                };
                this.npmProcess = Process.Start(npm);
                this.useNpm = true;
                this.npmProcess.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
                {
                    if (!String.IsNullOrEmpty(e.Data))
                    {
                        if (regex.IsMatch(e.Data))
                        {
                            this.localhostPort = Convert.ToInt32(regex.Match(e.Data).Groups[1].Value);
                        }
                    }
                    ErrorWriteLine(e.Data);
                });
                this.npmProcess.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler((sender, e) =>
                {
                    if (e.Data != null && e.Data.Contains("Error: "))
                    {
                        ErrorWriteLine("node indulási hiba, visszaállunk lokális fájl vizsgálatra.");
                        this.useNpm = false;

                    }
                    ErrorWriteLine(e.Data);
                });

                //this.npmProcess.StandardInput.WriteLine("set PORT="+(this.localhostPort));
                //this.npmProcess.StandardInput.WriteLine("node --inspect="+ (this.localhostPort +1000)+ " ./bin/www");
                this.npmProcess.StandardInput.WriteLine("node --inspect ./bin/www");

                this.npmProcess.BeginErrorReadLine();
                Thread.Sleep(2000);
                this.npmProcess.BeginOutputReadLine();
                LogHeaderWriteLine("node running...");
            }
            
            Thread.Sleep(1000);
            
            using (var driver = new ChromeDriver(this.headLessChromeOption))
            //using (var driver = new FirefoxDriver(this.firefoxOption))
            {
                LogHeaderWriteLine("Chrome created...");
                driver.Manage().Window.Size = new System.Drawing.Size(1017, 973);
                
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));
                wait.Until(x=> 
                    {
                        return ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState").Equals("complete");
                    }
                );
                LogHeaderWriteLine("Steps...");
                Thread.Sleep(5000);
                foreach (var (step, index) in project.Steps.Select((value, i) => (value, i)))
                {
                    var passes = 0;
                    foreach (var condition in step.Conditions)
                    {
                        try
                        {
                            string url = "";
                            if (this.useNpm && condition.Assertion != null)
                            {
                                url = String.Format($"http://localhost:{this.localhostPort}/{condition.URL}");
                            } else
                            {
                                url = String.Format($"file:///{this.path}/{project.LocalStartDir}/{condition.URL}");
                            }
                            driver.Navigate().GoToUrl(url);
                            var logs = driver.Manage().Logs.GetLog(LogType.Browser);

                            foreach (var log in logs)
                            {
                                ErrorWriteLine(log.ToString());
                            }
                            if (condition.Assertion != null)
                                condition.Assertion.ProjectName = this.Name;

                            if (condition.Assertion != null && condition.Assertion is AssertionCount)
                                passes += condition.Assertion.Assert(condition.Element.FindElements(driver)) ? 1 : 0;
                            else if (condition.Assertion == null && condition.AssertionDB != null)
                            {
                                try
                                {
                                    passes += condition.AssertionDB.Assert(condition.DBElement.FindElement(url),
                                        new MySqlConnection("server=localhost;user=root;password=;database=" + condition.DBElement.Database)) ? 1 : 0;
                                } 
                                catch (Exception ex)
                                {
                                    ErrorWriteLine(ex.Message);
                                }
                            }
                            else if (condition.Assertion != null && condition.Assertion is AssertionHtmlValidation)
                                passes += ((AssertionHtmlValidation)(condition.Assertion)).Assert(url) ? 1 : 0;
                            else if (condition.Assertion != null && condition.Assertion is AssertionCssValidation)
                                passes += ((AssertionCssValidation)(condition.Assertion)).Assert(url) ? 1 : 0;
                            else if (condition.Assertion != null)
                            {
                                var element = condition.Element.FindElement(driver);
                                object result = null;
                                foreach (var action in condition.Element.ActionsBefore)
                                {
                                    result = action.DoIt(driver, element);
                                }

                                passes += condition.Assertion.Assert(element, result) ? 1 : 0;
                            }
                            else
                            {

                            }
                        }
                        catch (Exception ex)
                        {
                            ErrorWriteLine(ex.Message);
                            StepErrors[index] += $"{ex.Message}\r";
                        }
                        this.StepPoints[index] = passes >= project.Steps[index].ConditionsNumberHaveToPass ? project.Steps[index].Points : 0;
                    }
                }
            }
            LogHeaderWriteLine("Steps end");

            if (this.npmProcess != null)
            {
                foreach (var node in Process.GetProcessesByName("node"))
                {
                    node.Kill();
                }
                this.npmProcess.Kill();
                LogHeaderWriteLine("Node kill...");
            }
            this.errorFile.Flush();
            this.errorFile.Close();
        }

        private void ErrorWriteLine(string error)
        {
            try
            {
                //Debug.WriteLine($"{this.Name}: {error}");
                if (error != null && error != "")
                {
                    this.errorFile.WriteLine($"{this.Name}: {error}");
                }

            } catch { }
        }

        private void LogHeaderWriteLine(string error)
        {
            try
            {
                //Debug.WriteLine($"{this.Name}: {error}");
                if (error != null && error != "")
                {
                    this.errorFile.WriteLine();
                    this.errorFile.WriteLine($"={DateTime.Now.ToLongTimeString()}======================================================");
                    this.errorFile.WriteLine($"{this.Name}: {error}");
                }

            }
            catch { }
        }

        private void IfNodeModulesNotExists(string conditionURL)
        {
            if (File.Exists(Path.Combine(this.path, conditionURL, "package.json")) && !Directory.Exists(Path.Combine(this.path, conditionURL, "node_modules")))
            {
                var psiNpmRunDist = new ProcessStartInfo
                {
                    FileName = "cmd",
                    RedirectStandardInput   = true,
                    RedirectStandardError   = true,
                    RedirectStandardOutput  = true,
                    StandardOutputEncoding  = Encoding.UTF8,
                    StandardErrorEncoding   = Encoding.UTF8,
                    WorkingDirectory        = Path.Combine(this.path, conditionURL)
                };
                var pNpmRunDist = Process.Start(psiNpmRunDist);
                pNpmRunDist.BeginErrorReadLine();
                pNpmRunDist.BeginOutputReadLine();
                pNpmRunDist.StandardInput.WriteLine("npm i & exit");
                pNpmRunDist.WaitForExit();
                pNpmRunDist.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
                {
                    ErrorWriteLine(e.Data);
                });
                pNpmRunDist.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
                {
                    ErrorWriteLine("Error: "+e.Data);
                });
            }
        }
    }
}
