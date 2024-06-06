using HtmlTestValidator.Models;
using HtmlTestValidator.Models.Project;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace HtmlTestValidator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        EvaluationSheet evaluationSheet = null;

        public MainWindow(string taskJsonPath, string testParentFolderPath)
        {
            InitializeComponent();
            txtTaskJsonPath.Text = taskJsonPath;
            txtTestParentFolder.Text = testParentFolderPath;
        }

        private void btnSelectTaskJsonPath_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
                txtTaskJsonPath.Text = openFileDialog.FileName;
        }

        private void btnSelectTestsParentFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    txtTestParentFolder.Text = dialog.SelectedPath;
        }

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            messageBar.Clear();
            if (!File.Exists(txtTaskJsonPath.Text))
            {
                messageBar.Error("A definicós állomány a megadott elérési úton nem létezik");
                return;
            }
            if (!Directory.Exists(txtTestParentFolder.Text))
            {
                messageBar.Error("A beadott dolgozatokhoz megadott könyvtár nem létezik");
                return;
            }
            Project project;
            try
            {
                project = Project.ReadFromFile(txtTaskJsonPath.Text);
            }
            catch (Exception ex)
            {
                messageBar.Error($"Json beolvaási hiba: {ex.Message}");
                return;
            }
            var evaluations = Directory.GetDirectories(txtTestParentFolder.Text)
                                       .Select((d, i) => new Evaluation(d, project.Steps.Length, (rbLocalDocker.IsChecked == true ? "http://localhost:4444/wd/hub" : "https://selenium-grid.jedlik.cloud/wd/hub"), i))
                                       .ToList();

            File.WriteAllText("feldolgozás.log", "");


            object lockObject = new object();
            var docker = rbLocalDocker.IsChecked == true;
            //foreach (var evaluation in evaluations)
            var options = new ParallelOptions { MaxDegreeOfParallelism = docker ? 5 : 1 };
            await Parallel.ForEachAsync(evaluations, options, async (evaluation, token) =>
            {
                File.WriteAllText($"feldolgozás - {evaluation.Name}.log", "");
                evaluation.LogEvent += (sender, message) =>
                {
                    lock(lockObject)
                        File.AppendAllLines("feldolgozás.log", new string[] { message });
                    File.AppendAllLines($"feldolgozás - {evaluation.Name}.log", new string[] { message });
                };
                evaluation.Log($"Dolgozat: {evaluation.Name}");
                if (docker)
                    await evaluation.CreateNginxContainer();
                else
                    evaluation.CopyFilesToWebServer();
                evaluation.Evaluate(project);
            });

            evaluationSheet = new EvaluationSheet(project, evaluations);
            evaluationSheet.SaveAs("teszt.xlsx");

            evaluationSheet.Dispose();
            Process.Start(new ProcessStartInfo("teszt.xlsx") { UseShellExecute = true });
            //this.Close();
        }
    }
}
