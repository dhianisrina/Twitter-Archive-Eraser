using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Twitter_Archive_Eraser
{
    /// <summary>
    /// Interaction logic for ArchiveFiles.xaml
    /// </summary>
    public partial class ArchiveFiles : Window
    {
        ObservableRangeCollection<JsFile> jsFiles = new ObservableRangeCollection<JsFile>();

        public ArchiveFiles()
        {
            InitializeComponent();
            jsFiles.Clear();
        }

        private void btnAddFiles_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".js";
            dlg.Filter = "JS archive files (*.js)|*.js";
            dlg.Multiselect = true;

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                foreach (var item in dlg.FileNames)
	            {
                    //if the file is not already present
                    if(!jsFiles.Any(file => file.Path == item))
                        jsFiles.Add(new JsFile() { Path = item, Selected = false });
	            }
            }

            gridFiles.ItemsSource = jsFiles;
        }

        private void btnRemoveFiles_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            List<JsFile> selectedFiles = new List<JsFile>();
            foreach (var item in jsFiles)
            {
                if(item.Selected)
                    selectedFiles.Add(item);
            }

            foreach (var item in selectedFiles)
            {
                jsFiles.Remove(item);    
            }
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (jsFiles.Count == 0)
            {
                MessageBox.Show("Please select at least one *.js file from the twitter archive", 
                                "Twitter Archive Eraser", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            List<string> files = new List<string>();
            foreach (var item in jsFiles)
            {
                files.Add(item.Path);
            }

            Application.Current.Properties["jsFiles"] = files;

            DeleteTweets page = new DeleteTweets();
            this.Hide();
            page.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            page.ShowDialog();
            this.Show();
            //Application.Current.Shutdown();
        }

        private void SelectAllCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var item in jsFiles)
            {
                item.Selected = true;
            }
        }

        private void SelectAllCheckBox_UnChecked(object sender, RoutedEventArgs e)
        {
            foreach (var item in jsFiles)
            {
                item.Selected = false;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }


        private void Window_Closed_1(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }
    }

    public class JsFile : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string Path { get; set; }

        private bool _selected;
        public bool Selected {
            get { return _selected; } 
            set 
            {
                if (value != _selected)
                {
                    _selected = value;
                    NotifyPropertyChanged("");
                }
            }
        }

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }
}
