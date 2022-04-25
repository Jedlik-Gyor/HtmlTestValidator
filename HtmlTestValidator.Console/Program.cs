using System;
using System.Linq;
using HtmlTestValidator.Models;
using HtmlTestValidator.Models.Project;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HtmlTestValidator
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length >= 2) {
                var taskJsonPath = args[0];
                var testParentFolderPath = args[1];

                EvaluationSheet evaluationSheet = null;
                if (!File.Exists(taskJsonPath))
                {
                    Console.WriteLine("A definicós állomány a megadott elérési úton nem létezik");
                    return;
                }
                if (!Directory.Exists(testParentFolderPath))
                {
                    Console.WriteLine("A beadott dolgozatokhoz megadott könyvtár nem létezik");
                    return;
                }
                Project project;
                try
                {
                    project = Project.ReadFromFile(taskJsonPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Json beolvaási hiba: {ex.Message}");
                    return;
                }
                var evaluations = Directory.GetDirectories(testParentFolderPath)
                                        .Select(d => new Evaluation(d, project.Steps.Length))
                                        .ToList();

                Parallel.ForEach(evaluations,
                                new ParallelOptions { MaxDegreeOfParallelism = 5 },
                                evaluation => evaluation.Evaluate(project));

                evaluationSheet = new EvaluationSheet(project, evaluations);
                var fileName = $"teszt{DateTime.Now:yyyy-mm-dd HH.mm.ss}.xlsx";
                evaluationSheet.SaveAs(fileName);

                evaluationSheet.Dispose();
                Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true });
            }
        }

        
    }
}
