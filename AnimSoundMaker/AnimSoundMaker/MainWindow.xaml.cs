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
using System.Windows.Input;
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
        public Dictionary<TabItem, Editor_RASD> editorConnections = new();
        public Dictionary<TabItem, TreeViewItem> treeViewConnection = new();
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

        private void SaveAsFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog();  
            dialog.Filter = "All supported files|*.brasd;*.rasd;*rasp|Binary Revolution Animation Sound Data|*.brasd|Revolution Animation Sound Data|*.rasd|Revolution Animation Sound Project|*.rasp";
            bool? result = dialog.ShowDialog();
            if(result == true)
            {
                TrySaveFile(dialog.FileName);
            }
        }

        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            editorConnections.TryGetValue(((TabItem)TabControl.SelectedItem), out Editor_RASD editor);
            TrySaveFile(editor.Path);
        }

        private void CloseFile_Click(object sender, RoutedEventArgs e)
        {
            if (TabControl.Items.Count == 0) return;
            MessageBoxResult messageBoxResult = MessageBox.Show("Are you sure to close the file?", "Close Confirmation", MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                CloseFile();
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
            TreeViewItem item = PopulateTree(shortName);
            CreateTab(shortName, (RASD)rasd, item);
            LoadBasicData((RASD)rasd);
            return rasd;
        }

        private void TrySaveFile(string path)
        {
            TabItem currentTab = (TabItem)TabControl.SelectedItem;
            if (currentTab == null) return;

            if(editorConnections.TryGetValue(currentTab, out Editor_RASD editor))
            {
                RASD data = editor.GetData();
                switch (Path.GetExtension(path))
                {
                    case ".rasd":
                        SaveRASD(path, data);
                        break;
                }
                
            }
            
        }

        private void CloseFile()
        {
            TabItem currentTab = (TabItem)TabControl.SelectedItem;
            if (currentTab == null) return;
            treeViewConnection.TryGetValue(currentTab, out TreeViewItem value);
            value.IsSelected = true;

            ((TreeViewItem)value.Parent).Items.Remove(value);
            TabControl.Items.Remove(currentTab);
            
        }

        private TreeViewItem PopulateTree(string name, RASP? rasp = null)
        {
            TreeViewItem output = new();
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
                output = rasdNode;
                defaultAnim.Items.Add(rasdNode);
            }
            return output;
        }

        private void CreateTab(string name, RASD rasd, TreeViewItem treeItem, string projectName = @"<null>.rasp")
        {
            TabItem tabItem = new TabItem();
            tabItem.Header = $"{projectName} - {name}";
            tabItem.IsSelected = true;

            Editor_RASD editor= new Editor_RASD();
            editor.LoadData(rasd);
            Frame frame = new();
            frame.Content = editor;
            tabItem.Content = frame;
            tabItem.MouseEnter += TabItem_MouseEnter;

            TabControl.Items.Add(tabItem);
            editorConnections.Add(tabItem, editor);
            treeViewConnection.Add(tabItem, treeItem);
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

        private volatile TabItem hover;

        private void TabItem_MouseEnter(object sender, MouseEventArgs e)
        {
            TabItem item = (TabItem)sender;
            hover = item;
            Debug.WriteLine(item);
        }

        private void TabMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            TabControl.SelectedItem = hover;
        }

        private void ContextMenu_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {

        }
    }

    


}
