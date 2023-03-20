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
                txtTaskJsonPath.Text = File.ReadAllText(openFileDialog.FileName);
        }

        private void btnSelectTestsParentFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    txtTestParentFolder.Text = dialog.SelectedPath;
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
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
                                       .Select(d => new Evaluation(d, project.Steps.Length))
                                       .ToList();

            Parallel.ForEach(evaluations,
                             new ParallelOptions { MaxDegreeOfParallelism = 5 },
                             evaluation => evaluation.Evaluate(project));

            evaluationSheet = new EvaluationSheet(project, evaluations);
            evaluationSheet.SaveAs("teszt.xlsx");

            evaluationSheet.Dispose();
            Process.Start(new ProcessStartInfo("teszt.xlsx") { UseShellExecute = true });
            //this.Close();
        }
    }
}
