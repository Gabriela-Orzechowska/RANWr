using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
            this.AllowDrop = true;
            this.Drop += MainWindow_Drop;
            this.KeyDown+= MainWindow_KeyDown;
            string[] args = Environment.GetCommandLineArgs();
            foreach(string arg in args.Skip(1))
            {
                TryImportFile(arg);      
            }         
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            
            if(Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S)
            {
                if (loadedProject == null) return;
                TabItem currentTab = (TabItem)TabControl.SelectedItem;
                if (currentTab == null) return;
                string name = currentTab.Name.ToString().Replace("__", "_");
                Editor_RASD? editor = null;
                foreach (var dat in datas)
                {
                    if (dat.Name == name)
                    {
                        editor = dat.editor; break;
                    }
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
            if (e.Data is System.Windows.DataObject && ((System.Windows.DataObject)e.Data).ContainsFileDropList())
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
            if (loadedProject == null) return;
            var dialog = new SaveFileDialog();  
            dialog.Filter = "All supported files|*.brasd;*.rasd|Binary Revolution Animation Sound Data|*.brasd|Revolution Animation Sound Data|*.rasd";
            bool? result = dialog.ShowDialog();
            if(result == true)
            {
                TabItem currentTab = (TabItem)TabControl.SelectedItem;
                if (currentTab == null) return;
                string name = currentTab.Name.ToString().Replace("__", "_");

                TrySaveFile(dialog.FileName, name);
            }
        }

        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            if (loadedProject == null) return;
            TabItem currentTab = (TabItem)TabControl.SelectedItem;
            if (currentTab == null) return;
            string name = currentTab.Name.ToString().Replace("__", "_");
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
                    else rasp = AddSoundToProject(defaultAnim, shortName, rasd, (RASP)loadedProject, Path.GetFileName(Path.GetDirectoryName(path)));
                    rasds.Add(rasd);
                    break;
                case ".rasd":
                    rasd = TryOpenRASD(path);
                    if (loadedProject == null) rasp = ProjectFromRASD(shortName, rasd, Path.GetFileName(Path.GetDirectoryName(path)));
                    else rasp = AddSoundToProject(defaultAnim, shortName, rasd, (RASP)loadedProject, Path.GetFileName(Path.GetDirectoryName(path)));
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
                itModel.Name = model.Name; ;
                itModel.IsExpanded = true;

                foreach(var anim in model.Anims)
                {
                    TreeViewItem itAnim = new();
                    itAnim.Header = anim.Name + ".rchr";
                    itAnim.Name = anim.Name;
                    itAnim.Tag = "Animation";
                    itAnim.IsExpanded = true;

                    foreach (var sound in anim.Sounds)
                    {
                        TreeViewItem itSound = new();
                        itSound.Header = sound.Name + ".rasd";
                        itSound.Tag = "Sound";
                        itSound.MouseDoubleClick += TreeViewItem_DoubleClick;
                        itSound.ContextMenuOpening += ProjectTree_ContextMenuOpening;
                       

                        if (datas.Cast<FileData>().Any(i => i.Name == sound.Name && i.ModelName == model.Name))
                        {
                            foreach(var dat in datas)
                            {
                                if (dat.Name == sound.Name && dat.ModelName == model.Name)
                                {
                                    Debug.WriteLine(model.Name);
                                    Debug.WriteLine(dat.ModelName);
                                    itSound = dat.treeItem;
                                    ((TreeViewItem)itSound.Parent).Items.Clear();
                                    Debug.WriteLine(itSound.Parent);
                                }
                            }    
                        }
                        else
                        {
                            FileData dat = new();
                            dat.Name = name;
                            dat.treeItem = itSound;
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
            tabItem.Name = name;
            tabItem.IsSelected = true;

            editor= new Editor_RASD();
            editor.AllowDrop = true;
            
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

        private volatile TreeViewItem currentTabItem;

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
                currentTabItem = item;
                currentTabItem.IsSelected= true;
            }
        }

        private void SaveTreeItem_Click(object sender, RoutedEventArgs e)
        {
            var item = currentTabItem;
            TabItem? tabItem = null;
            Editor_RASD? editor = null;
            string name = "";
            foreach(var dat in datas)
            {
                if (dat.treeItem.Name == item.Name)
                {
                    tabItem = dat.tabItem;
                    editor = dat.editor;
                    name = dat.Name; break;
                }
            }
            if(tabItem != null)
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
                var item = currentTabItem;
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

        private void CloseFile_Click(object sender, RoutedEventArgs e)
        {

            MessageBoxResult result = MessageBox.Show("Are you sure to close this file?", "Close file confirmation", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.No) return;

            var item = currentTabItem;
            FileData? data = null;
            foreach(var dat in datas)
            {
                if(dat.treeItem == item)
                {
                    data = dat; break;
                }
            }
            if(data != null)
            {
                FileData actualData = (FileData)data; //I hate that sometimes I can reference anything if data types is nullable, other times it works perfectly when unde is null statement
                if(TabControl.Items.Contains(actualData.tabItem)) TabControl.Items.Remove(actualData.tabItem);
                datas.Remove(actualData);
            }


            loadedProject = RemoveSoundFromProject(item, (RASP)loadedProject);
            ProjectTree.Items.Clear();
            var itS = PopulateTree("", (RASP)loadedProject, "");
        }
    }


}
