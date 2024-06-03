using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace HtmlTestValidator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            //base.OnStartup(e);
            var taskJsonPath = "";
            var testParentFolderPath = "";
            if (e.Args.Length >= 1)
                taskJsonPath = e.Args[0].Trim();
            if (e.Args.Length >= 2)
                testParentFolderPath = e.Args[1].Trim();
            MainWindow wnd = new MainWindow(taskJsonPath, testParentFolderPath);
            wnd.Show();
            
        }
    }
}
