using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static lib_RASD;
using static lib_RASP;

namespace AnimSoundMaker
{
    public partial class MainWindow : Window
    {
        public static readonly string applicationPath = new Uri(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase)).LocalPath;

        [DisplayName("[General]")]
        public class BasicData
        {
            public string Creator { get; set; }
            public string Generator { get; set; }
            [DisplayName("Date Modified")]
            public string DateModified { get; set; }

            public BasicData()
            {
                Creator = "Unknown";
                Generator = "Unknown";
                DateModified = "Unknown";
            }

        }

        public BasicData Data { get; set; }

        public List<RASD> loadedFiles = new List<RASD>();
        public TreeViewItem defaultAnim;

        public MainWindow()
        {
            InitializeComponent();

            string[] args = Environment.GetCommandLineArgs();
            foreach(string arg in args)
            {
                if (!File.Exists(arg)) continue;
                if (Path.GetExtension(arg) == ".dll") continue;
                TryLoadFile(arg);
                
            }
                
            
          
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "All supported files|*.brasd;*.rasd;*rasp|Binary Revolution Animation Sound Data|*.brasd|Revolution Animation Sound Data|*.rasd|Revolution Animation Sound Project|*.rasp";
            bool? result = dialog.ShowDialog();
            
            if(result == true)
            {
                TryLoadFile(dialog.FileName);
            }
        }

        private RASD? TryLoadFile(string path)
        {
            RASD? rasd = null;
            switch (Path.GetExtension(path).Trim())
            {
                case ".brasd":
                    rasd = TryOpenBRASD(path);
                    break;
                case ".rasd":
                    rasd = TryOpenRASD(path);
                    break;
            }
            if (rasd == null) return null;

            string name = Path.GetFileName(path);
            string shortName = Path.GetFileNameWithoutExtension(path);
            loadedFiles.Add((RASD)rasd);
            PopulateTree(shortName);
            CreateTab(shortName, (RASD)rasd);
            LoadBasicData((RASD)rasd);
            SaveRASD("", loadedFiles[0]);
            return rasd;
        }

        private void PopulateTree(string name, RASP? rasp = null)
        {
            if (rasp == null)
            {
                if(!ProjectTree.Items.Cast<TreeViewItem>().Any(item => item.Header.ToString() == @"<null>.rasp"))
                {
                    TreeViewItem tempProjectName = new();
                    tempProjectName.Header = @"<null>.rasp";
                    
                    ProjectTree.Items.Add(tempProjectName);

                    TreeViewItem tempModelName = new();
                    tempModelName.Header = @"<null>.rmdl";
                    tempModelName.Foreground = Brushes.Red;
                    tempProjectName.Items.Add(tempModelName);

                    TreeViewItem tempAnimName = new();
                    tempAnimName.Header = @"<null>.rcha";
                    tempModelName.Items.Add(tempAnimName);

                    tempProjectName.IsExpanded = true;
                    tempModelName.IsExpanded = true;
                    tempAnimName.IsExpanded = true;

                    defaultAnim = tempAnimName;
                }

                TreeViewItem rasdNode = new();
                rasdNode.Header = name;
                rasdNode.IsSelected= true;
                defaultAnim.Items.Add(rasdNode);
            }
        }

        private void CreateTab(string name, RASD rasd, string projectName = @"<null>.rasp")
        {
            TabItem tabItem = new TabItem();
            tabItem.Header = $"{projectName} - {name}";
            tabItem.IsSelected = true;

            Editor_RASD editor= new Editor_RASD();
            editor.LoadData(rasd);
            Frame frame = new();
            frame.Content = editor;
            tabItem.Content = frame;

            TabControl.Items.Add(tabItem);
        }

        private void LoadBasicData(RASD rasd)
        {
            Data = new();
            Data.Creator = rasd.Header.CreatorName;
            Data.Generator = rasd.Header.Generator;
            Data.DateModified = rasd.Header.DataSaved;
            PropertyBox.SelectedObject = Data;
            PropertyBox.NameColumnWidth = 60;
        }

    }

    


}
