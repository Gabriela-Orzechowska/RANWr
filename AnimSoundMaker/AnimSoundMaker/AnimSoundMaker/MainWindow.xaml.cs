using System.ComponentModel;
using System.Windows;
using static lib_RASD;

namespace AnimSoundMaker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
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

        public MainWindow()
        {
            
            InitializeComponent();
            Editor_RASD editor = new();
            RASD rasd = (lib_RASD.RASD)lib_RASD.TryOpenRASD("C:\\NintendoWare\\Revolution\\AnimSoundMaker\\sample\\sample.rasd");
            editor.LoadData(rasd);
            TestPage.Content = editor;
            Data = new();
            Data.Creator = rasd.Header.CreatorName;
            Data.Generator = rasd.Header.Generator;
            Data.DateModified = rasd.Header.DataSaved;
            PropertyBox.SelectedObject = Data;
            PropertyBox.NameColumnWidth = 60;
          
        }
    }
}
