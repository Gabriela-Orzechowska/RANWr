using Microsoft.Win32;
using System.Windows;
using gablibela;
using gablibela.arc;
using gablibela.cmpr;
using System.Linq;
using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Net;
using System.Text.Json;

namespace ArchiveExplorer
{
    public partial class MainWindow : Window
    {
        private bool _saved = true;
        public static readonly string AppTag = "AE";
        public static readonly string Version = "v0.26";

        public MainWindow()
        {
            InitializeComponent();
            this.Title = $"RANWr ArchiveExplorer {Version}";
            this.AllowDrop= true;
            this.Drop += MainWindow_Drop;

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(ExceptionHandler);

            string[] args = Environment.GetCommandLineArgs();
            if(args.Length > 1)
            {
                TryOpenFile(args[1]);
            }
            CheckForUpdate();

#if DEBUG
            AllocConsole();
            Console.WriteLine($"RANWr ArchiveExplorer {AppTag}.{Version} Debug Build - {Properties.Resources.BuildDate}");
#endif
        }

        [DllImport("Kernel32")]
        public static extern void AllocConsole();

        [DllImport("Kernel32")]
        public static extern void FreeConsole();

        private void CheckForUpdate()
        {
            string html = HTMLGetRequest("https://api.github.com/repos/Gabriela-Orzechowska/RANWr/releases");
            github_releases[] releases = null;
            try
            {
                releases = JsonSerializer.Deserialize<github_releases[]>(html);
            }
            catch
            {
                return;
            }
            if (releases == null) return;
            int baseVersion = int.Parse(Version.Replace(".", "").Substring(1));
            int version = baseVersion;
            string versionUri = string.Empty;
            string downloadUri = string.Empty;
            string _body = "";

            foreach (var release in releases)
            {
                if (release.tag_name.StartsWith(AppTag + "."))
                {
                    var newVersion = int.Parse(release.tag_name.Replace(".", "").Substring(AppTag.Length + 2));
                    if (newVersion > version)
                    {
                        version = newVersion;
                        versionUri = release.html_url;
                        _body = release.body;
                        if (release.assets.Length > 0)
                            downloadUri = release.assets[0].browser_download_url;
                    }
                }
            }
            if (version != baseVersion) 
            {
                MessageBoxResult result = MessageBox.Show($"There's a new version available! Would you like to go to the download page?\n\nChangelog:\n{_body}", "New Version!", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    OpenLinkInBrowser(downloadUri);
                }
            }
        }

        private void UpdateTitleSaveIndication(bool enabled)
        {
            if (this._saved == !enabled) return;

            this._saved = !enabled;
            if (!this._saved)
                this.Title = this.Title + "*";
            else
                this.Title = this.Title.Substring(0, this.Title.Length - 1);
        }

        public struct github_releases
        {
            public string html_url { get; set; }
            public string tag_name { get; set; }
            public string name { get; set; }
            public string body { get; set; }
            public github_assets[] assets { get; set; }
        }
        public struct github_assets
        {
            public string url { get; set; }
            public string name { get; set; }
            public string browser_download_url { get; set; }
        }

        public string HTMLGetRequest(string uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = "GET";
            request.Headers["User-Agent"] = "RANWr Auto-Updater";
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
            catch
            {
                return "";
            }
        }

        static void ExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            var exception = args.ExceptionObject is Exception ? ((Exception)args.ExceptionObject).Message + ((Exception)args.ExceptionObject).StackTrace : string.Empty;
            MessageBox.Show("Whoops! Please contact the developers with the following information:\r\n\r\n" + exception,
                            "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Stop);
        }

        public Dictionary<TreeViewItem, ARC.Node> nodeConnects = new();
        public ARC currentFile;
        public ARC.Node currentNode;
        public string currentFilePath;

        public Dictionary<string, string> fileTypes = new Dictionary<string, string>()
        {
            //Effects
            { ".breft", "Binary Revolution Effect Texture Project" },
            { ".breff", "Binary Revolution Effect Project" },

            //Menu Files
            { ".bmg", "Binary Message Group" },
            { ".brctr", "Binary Revolution Control" },
            { ".brfnt", "Binary Revolution Font" },
            { ".brlan", "Binary Revolution Layout Animation" },
            { ".brlyt", "Binary Revolution Layout" },

            { ".thp", "THP Movie File" },
            { ".tpl", "Texture Palette Library" },

            //Resources
            { ".brres", "Binary Revolution Resource" },
            //All BRRES Formats ffs
            { ".brmdl", "Binary Revolution Model Resource" },
            { ".brtex", "Binary Revolution Texture Resource" },
            { ".brcha", "Binary Revolution Character Animation Resource" },
            { ".brcla", "Binary Revolution Color Animation Resource" },
            { ".brplt", "Binary Revolution Palette Resource" },
            { ".brsca", "Binary Revolution Scene Animation Resource" },
            { ".brsha", "Binary Revolution Shape Animation Resource" },
            { ".brtpa", "Binary Revolution Texture Pattern Animation Resource" },
            { ".brtsa", "Binary Revolution Texture SRT Animation Resource" },
            { ".brvia", "Binary Revolution Visibility Animation Resource" },

            { ".kcl", "KCL Collision File" },

            //Mario Kart Wii Files
            { ".kmp", "Mario Kart Wii Map Parameters" },
            { ".krm", "Mario Kart Wii Rumble" },
            { ".bsp", "Binary Settings and Physics" },
            { ".btiEnv", "Binary Texture Information" },
            { ".bti", "Binary Texture Information" },
            { ".btiMat", "Binary Texture Information" },
            { ".rkc", "Mario Kart Wii Competition File" },
            { ".rkg", "Mario Kart Wii Ghost File" },
            { ".ikp", "Inverse Kinematics Parameters" },
            { ".bcp", "Binary Camera Parameters" },
            { ".bmm", "Binary Mii Material" },

            
            //Sound
            { ".brsar", "Binary Revolution Sound Archive" },
            { ".brwar", "Binary Revolution Wave Archive" },
            { ".brstm", "Binary Revolution Stream Sound" },
            { ".brseq", "Binary Revolution Sequence" },
            { ".brwsd", "Binary Revolution Wave Sound Data" },
            { ".brasd", "Binary Revolution Animation Sound Data" },

            //Mario Kart Wii PostEffects
            { ".bdof", "Binary Depth of Field" },
            { ".bblm", "Binary Bloom" },
            { ".blmap", "Binary Light Map" },
            { ".bfg", "Binary Fog" },
            { ".blight", "Binary Lighting" },
        };
        
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if(Keyboard.Modifiers == ModifierKeys.Control)
            {
                if(Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    switch(e.Key)
                    {
                        case Key.S:
                            saveFile(); break;
                    }
                }
                else
                {
                    switch (e.Key)
                    {
                        case Key.S:
                            saveFile(2); break;
                        case Key.C:
                            CopyFiles(); break;
                        case Key.V:
                            PasteFiles(); break;
                        case Key.X:
                            CutFiles(); break;
                        case Key.D:
                            DuplicateFile(); break;
                        case Key.N:
                            CreateNewFile(); break;
                        case Key.O:
                            OpenFileDialog(); break;
                        case Key.I:
                            ImportFileDialog(); break;
                        case Key.R:
                            ReplaceFileDialog(); break;
                    }
                }
            }
            else if(e.Key == Key.Delete)
            {
                if (currentFile == null) return;
                if (FileView.SelectedItems.Count > 0)
                {
                    List<FileListItem> items = new();
                    foreach(var selItem in FileView.SelectedItems)
                    {
                        items.Add(selItem as FileListItem);
                    }
                    DeleteFile(items.ToArray());
                    return;
                }
            }
            else if(e.Key == Key.Enter)
            {
                if (currentFile == null) return;
                FileListItem item = FileView.SelectedItem as FileListItem;
                if(Keyboard.Modifiers == ModifierKeys.Shift)
                    OpenCurrentSelectedWith(item);
                else 
                    OpenCurrentSelected(item);
            }
            else if(e.Key == Key.F2)
            {
                lastItem = FileView.SelectedItem as FileListItem;
                RenameFile();
            }
        }


        private void CopyFiles()
        {
            if (FileView.SelectedItems.Count == 0) return;
            StringCollection paths = new StringCollection();
            foreach (FileListItem item in FileView.SelectedItems)
            {
                ARC.Node node = item.Node;
                string nodePath = currentFile.GetNodePath(node);
                string exportPath = currentFile.PathCombine(currentFile.TemporaryPath, nodePath);
                paths.Add(exportPath);
            }

            Clipboard.SetFileDropList(paths);
        }

        private void CutFiles()
        {
            /*
            if (FileView.SelectedItems.Count == 0) return;
            StringCollection paths = new StringCollection();
            foreach (FileListItem item in FileView.SelectedItems)
            {
                DARCH.Node node = item.Node;
                string nodePath = currentFile.GetNodePath(node);
                string exportPath = currentFile.PathCombine(currentFile.TemporaryPath, nodePath);
                paths.Add(exportPath);
            }
            DataObject data = new DataObject();

            byte[] moveEffect = new byte[] { 2, 0, 0, 0 };
            MemoryStream dropEffect = new MemoryStream();
            dropEffect.Write(moveEffect, 0, moveEffect.Length);


            data.SetFileDropList(paths);
            data.SetData("Preferred DropEffect", dropEffect);
            Clipboard.SetDataObject(data);
            */
        }


        private void PasteFiles()
        {
            if (Clipboard.ContainsFileDropList())
            {
                var files = Clipboard.GetFileDropList();
                foreach (var file in files)
                {
                    TryImportFile(file);
                }
            }
        }

        private void DuplicateFile()
        {
            FileListItem item = FileView.SelectedItem as FileListItem;
            if (item == null) return;
            ARC.Node node = item.Node;
            string nodePath = currentFile.GetNodePath(node);
            string exportPath = currentFile.PathCombine(currentFile.TemporaryPath, nodePath);
            TryImportFile(exportPath, true);
        }

        private void FileView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (currentFile == null)
            {
                e.Handled = true;
                FileView.ContextMenu.IsOpen = false;
            }
        }

        private void NewFile_Click(object sender, RoutedEventArgs e) => CreateNewFile();

        private void CreateNewFile()
        {
            if (currentFile != null)
            {
                MessageBoxResult result = MessageBox.Show("Do you want to save before quitting?", "Warning", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Yes)
                    saveFile();
                else if (result == MessageBoxResult.Cancel) return;
                currentFile.FreeArchive();
            }

            ARC arc = new();
            arc.name = "untitled.arc";
            currentFile = arc;
            UpdateTreeView();
            currentNode = currentFile.structure;
            UpdateListView(currentFile.structure);
            UpdatePath();
            UpdateTitleSaveIndication(true);
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e) => OpenFileDialog();

        private void OpenFileDialog()
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Nintendo ARC Archive|*.arc;*.szs;*.u8";
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                TryOpenFile(dialog.FileName);
            }
        }

        private void saveFile(byte level = 10)
        {
            if (currentFile == null) return;
            currentFile.UpdateAllNodeData();
            byte[] saveData = currentFile.EncodeARC();
            if(Path.GetExtension(currentFilePath) == ".szs") saveData = YAZ0.Compress(saveData, level);
            File.WriteAllBytes(currentFilePath, saveData);
            UpdateTitleSaveIndication(false);
        }

        private void QuickSave_Click(object sender, RoutedEventArgs e)
        {
            if (currentFile == null) return;
            if (currentFilePath != null) saveFile(2);
            else
            {
                QuickSaveAs_Click(sender, e);
            }
        }

        private void QuickSaveAs_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog();
            dialog.Filter = "All supported files|*.szs;*.arc;*.u8|Compressed Archive|*.szs|Nintendo ARC Archive|*.arc;*.u8";
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                currentFilePath = dialog.FileName;
                saveFile(2);
                currentFile.name = Path.GetFileName(currentFilePath);
                UpdateTreeView();
            }
        }

        private void FullSave_Click(object sender, RoutedEventArgs e)
        {
            if (currentFile == null) return;
            if (currentFilePath != null) saveFile();
            else
            {
                FullSaveAs_Click(sender, e);
            }
        }

        private void FullSaveAs_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog();
            dialog.Filter = "All supported files|*.szs;*.arc;*.u8|Compressed Archive|*.szs|Nintendo ARC Archive|*.arc;*.u8";
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                currentFilePath = dialog.FileName;
                saveFile();
                currentFile.name = Path.GetFileName(currentFilePath);
                UpdateTreeView();
            }
        }

        private void ImportFile_Click(object sender, RoutedEventArgs e) => ImportFileDialog();

        private void ImportFileDialog() 
        {
            if(currentFile == null) return;
            var dialog = new OpenFileDialog();
            dialog.Multiselect = true;
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                foreach (var file in dialog.FileNames)
                {
                    var extension = Path.GetExtension(file);
                    if (extension == "*.szs" || extension == "*.arc" || extension == "*.u8") continue;
                    TryImportFile(file,false,false);
                }
            }
        }

        private void ReplaceFile_Click(object sender, RoutedEventArgs e)
        {
            ReplaceFileDialog();
        }

        private void ReplaceFileDialog()
        {
            if (currentFile == null) return;
            var dialog = new OpenFileDialog();
            dialog.Multiselect = false;
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                var file = dialog.FileName;
                var extension = Path.GetExtension(file);
                if (extension == "*.szs" || extension == "*.arc" || extension == "*.u8") return;
                var currentItem = FileView.SelectedItem as FileListItem;
                if (currentItem == null) return;
                TryImportFile(file, false, false, currentItem.Text, true);
                UpdateTitleSaveIndication(true);
            }
        }

        public void MainWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data is DataObject && ((DataObject)e.Data).ContainsFileDropList())
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var file in files)
                {
                    if (currentFile != null)
                    {
                        if (file.Contains(currentFile.TemporaryPath)) continue;
                    }
                    TryImportFile(file);
                    UpdateTitleSaveIndication(true);
                }
            }
        }

        public void FileViewElement_Drop(object sender, DragEventArgs e)
        {
            FileListItem item = ((StackPanel)sender).DataContext as FileListItem;
            if(item == null) return;
            
            var node = item.Node;
            if (node == null) return;
            if (node.Type == ARC.Node.NodeType.File) return;

            if (e.Data is DataObject && ((DataObject)e.Data).ContainsFileDropList())
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var file in files)
                {
                    TryImportFile(file, node);
                    FileAttributes attr = File.GetAttributes(file);
                    string relative = Path.GetRelativePath(currentFile.TemporaryPath, file);
                    currentFile.RemoveNode(currentFile.GetNodeByPath(relative));
                    if (attr.HasFlag(FileAttributes.Directory)) Directory.Delete(file);
                    else File.Delete(file);
                }

            }
            UpdateTreeView();
            UpdateListView(currentNode);
            UpdateTitleSaveIndication(true);
        }

        private void TreeViewItem_Drop(object sender, DragEventArgs e) 
        {
            TreeViewItem item = sender as TreeViewItem;
            if (item == null) return;
            if (nodeConnects.TryGetValue(item, out ARC.Node node))
            {
                if (node == null) return;
                if (e.Data is DataObject && ((DataObject)e.Data).ContainsFileDropList())
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    foreach (var file in files)
                    {
                        TryImportFile(file, node);
                        FileAttributes attr = File.GetAttributes(file);
                        string relative = Path.GetRelativePath(currentFile.TemporaryPath, file);
                        currentFile.RemoveNode(currentFile.GetNodeByPath(relative));
                        if (Path.Exists(file))
                        {
                            if (attr.HasFlag(FileAttributes.Directory)) Directory.Delete(file);
                            else File.Delete(file);
                        }
                    }
                }
            }
            UpdateTreeView();
            UpdateListView(currentNode);
            UpdateTitleSaveIndication(true);
        }


        private void FileTextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled= true;
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            if(currentNode.Parent != null)
            {
                currentNode = currentNode.Parent;
            }
            UpdatePath();
            UpdateListView(currentNode);
        }

        private void FolderView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem item = (sender as TreeView).SelectedItem as TreeViewItem;
            if (item == null) return;
            if (nodeConnects.TryGetValue(item, out ARC.Node node))
            {
                if (node == null) return;
                currentNode = node;
                UpdatePath();
                UpdateListView(currentNode);
            }
        }

        private void FileView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            FileListItem item = ((FrameworkElement)e.OriginalSource).DataContext as FileListItem;
            if (e.ChangedButton != MouseButton.Left) return;

            OpenCurrentSelected(item);
        }


        private void FileViewItem_Move(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (e.Source != null)
                {
                    List<string> myList = new List<string>();
                    foreach (FileListItem item in FileView.SelectedItems)
                    {
                        var node = item.Node;
                        if(node == null) continue;
                        string nodePath = currentFile.GetNodePath(node);
                        string exportPath = currentFile.PathCombine(currentFile.TemporaryPath, nodePath);
                        myList.Add(exportPath);
                    }

                    DataObject dataObject = new DataObject(DataFormats.FileDrop,myList.ToArray());
                    DragDrop.DoDragDrop(FileView, dataObject, DragDropEffects.Copy);
                }
            }
        }

        private void OpenCurrentSelected(FileListItem item)
        {
            if (item == null) return;

            ARC.Node node = item.Node;
            if (node.Type == ARC.Node.NodeType.File)
            {
                currentFile.OpenNode(node);
            }
            else
            {
                currentNode = node;
                UpdatePath();
                UpdateListView(currentNode);
            }
        }

        private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            FileListItem item = ((MenuItem)sender).DataContext as FileListItem;
            if (item == null) return;
            ARC.Node node = item.Node;
            if (node.Type == ARC.Node.NodeType.File)
            {
                currentFile.OpenNode(node);
            }
            else
            {
                currentNode = node;
                UpdatePath();
                UpdateListView(currentNode);
            }
        }

        private void OpenWithMenuItem_Click(object sender, RoutedEventArgs e)
        {
            FileListItem item = ((MenuItem)sender).DataContext as FileListItem;
            OpenCurrentSelectedWith(item);
        }

        private void OpenCurrentSelectedWith(FileListItem item)
        {
            if (item == null) return;
            ARC.Node node = item.Node;
            if (node.Type == ARC.Node.NodeType.File)
            {
                currentFile.OpenWithNode(node);
            }
        }

        StackPanel lastListViewItemPanel = null;
        FileListItem lastItem = null;
        string originalName = null;

        private void FileView_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            FileListItem item = ((ListView)sender).SelectedItem as FileListItem;
            if(item == null) return;

        }

        private void RenameItem_Click(object sender, RoutedEventArgs e)
        {
            lastItem = ((MenuItem)sender).DataContext as FileListItem;
            RenameFile();
        }


        private void RenameFile()
        {
            ListViewItem item = FileView.ItemContainerGenerator.ContainerFromItem(FileView.SelectedItem) as ListViewItem;
            ContentPresenter presenter = FindVisualChild<ContentPresenter>(item);
            DataTemplate template = presenter.ContentTemplate;
            TextBox textBox = (TextBox)template.FindName("FileTextBox", presenter);

            originalName = textBox.Text;
            textBox.IsReadOnly = false;
            textBox.Focusable = true;
            textBox.Focus();
            textBox.CaretBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));
            textBox.Cursor = Cursors.IBeam;
            textBox.BorderThickness = new Thickness(1);
            textBox.SelectAll();

            UpdateTitleSaveIndication(true);
        }

        private childItem FindVisualChild<childItem>(DependencyObject obj)
    where childItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is childItem)
                {
                    return (childItem)child;
                }
                else
                {
                    childItem childOfChild = FindVisualChild<childItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        private void CreateFolderMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string newFolderName = currentFile.GetFirstNameAvailable("New Folder", currentNode);
            ARC.Node node = currentFile.AddNode(newFolderName, ARC.Node.NodeType.Directory, currentNode, true);
            currentFile.ExportNode(node);
            UpdateListView(currentNode);
            UpdateTreeView();
            UpdateTitleSaveIndication(true);
        }

        private void CopyItem_Click(object sender, RoutedEventArgs e)
        {
            CopyFiles();
        }

        private void PasteItem_Click(object sender, RoutedEventArgs e)
        {
            PasteFiles();
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (FileView.SelectedItems.Count > 0)
            {
                List<FileListItem> items = new();
                foreach (var selItem in FileView.SelectedItems)
                {
                    items.Add(selItem as FileListItem);
                }
                DeleteFile(items.ToArray());
                return;
            }
        }

        private void DeleteFile(FileListItem[] items = null)
        {
            if (items == null) return;
            string messageBoxText = "Are you sure to delete these files?";
            if (items.Length == 1) messageBoxText = "Are you sure to delete this file?";
            MessageBoxResult result = MessageBox.Show(messageBoxText, "Delete file?", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                foreach(var item in items)
                    currentFile.RemoveNode(item.Node);
                UpdateListView(currentNode);
                UpdateTreeView();
                UpdateTitleSaveIndication(true);
            }
        }

        private void DeleteFile(FileListItem item = null)
        {
            if (item == null) return;
            MessageBoxResult result = MessageBox.Show("Are you sure to delete this file?", "Delete file?", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                currentFile.RemoveNode(item.Node);
                UpdateListView(currentNode);
                UpdateTitleSaveIndication(true);
            }
            UpdateTreeView();
        }

        private void FileTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox box = (TextBox)sender;
            if (e.Key == Key.Enter)
            {
                if (!box.IsReadOnly)
                {
                    box.IsReadOnly = true;
                    box.BorderThickness = new Thickness(0);
                    if (originalName == box.Text) return;
                    RenameFile(lastItem, box.Text, box);
                    box.CaretBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0,0,0,0));
                    box.Cursor = Cursors.Arrow;
                    box.Focusable = false;
                }
            }
            else if (e.Key == Key.Escape)
            {
                if (!box.IsReadOnly)
                {
                    box.IsReadOnly = true;
                    box.BorderThickness = new Thickness(0);
                    box.Text = originalName;
                    box.CaretBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0, 0, 0, 0));
                    box.Cursor = Cursors.Arrow;
                    box.Focusable = false;
                }
            }
            UpdateTreeView();
        }

        private void FileTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox box = (TextBox)sender;

            if (!box.IsReadOnly)
            {
                box.IsReadOnly = true;
                box.BorderThickness = new Thickness(0);
                box.CaretBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0, 0, 0, 0));
                box.Cursor = Cursors.Arrow;
                box.Focusable = false;
                if (originalName == box.Text) return;
                RenameFile(lastItem, box.Text, box);
                
            }
        }

        private void RenameFile(FileListItem item, string newName, TextBox box)
        {
            string newerName = currentFile.GetFirstNameAvailable(newName, currentNode);
            currentFile.RenameNode(item.Node, newName);
            if(newName != newerName)
            {
                box.Text = newerName;
            }
            currentFile.RecalculateStructureIndexes();
            UpdateListView(currentNode);
            UpdateTreeView();
            UpdateTitleSaveIndication(true);
        }

        private void TryImportFile(string filepath, bool forceDuplicate = false, bool importARC = true, string originalPath = null, bool forceOverride = false)
        {
            TryImportFile(filepath, currentNode, forceDuplicate, importARC, originalPath, forceOverride);
        }

        private void TryImportFile(string filepath, ARC.Node importNode, bool forceDuplicate = false, bool importARC = true, string originalPath = null, bool forceOverride = false)
        {
            bool isDirectory = File.GetAttributes(filepath).HasFlag(FileAttributes.Directory);
            byte[] data;
            string name = Path.GetFileName(filepath);
            if(originalPath != null) name = Path.GetFileName(originalPath);
            if (!isDirectory)
            {
                data = File.ReadAllBytes(filepath);
                if (data.Length > 4)
                {
                    var signature = BitConverter.ToUInt32(data.Take(4).Reverse().ToArray());
                    if (signature == ARC.Signature || signature == YAZ0.SignatureHex)
                    {
                        if (importARC) TryOpenFile(filepath);
                        return;
                    }
                }
                if (currentFile == null) return;
                var curFolderPath = Path.Combine(currentFile.TemporaryPath, currentFile.GetNodePath(importNode));
                var folderPath = Path.Combine(curFolderPath, name);
                

                if(Path.Exists(folderPath))
                {
                    if (forceDuplicate)
                    {
                        string newName = currentFile.GetFirstNameAvailable(name, importNode);
                        folderPath = Path.Combine(curFolderPath, newName);
                        File.Copy(filepath, folderPath, true);
                    }
                    else if (forceOverride)
                    {
                        if (folderPath == filepath) return;
                        File.Copy(filepath, folderPath, true);
                    }
                    else
                    {
                        if (folderPath == filepath) return;
                        MessageBoxResult result = MessageBox.Show($"File {Path.GetFileName(folderPath)} already exists, replace it?", "Warning", MessageBoxButton.YesNoCancel);
                        if (result == MessageBoxResult.Yes)
                        {
                            if (folderPath == filepath) return;
                            File.Copy(filepath, folderPath, true);
                        }
                        else if (result == MessageBoxResult.No)
                        {
                            string newName = currentFile.GetFirstNameAvailable(name, importNode);
                            folderPath = Path.Combine(curFolderPath, newName);
                            File.Copy(filepath, folderPath, true);
                        }
                    }
                }
                else
                    File.Copy(filepath, folderPath, true);
            }
            else
            {
                if (currentFile == null) return;
                var curFolderPath = Path.Combine(currentFile.TemporaryPath, currentFile.GetNodePath(importNode));
                var folderPath = Path.Combine(curFolderPath, name);


                bool somethingExists = false;
                if (Path.Exists(folderPath))
                {
                    List<string> allfiles = Directory.GetFileSystemEntries(folderPath, "*", SearchOption.AllDirectories).ToList();

                    foreach (string file in allfiles)
                    {
                        if (Path.Exists(file)) somethingExists = true; break;
                    }
                }
                Directory.CreateDirectory(folderPath);
                if (somethingExists)
                {
                    MessageBoxResult result = MessageBox.Show($"Replace files?", "Warning", MessageBoxButton.YesNo);
                    CopyFilesRecursively(filepath, folderPath, result == MessageBoxResult.Yes);
                }
                else
                {
                    CopyFilesRecursively(filepath, folderPath, true);
                }
                
            }
            currentFile.UpdateAllNodeData();
            UpdateListView(currentNode);
            UpdateTreeView();
            UpdateTitleSaveIndication(true);
        }
        
        private void TryOpenFile(string filepath)
        {
            if(currentFile != null)
            {
                MessageBoxResult result = MessageBox.Show("Do you want to save before quitting?", "Warning", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Yes)
                {
                    if (currentFilePath != null) saveFile();
                    else
                    {
                        var dialog = new SaveFileDialog();
                        dialog.Filter = "All supported files|*.szs;*.arc;*.u8|Compressed Archive|*.szs|Nintendo ARC Archive|*.arc;*.u8";
                        bool? newResult = dialog.ShowDialog();
                        if (newResult == true)
                        {
                            currentFilePath = dialog.FileName;
                            saveFile();
                        }
                    }
                }
                else if (result == MessageBoxResult.Cancel) return;
                currentFile.FreeArchive();
            }

            ARC arc = GetArchive(filepath, Path.GetFileName(filepath));
            if (arc == null) return;
            currentFilePath = filepath;
            currentFile = arc;
            arc.ExportAllNodes();
            UpdateTreeView();
            UpdateListView(currentFile.structure);
            currentNode = currentFile.structure;
            UpdatePath();
            UpdateTitleSaveIndication(false);
        }



        private void UpdatePath()
        {
            string path = $"{currentFile.name}\\{currentFile.GetNodePath(currentNode)}";
            if (currentNode.Name != "") path += "\\";
            PathBar.Text = path;
        }

        private void UpdateTreeView()
        {
            if (currentFile == null) return;
            nodeConnects.Clear();
            FolderView.Items.Clear();
            TreeViewItem archiveNode = GetNodeItem(currentFile.structure);
            archiveNode.Tag = currentFile.name;

            StackPanel panel = new();
            panel.Orientation = Orientation.Horizontal;
            panel.Height = 18;

            TextBlock block = new();
            System.Windows.Controls.Image image = new();
            var uri = new Uri("/Icons/FolderOpened.png", UriKind.Relative);
            image.Source = new BitmapImage(uri);
            image.Height = 18;

            block.Text = currentFile.name;
            block.Height= 18;
            block.Padding = new Thickness(5,0,0,0);

            panel.Children.Add(image);
            panel.Children.Add(block);

            archiveNode.Header = panel;
            archiveNode.IsExpanded = true;
            archiveNode.AllowDrop= true;
            archiveNode.Drop += TreeViewItem_Drop;
            FolderView.Items.Add(archiveNode);
            nodeConnects.Add(archiveNode, currentFile.structure);
        }

        private static void CopyFilesRecursively(string sourcePath, string targetPath, bool replace = true)
        {
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), replace);
            }
        }

        private void UpdateListView(ARC.Node node)
        {
            List<ARC.Node> nodes = node.Children.OrderByDescending(x => x.Type).ToList();
            FileView.Items.Clear();
            foreach(var child in nodes)
            {
                FileListItem item = new FileListItem();
                if (child.Type == ARC.Node.NodeType.Directory)
                {
                    var uri = new Uri("/Icons/FolderOpened.png", UriKind.Relative);
                    item.Icon = new BitmapImage(uri);
                    item.Size = "";
                }
                else
                {
                    item.Icon = IconManager.FindIconForFilename(child.Name, true);
                    item.Size = FormatFileSize(child.DataSize);
                }
                item.Node = child;
                item.Text = child.Name;
                item.Type = "";
                var digits = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
                var extension = Path.GetExtension(child.Name);

                if (fileTypes.TryGetValue(extension.TrimEnd(digits), out var newName))
                {
                    item.Type = newName;
                }
                item.isDir = child.Type == ARC.Node.NodeType.Directory;
                item.isFile = !item.isDir;
                
                FileView.Items.Add(item);
            }
        }

        public class FileListItem
        {
            public ImageSource Icon { get; set; }
            public string Text { get; set; }
            public string Size { get; set; }
            public ARC.Node Node { get; set; }
            public string Type { get; set; }
            public bool isDir { get; set; }
            public bool isFile { get; set; }
            public TextBox FileTextBox;
        }

        public static string FormatFileSize(long bytes)
        {
            var unit = 1024;
            if (bytes < unit) { return $"{bytes} B"; }

            var exp = (int)(Math.Log(bytes) / Math.Log(unit));
            return $"{bytes / Math.Pow(unit, exp):F2} {("KMGTPE")[exp - 1]}B";
        }

        private TreeViewItem GetNodeItem(ARC.Node node)
        {
            TreeViewItem curNodeItem = new();
            curNodeItem.IsExpanded = true;
            if(node.Children.Count > 0) 
            { 
                foreach(var child in node.Children)
                {
                    if (child.Type == ARC.Node.NodeType.File) continue;
                    TreeViewItem childNodeItem = GetNodeItem(child);
                    childNodeItem.IsExpanded = true;

                    StackPanel panel = new();
                    panel.Orientation = Orientation.Horizontal;
                    panel.Height = 18;

                    TextBlock block = new();
                    System.Windows.Controls.Image image = new();
                    var uri = new Uri("/Icons/FolderOpened.png", UriKind.Relative);
                    image.Source = new BitmapImage(uri);
                    image.Height = 18;

                    block.Text = child.Name;
                    block.Height = 18;
                    block.Padding = new Thickness(5, 0, 0, 0);

                    panel.Children.Add(image);
                    panel.Children.Add(block);

                    childNodeItem.Header = panel;

                    childNodeItem.DataContext= child;
                    childNodeItem.AllowDrop = true;
                    childNodeItem.Drop += TreeViewItem_Drop;
                    curNodeItem.Items.Add(childNodeItem);
                    nodeConnects.Add(childNodeItem, child);
                }
            }
            return curNodeItem;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (currentFile != null && this._saved == false)
            {
                MessageBoxResult result = MessageBox.Show("Do you want to save before quitting?", "Warning", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Yes)
                {
                    if(currentFilePath != null) saveFile();
                    else
                    {
                        var dialog = new SaveFileDialog();
                        dialog.Filter = "All supported files|*.szs;*.arc;*.u8|Compressed Archive|*.szs|Nintendo ARC Archive|*.arc;*.u8";
                        bool? newResult = dialog.ShowDialog();
                        if (newResult == true)
                        {
                            currentFilePath = dialog.FileName;
                            saveFile();
                        }
                    }
                }
                    
                else if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            if(currentFile != null) currentFile.FreeArchive();
            base.OnClosed(e);
        }

        public static ARC GetArchive(string path, string filename) => GetArchive(File.ReadAllBytes(path), filename);

        public static ARC GetArchive(byte[] data, string filename)
        {
            var signature = BitConverter.ToUInt32(data.Take(4).Reverse().ToArray());
            if (signature == YAZ0.SignatureHex)
            {
                data = YAZ0.Decode(data);
                signature = BitConverter.ToUInt32(data.Take(4).Reverse().ToArray());
            }
            if (signature != ARC.Signature) return null;
            return new(data, filename);
        }

        private void AboutItem_Click(object sender, RoutedEventArgs e) => OpenLinkInBrowser("https://www.youtube.com/watch?v=__nioTv19bQ");
        private void GithubAboutItem_Click(object sender, RoutedEventArgs e) => OpenLinkInBrowser("https://github.com/Gabriela-Orzechowska/RANWr");

        private void OpenLinkInBrowser(string uri)
        {
            var psi = new System.Diagnostics.ProcessStartInfo();
            psi.UseShellExecute = true;
            psi.FileName = uri;
            System.Diagnostics.Process.Start(psi);
        }
    }

    public static class IconManager
    {
        private static readonly Dictionary<string, ImageSource> _smallIconCache = new Dictionary<string, ImageSource>();
        private static readonly Dictionary<string, ImageSource> _largeIconCache = new Dictionary<string, ImageSource>();

        public static ImageSource FindIconForFilename(string fileName, bool large)
        {
            var extension = Path.GetExtension(fileName);
            if (extension == null)
                return null;
            var cache = large ? _largeIconCache : _smallIconCache;
            ImageSource icon;
            if (cache.TryGetValue(extension, out icon))
                return icon;
            icon = IconReader.GetFileIcon(fileName, large ? IconReader.IconSize.Large : IconReader.IconSize.Small, false).ToImageSource();
            cache.Add(extension, icon);
            return icon;
        }

        static ImageSource ToImageSource(this Icon icon)
        {
            var imageSource = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            return imageSource;
        }

        static class IconReader
        {
            public enum IconSize
            {
                Large = 0,
                Small = 1
            }
            public static Icon GetFileIcon(string name, IconSize size, bool linkOverlay)
            {
                var shfi = new Shell32.Shfileinfo();
                var flags = Shell32.ShgfiIcon | Shell32.ShgfiUsefileattributes;
                if (linkOverlay) flags += Shell32.ShgfiLinkoverlay;
                if (IconSize.Small == size)
                    flags += Shell32.ShgfiSmallicon;
                else
                    flags += Shell32.ShgfiLargeicon;
                Shell32.SHGetFileInfo(name,
                    Shell32.FileAttributeNormal,
                    ref shfi,
                    (uint)Marshal.SizeOf(shfi),
                    flags);
                var icon = (Icon)Icon.FromHandle(shfi.hIcon).Clone();
                User32.DestroyIcon(shfi.hIcon);  
                return icon;
            }
        }
        static class Shell32
        {
            private const int MaxPath = 256;
            [StructLayout(LayoutKind.Sequential)]
            public struct Shfileinfo
            {
                private const int Namesize = 80;
                public readonly IntPtr hIcon;
                private readonly int iIcon;
                private readonly uint dwAttributes;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxPath)]
                private readonly string szDisplayName;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Namesize)]
                private readonly string szTypeName;
            };
            public const uint ShgfiIcon = 0x000000100;     // get icon
            public const uint ShgfiLinkoverlay = 0x000008000;     // put a link overlay on icon
            public const uint ShgfiLargeicon = 0x000000000;     // get large icon
            public const uint ShgfiSmallicon = 0x000000001;     // get small icon
            public const uint ShgfiUsefileattributes = 0x000000010;     // use passed dwFileAttribute
            public const uint FileAttributeNormal = 0x00000080;
            [DllImport("Shell32.dll")]
            public static extern IntPtr SHGetFileInfo(
                string pszPath,
                uint dwFileAttributes,
                ref Shfileinfo psfi,
                uint cbFileInfo,
                uint uFlags
                );
        }

        static class User32
        {

            [DllImport("User32.dll")]
            public static extern int DestroyIcon(IntPtr hIcon);
        }
    }

}
