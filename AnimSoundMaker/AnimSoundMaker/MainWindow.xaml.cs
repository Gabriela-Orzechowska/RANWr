﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using gablibela;
using static gablibela.lib_RASP;
using static gablibela.lib_RASD;

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
            public bool newFile;


        }

        public BasicData Data { get; set; }

        public List<RASD> loadedFiles = new List<RASD>();
        public lib_RASP.RASP? loadedProject;
        public TreeViewItem? defaultNode = null;
        public List<FileData> datas = new();
        public TreeViewItem defaultAnim;

        public MainWindow()
        {
            InitializeComponent();
            this.AllowDrop = true;
            this.Drop += MainWindow_Drop;
            this.KeyDown+= MainWindow_KeyDown;
            this.PreviewMouseDown += MainWindow_PreviewMouseDown;
            string[] args = Environment.GetCommandLineArgs();
            foreach(string arg in args.Skip(1))
            {
                TryImportFile(arg);      
            }         
        }
                
        private void MainWindow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            /*
            if (e.ClickCount < 2)
            {
                FileData data = datas.Cast<FileData>().FirstOrDefault(i => i.treeItem == currentTreeViewItem);
                if (data.editor != null)
                {
                    Debug.WriteLine(data.editor.DataGrid.);
                    if(data.editor.DataGrid.SelectedCells.Count > 0)
                    {
                        var i = data.editor.DataGrid.CommitEdit();
                        Keyboard.ClearFocus();
                    }
                    
                }
                               
            }
            */
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            
            if(Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S)
            {
                if (loadedProject == null) return;
                TabItem currentTab = (TabItem)TabControl.SelectedItem;
                if (currentTab == null) return;
                char[] tabHeader = "T_".ToCharArray();
                string name = currentTab.Name.ToString().Replace("__", "_").TrimStart(tabHeader);
                Editor_RASD? editor = null;
                bool newFile = false;
                foreach (var dat in datas)
                {
                    if (dat.Name == name)
                    {
                        editor = dat.editor; 
                        newFile = dat.newFile; break;
                    }
                }
                if (newFile)
                {
                    Debug.WriteLine("Save as:");
                    SaveAsTreeItem_Click(sender, e);
                    Debug.WriteLine("Nothing");
                    return;
                }
                if (editor == null) return;

                TrySaveFile(editor.Path, name);
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (datas.Count > 0)
            {
                var result = MessageBox.Show("Are you sure to close?", "Close App Confirmation", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No) e.Cancel = true;
            }
        }

        public void MainWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data is DataObject && ((DataObject)e.Data).ContainsFileDropList())
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var file in files)
                {
                    TryImportFile(file);
                }
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
            dialog.Filter = "All supported files|*.brasd;*.rasd;*.rasp|Binary Revolution Animation Sound Data|*.brasd|Revolution Animation Sound Data|*.rasd";
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
        private void SaveTreeItem_Click(object sender, RoutedEventArgs e)
        {
            var item = currentTreeViewItem;
            TabItem? tabItem = null;
            Editor_RASD? editor = null;
            string name = "";
            bool newFile = false;
            foreach (var dat in datas)
            {

                if (dat.treeItem.Name == item.Name)
                {
                    tabItem = dat.tabItem;
                    editor = dat.editor;
                    name = dat.Name;
                    newFile = dat.newFile;
                    break;
                }
            }
            if (newFile)
            {
                Debug.WriteLine("Save as:");
                SaveAsTreeItem_Click(sender, e);
                Debug.WriteLine("Nothing");
                return;
            }

            if (tabItem != null)
            {
                tabItem.IsSelected = true;
                TrySaveFile(editor.Path, name);
            }
        }

        private void SaveAsTreeItem_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog();
            dialog.Filter = "All supported files|*.brasd;*.rasd|Binary Revolution Animation Sound Data|*.brasd|Revolution Animation Sound Data|*.rasd";
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                var item = currentTreeViewItem;
                TabItem? tabItem = null;
                Editor_RASD? editor = null;
                string name = "";
                foreach (var dat in datas)
                {
                    if (dat.treeItem.Name == item.Name)
                    {
                        tabItem = dat.tabItem;
                        editor = dat.editor;
                        name = dat.Name; break;

                    }
                }
                if (tabItem != null)
                {
                    tabItem.IsSelected = true;
                    TrySaveFile(dialog.FileName, name);
                }
            }
        }

        private void SaveAsFile_Click(object sender, RoutedEventArgs e)
        {
            if (loadedProject == null) return;
            var dialog = new SaveFileDialog();  
            dialog.Filter = "All supported files|*.brasd;*.rasd|Binary Revolution Animation Sound Data|*.brasd|Revolution Animation Sound Data|*.rasd";
            bool? result = dialog.ShowDialog();
            if(result == true)
            {
                TabItem currentTab = (TabItem)TabControl.SelectedItem;
                if (currentTab == null) return;
                char[] tabHeader = "T_".ToCharArray();
                string name = currentTab.Name.ToString().Replace("__", "_").TrimStart(tabHeader);

                TrySaveFile(dialog.FileName, name);
            }
        }

        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            if (loadedProject == null) return;
            TabItem currentTab = (TabItem)TabControl.SelectedItem;
            if (currentTab == null) return;
            char[] tabHeader = "T_".ToCharArray();
            string name = currentTab.Name.ToString().Replace("__", "_").TrimStart(tabHeader);
            Editor_RASD? editor = null;
            bool newFile = false;
            foreach (var dat in datas)
            {
                if (dat.tabItem == currentTab)
                {
                    editor = dat.editor;
                    name = dat.Name;
                    newFile = dat.newFile;
                    break;
                }
            }
            FileData data = datas.Cast<FileData>().FirstOrDefault(i => i.treeItem == currentTreeViewItem);
            if (data.editor != null)
            {
                var i = data.editor.DataGrid.CommitEdit();
            }

            Keyboard.ClearFocus();
            if (newFile)
            {
                Debug.WriteLine("Save as:");
                SaveAsTreeItem_Click(sender, e);
                Debug.WriteLine("Nothing");
                return;
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
            lib_RASP.RASP rasp = new();

            List<RASD?> rasds = new();

            string name = Path.GetFileName(path);
            string shortName = Path.GetFileNameWithoutExtension(path);
            string folderName = Path.GetFileName(Path.GetDirectoryName(path));
            if(datas.Cast<FileData>().Any(i => i.Name == shortName && i.ModelName == folderName))
            {
                MessageBox.Show("File is already opened", "File Open Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }


            switch (Path.GetExtension(path).Trim())
            {
                case ".brasd":
                    rasd = TryOpenBRASD(path);
                    if (loadedProject == null) rasp = ProjectFromRASD(shortName, rasd, Path.GetFileName(Path.GetDirectoryName(path)));
                    else rasp = AddSoundToProject(defaultAnim, shortName, rasd, (lib_RASP.RASP)loadedProject, Path.GetFileName(Path.GetDirectoryName(path)));
                    rasds.Add(rasd);
                    break;
                case ".rasd":
                    rasd = TryOpenRASD(path);
                    if (loadedProject == null) rasp = ProjectFromRASD(shortName, rasd, Path.GetFileName(Path.GetDirectoryName(path)));
                    else rasp = AddSoundToProject(defaultAnim, shortName, rasd, (lib_RASP.RASP)loadedProject, Path.GetFileName(Path.GetDirectoryName(path)));
                    rasds.Add(rasd);
                    break;
                case ".rasp":
                    lib_RASP.RASP? _rasp = TryReadRASP(path);
                    if (_rasp != null) rasp = (lib_RASP.RASP)_rasp;
                    break;

            }

            loadedProject = rasp;
            ProjectTree.Items.Clear();
            var itS = PopulateTree(shortName, (lib_RASP.RASP)loadedProject, path);
            //LoadBasicData(rasd);
            return rasd;
        }

        private void TrySaveFile(string path, string name)
        {
            Editor_RASD? editor = null;
            TreeViewItem? item = null;
            bool newFile = false;

            foreach (var dat in datas)
            {
                if (dat.Name == name)
                {
                    newFile = dat.newFile;
                    item = dat.treeItem;
                    editor = dat.editor; break;
                }
            }
            if (editor == null) return;

            editor.ConfirmAnyEdits();

            RASD data = editor.GetData();

            switch (Path.GetExtension(path))
            {
                case ".rasd":
                    SaveRASD(path, data);
                    break;
                case ".brasd":
                    SaveBRASD(path, data);
                    break;
                default:
                    path += ".brasd";
                    SaveBRASD(path, data);
                    break;
            }
            if (newFile)
            {
                CloseFile();   
                TryImportFile(path);

            }


        }


        private List<TreeViewItem> PopulateTree(string name, lib_RASP.RASP rasp, string path = "")
        {
            Debug.WriteLine("Populate Tree");
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
                itModel.Name = "M_" + model.Name;
                itModel.IsExpanded = true;

                foreach(var anim in model.Anims)
                {
                    TreeViewItem itAnim = new();
                    itAnim.Header = anim.Name + ".rchr";
                    itAnim.Tag = "Animation";
                    itAnim.Name = "A_" + anim.Name;
                    itAnim.IsExpanded = true;

                    foreach (var sound in anim.Sounds)
                    {
                        TreeViewItem itSound = new();
                        itSound.Header = sound.Name + ".rasd";
                        itSound.Tag = "Sound";
                        itSound.Name = "S_" + sound.Name;
                        itSound.MouseDoubleClick += TreeViewItem_DoubleClick;
                        itSound.ContextMenuOpening += ProjectTree_ContextMenuOpening;


                        FileData data = datas.Cast<FileData>().FirstOrDefault(i => i.Name == sound.Name
                                                        && i.ModelName == model.Name);

                        if(data.data != null)
                        {
                            Debug.WriteLine(model.Name);
                            Debug.WriteLine(data.ModelName);
                            itSound = data.treeItem;
                            ((TreeViewItem)itSound.Parent).Items.Clear();
                            Debug.WriteLine(itSound.Parent);
                        }
                        else
                        {
                            FileData dat = new();
                            dat.Name = name;
                            dat.treeItem = itSound;
                            dat.newFile = sound.Data.newFile;
                            dat.tabItem = CreateTab(name, sound.Data, itSound, out Editor_RASD editor, model.Name);
                            dat.data = sound.Data;
                            dat.editor= editor;
                            dat.FilePath = path;
                            dat.ModelName = model.Name;
                            Guid guid = Guid.NewGuid();
                            dat.UUID = guid.ToString();
                            datas.Add(dat);
                        }
                        defaultAnim = itAnim;
                        if(!itAnim.Items.Contains(itSound))
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

        private TabItem CreateTab(string name, RASD rasd, TreeViewItem treeItem, out Editor_RASD editor, string tabName)
        {
            TabItem tabItem = new TabItem();
            Debug.WriteLine(name);
            tabItem.Header = $@"{tabName} - {name.Replace("_", "__")}";
            tabItem.Name = "T_" + name;
            tabItem.IsSelected = true;

            editor= new Editor_RASD();
            editor.AllowDrop = true;
            
            editor.LoadData(rasd);
            Frame frame = new();
            frame.Content = editor;
            tabItem.Content = frame;
            tabItem.MouseEnter += TabItem_MouseEnter;

            TabControl.Items.Add(tabItem);
            currentTreeViewItem = treeItem;
            return tabItem;
        }

        private void LoadBasicData(RASD rasd)
        {
            Data = new();
            Data.Creator = rasd.Header.CreatorName;
            Data.Generator = rasd.Header.Generator;
            Data.DateModified = rasd.Header.DataSaved;
            //PropertyBox.SelectedObject = Data;
            //PropertyBox.NameColumnWidth = 60;
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

        private void TabItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!(e.Source is TabItem tabItem))
            {
                return;
            }

            if (Mouse.PrimaryDevice.LeftButton == MouseButtonState.Pressed)
            {
                DragDrop.DoDragDrop(tabItem, tabItem, DragDropEffects.All);
            }
        }




        private void TabItem_Drop(object sender, DragEventArgs e)
        {
            if (e.Source is TabItem tabItemTarget &&
                e.Data.GetData(typeof(TabItem)) is TabItem tabItemSource &&
                !tabItemTarget.Equals(tabItemSource) &&
                tabItemTarget.Parent is TabControl tabControl)
            {
                int targetIndex = tabControl.Items.IndexOf(tabItemTarget);

                tabControl.Items.Remove(tabItemSource);
                tabControl.Items.Insert(targetIndex, tabItemSource);
                tabItemSource.IsSelected = true;
            }
        }

        private volatile TreeViewItem currentTreeViewItem;

        private void ProjectTree_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (sender is TreeView)
            {
                e.Handled = true; return;
            }
            TreeViewItem item = sender as TreeViewItem;
            if (!item.Tag.ToString().Contains("Sound")) e.Handled = true;
            else
            {
                ProjectTree.ContextMenu.IsOpen = true;
                currentTreeViewItem = item;
                currentTreeViewItem.IsSelected= true;
            }
        }



        private void CloseFile(TreeViewItem item = null)
        {
            if(item == null) item = currentTreeViewItem;
            FileData data = datas.Cast<FileData>().FirstOrDefault(d => d.treeItem == item);

            if (data.ModelName != null)
            {
                if (TabControl.Items.Contains(data.tabItem)) TabControl.Items.Remove(data.tabItem);
                datas.Remove(data);
            }

            loadedProject = RemoveSoundFromProject(item, (RASP)loadedProject);
            ProjectTree.Items.Clear();
            var itS = PopulateTree("", (RASP)loadedProject, "");
        }

        private void CloseFile_Click(object sender, RoutedEventArgs e)
        {

            MessageBoxResult result = MessageBox.Show("Are you sure to close this file?", "Close file confirmation", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.No) return;

            var item = currentTreeViewItem;
            CloseFile(item);
        }

        private void NewFile_Click(object sender, RoutedEventArgs e)
        {
            RASD rasd = new();
            rasd.newFile = true;
            lib_RASP.RASP rasp;
            int newSounds = datas.Cast<FileData>().Count(i => i.newFile);
            if (loadedProject == null) rasp = ProjectFromRASD($"new_sound_{newSounds}", rasd, "new_model");
            else rasp = AddSoundToProject(defaultAnim, $"new_sound_{newSounds}", rasd, (lib_RASP.RASP)loadedProject, "new_model");
            loadedProject = rasp;
            ProjectTree.Items.Clear();
            var itS = PopulateTree($"new_sound_{newSounds}", (lib_RASP.RASP)loadedProject);
        }
    }


}
