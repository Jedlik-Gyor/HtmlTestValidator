using HtmlTestValidator.Models.Project;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
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


        private ChromeOptions headLessChromeOption;
        private string path;

        public Evaluation(string path, int numberOfSteps)
        {
            this.path = path.Replace('\\', '/');
            this.Name = Path.GetFileName(path);
            StepPoints = new double[numberOfSteps];
            StepErrors = new string[numberOfSteps];
            Presenced = Directory.GetFiles(path).Length + Directory.GetDirectories(path).Length > 0;

            headLessChromeOption = new ChromeOptions();
            headLessChromeOption.AddArguments("headless");
            //headLessChromeOption.AddArguments("--window-size=1017,973");
        }

        public void Evaluate(Project.Project project)
        {
            using (var driver = new ChromeDriver(headLessChromeOption))
            {
                driver.Manage().Window.Size = new System.Drawing.Size(1017, 973);
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

                //driver.Navigate().GoToUrl($"file:///{this.path}/haz01.html");
                //var xx = driver.FindElement(By.XPath("//body"));
                //System.Windows.MessageBox.Show(xx.GetCssValue("width"));

                //element = driver.FindElement(By.XPath("//head/link[@rel='stylesheet'][2][contains(@href, 'style.css')]"));


                foreach (var (step, index) in project.Steps.Select((value, i) => (value, i)))
                {
                    var passes = 0;
                    foreach (var condition in step.Conditions)
                    {
                        try
                        {
                            driver.Navigate().GoToUrl($"file:///{this.path}/{condition.URL}");                            
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
    }
}
