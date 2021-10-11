using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace TABresetGUI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public class Save
        {
            public string Name { get; set; }
            public string LastWrite { get; set; }
        }
        public DispatcherTimer timer;
        public string save_folder;
        public string tab_folder;
        public string cl_exec_path;
        public MainWindow()
        {
            InitializeComponent();
            if (string.IsNullOrEmpty(save_folder))
                save_folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"My Games\They Are Billions\Saves\");
            if (!Directory.Exists(save_folder)) throw new DirectoryNotFoundException(save_folder);
            if (string.IsNullOrEmpty(tab_folder))
                tab_folder = Path.Combine((string)Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam").GetValue("SteamPath"), @"steamapps\common\They Are Billions\").Replace('/', '\\');
            if (!Directory.Exists(tab_folder)) throw new DirectoryNotFoundException(tab_folder);
            Environment.CurrentDirectory = tab_folder;
            if (string.IsNullOrEmpty(cl_exec_path)) cl_exec_path = "TABRTreset.exe";
            if (!File.Exists(cl_exec_path)) throw new FileNotFoundException(cl_exec_path);
            WriteLine($"save folder=\"{save_folder}\"\nTAB folder=\"{tab_folder}\"\nCommandline program=\"{cl_exec_path}\"");
            timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, TimerElapsed, Dispatcher.CurrentDispatcher);
            timer.Start();
            RefreshSaveList();
        }
        void TimerElapsed(object sender, EventArgs e) => RefreshSaveList();
        void RefreshSaveList()
        {
            var selected_name = (listview1.SelectedItem as Save)?.Name;
            listview1.Items.Clear();
            foreach (var path in Directory.EnumerateFiles(save_folder, "*.zxsav"))
            {
                var save = new Save { Name = Path.GetFileNameWithoutExtension(path), LastWrite = File.GetLastWriteTime(path).ToString() };
                listview1.Items.Add(save);
                if (save.Name == selected_name) listview1.SelectedItem = save;
            }
        }
        void WriteLine(string text) => console.Text += text + '\n';
        void execute(params string[] args)
        {
            var process = new Process { StartInfo = new ProcessStartInfo { FileName = cl_exec_path, Arguments = string.Join(" ", args), RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false } };
            WriteLine($"executing command: {process.StartInfo.FileName} {process.StartInfo.Arguments}");
            process.Start();
            var output = process.StandardOutput;
            var error = process.StandardError;
            process.WaitForExit();
            WriteLine(output.ReadToEnd());
            WriteLine(error.ReadToEnd());
        }
        string selected_savename { get => (listview1.SelectedItem as Save)?.Name; }
        void tryopen(params string[] path)
        {
            try
            {
                foreach (var item in path) Process.Start(item);
            }
            catch (FileNotFoundException) { }
        }
        bool getfilename(out string filename, string InitialDirectory, string Filter = null, bool IsFile = true)
        {
            if (IsFile)
            {
                var fileDialog = new OpenFileDialog
                {
                    InitialDirectory = InitialDirectory,
                    Filter = Filter,
                };
                if (fileDialog.ShowDialog() == true) { filename = fileDialog.FileName; return true; }
            }
            else
            {
                var fileDialog = new VistaFolderBrowserDialog
                {
                    SelectedPath = InitialDirectory
                };
                if (fileDialog.ShowDialog() == true) { filename = fileDialog.SelectedPath; return true; }
            }
            filename = null;
            return false;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string filename;
            var tag = (sender as Button).Tag as string;
            switch (tag)
            {
                case "opensavefolder":
                    tryopen(save_folder);
                    break;
                case "reset":
                case "resetperk":
                    if (selected_savename != null)
                        execute(tag, selected_savename);
                    break;
                case "gencheck":
                    if (getfilename(out filename, save_folder, "ZXSave (*.zxsav)|*.zxsav"))
                        execute(tag, '"' + filename + '"');
                    break;
                case "genpswd":
                    execute(tag);
                    tryopen("savepswd.json", "datpswd.json");
                    break;
                case "unpacksave":
                    if (selected_savename != null)
                    {
                        execute(tag, selected_savename);
                        tryopen(Path.Combine(save_folder, selected_savename));
                    }
                    break;
                case "unpackdat":
                    if (getfilename(out filename, tab_folder, "dat (*.dat)|*.dat"))
                    {
                        execute(tag, '"' + filename + '"');
                        tryopen(Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename)));
                    }
                    break;
                case "packsave":
                    if (getfilename(out filename, save_folder, IsFile: false))
                        execute(tag, '"' + filename + '"');
                    break;
                case "packdat":
                    if (getfilename(out filename, tab_folder, IsFile: false))
                        execute(tag, '"' + filename + '"');
                    break;
                case "about":
                    WriteLine(@"
Github: https://github.com/waingt/TAB-research-tree-reset
百度贴吧: https://tieba.baidu.com/p/7305892887

推荐使用Github issues来报告Bugs
");
                    break;
            }
        }
    }
}
