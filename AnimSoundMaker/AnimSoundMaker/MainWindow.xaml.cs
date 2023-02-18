using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Xceed.Wpf.Toolkit.Panels;
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

        public struct FileData
        {
            public string Name;
            public string UUID;
            public string FilePath;
            public string AnimName;
            public string ModelName;
            public TreeViewItem treeItem;
            public TabItem tabItem;
            public RASD data;
            public Editor_RASD editor;
        }

        public BasicData Data { get; set; }

        public List<RASD> loadedFiles = new List<RASD>();
        public RASP? loadedProject;
        public TreeViewItem? defaultNode = null;
        public List<FileData> datas = new();
        public TreeViewItem defaultAnim;

        public MainWindow()
        {
            InitializeComponent();

            

            string[] args = Environment.GetCommandLineArgs();
            foreach(string arg in args.Skip(1))
            {
                if (!File.Exists(arg)) continue;
                TryImportFile(arg);
                
            }         
        }

        private void OpenProject_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Revolution Animation Sound Project|*.rasp";
            bool? result = dialog.ShowDialog();
            
            if(result == true)
            {
                TryImportFile(dialog.FileName);
            }
        }

        private void ImportFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "All supported files|*.brasd;*.rasd|Binary Revolution Animation Sound Data|*.brasd|Revolution Animation Sound Data|*.rasd";
            dialog.Multiselect = true;
            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                foreach (var item in dialog.FileNames)
                {
                    TryImportFile(item);
                }
            }
        }

        private void SaveAsFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog();  
            dialog.Filter = "All supported files|*.brasd;*.rasd|Binary Revolution Animation Sound Data|*.brasd|Revolution Animation Sound Data|*.rasd";
            bool? result = dialog.ShowDialog();
            if(result == true)
            {
                TabItem currentTab = (TabItem)TabControl.SelectedItem;
                if (currentTab == null) return;
                string name = currentTab.Header.ToString().Replace("__", "_");

                TrySaveFile(dialog.FileName, name);
            }
        }

        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            TabItem currentTab = (TabItem)TabControl.SelectedItem;
            if (currentTab == null) return;
            string name = currentTab.Header.ToString().Replace("__", "_");
            Editor_RASD? editor = null;
            foreach(var dat in datas)
            {
                if(dat.Name == name)
                {
                    editor = dat.editor; break;
                }
            }
            if (editor == null) return;

            TrySaveFile(editor.Path, name);
        }

        private void TreeViewItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not TreeViewItem) return;
            var item = (TreeViewItem)sender;
            TabItem? tabItem = null;
            foreach (var dat in datas)
            {
                if (dat.treeItem.Header == item.Header)
                {
                    tabItem = dat.tabItem; break;
                }
            }
            if (tabItem == null) return;
            if (TabControl.Items.Contains(tabItem)) return;
            TabControl.Items.Add(tabItem);
            tabItem.IsSelected = true;

        }

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            if (TabControl.Items.Count == 0) return;
            //MessageBoxResult messageBoxResult = MessageBox.Show("Are you sure to close the file?", "Close Confirmation", MessageBoxButton.YesNo);
            //if (messageBoxResult == MessageBoxResult.Yes)
            //{
            //    CloseTab();
            //}
            CloseTab();

        }

        private void CloseTab()
        {
            TabItem currentTab = (TabItem)TabControl.SelectedItem;
            if (currentTab == null) return;
            TabControl.Items.Remove(currentTab);

        }

        private RASD? TryImportFile(string path)
        {
            RASD? rasd = null;
            RASP rasp = new();

            List<RASD?> rasds = new();

            string name = Path.GetFileName(path);
            string shortName = Path.GetFileNameWithoutExtension(path);

            switch (Path.GetExtension(path).Trim())
            {
                case ".brasd":
                    rasd = TryOpenBRASD(path);
                    if (loadedProject == null) rasp = ProjectFromRASD(shortName, rasd, Path.GetFileName(Path.GetDirectoryName(path)));
                    else rasp = AddSoundToProject(defaultAnim, shortName, rasd, (RASP)loadedProject);
                    rasds.Add(rasd);
                    break;
                case ".rasd":
                    rasd = TryOpenRASD(path);
                    if (loadedProject == null) rasp = ProjectFromRASD(shortName, rasd, Path.GetFileName(Path.GetDirectoryName(path)));
                    else rasp = AddSoundToProject(defaultAnim, shortName, rasd, (RASP)loadedProject);
                    rasds.Add(rasd);
                    break;                
            }

            loadedProject = rasp;
            ProjectTree.Items.Clear();
            var itS = PopulateTree(shortName, (RASP)loadedProject, path);
            LoadBasicData(rasd);
            return rasd;
        }

        private void TrySaveFile(string path, string name)
        {
            Editor_RASD? editor = null;
            foreach (var dat in datas)
            {
                if (dat.Name == name)
                {
                    editor = dat.editor; break;
                }
            }
            if (editor == null) return;

            RASD data = editor.GetData();
            switch (Path.GetExtension(path))
            {
                case ".rasd":
                    SaveRASD(path, data);
                    break;
                case ".brasd":
                    SaveBRASD(path, data);
                    break;
            }
               
        }

        private List<TreeViewItem> PopulateTree(string name, RASP rasp, string path = "")
        {
            AnimSoundProject project = rasp.Project;
            List<TreeViewItem> output = new();
            TreeViewItem itRoot = new();

            itRoot.Header = rasp.Project.Name;
            itRoot.Tag = "Project";
            itRoot.IsExpanded = true;

            foreach(var model in project.Models)
            {
                TreeViewItem itModel = new();
                itModel.Header = model.Name;
                itModel.Tag = "Model";
                itModel.IsExpanded = true;

                foreach(var anim in model.Anims)
                {
                    TreeViewItem itAnim = new();
                    itAnim.Header = anim.Name;
                    itAnim.Tag = "Animation";
                    itAnim.IsExpanded = true;

                    foreach(var sound in anim.Sounds)
                    {
                        TreeViewItem itSound = new();
                        itSound.Header = sound.Name;
                        itSound.Tag = "Sound";
                        itSound.MouseDoubleClick += TreeViewItem_DoubleClick;

                        if (datas.Cast<FileData>().Any(i => i.Name == sound.Name))
                        {
                            foreach(var dat in datas)
                            {
                                if (dat.Name == name && dat.ModelName == anim.Name)
                                {
                                    itSound = dat.treeItem;
                                }
                            }    
                        }
                        else
                        {
                            
                            FileData dat = new();
                            dat.Name = name;
                            dat.treeItem = itSound;
                            dat.tabItem = CreateTab(name, sound.Data, itSound, out Editor_RASD editor);
                            dat.data = sound.Data;
                            dat.editor= editor;
                            dat.FilePath = path;
                            dat.ModelName = anim.Name;
                            Guid guid = Guid.NewGuid();
                            dat.UUID = guid.ToString();
                            datas.Add(dat);
                        }
                        defaultAnim = itAnim;
                        
                        itAnim.Items.Add(itSound);
                        output.Add(itSound);
                    }

                    itModel.Items.Add(itAnim);
                }  
                itRoot.Items.Add(itModel);
            }
            ProjectTree.Items.Add(itRoot);

            return output;
        }

        private TabItem CreateTab(string name, RASD rasd, TreeViewItem treeItem, out Editor_RASD editor)
        {
            TabItem tabItem = new TabItem();
            Debug.WriteLine(name);
            tabItem.Header = name.Replace("_","__");
            tabItem.IsSelected = true;

            editor= new Editor_RASD();
            editor.LoadData(rasd);
            Frame frame = new();
            frame.Content = editor;
            tabItem.Content = frame;
            tabItem.MouseEnter += TabItem_MouseEnter;

            TabControl.Items.Add(tabItem);

            return tabItem;
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
        }

        private void TabMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            TabControl.SelectedItem = hover;
        }

    }

    


}
