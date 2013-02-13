using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace Twitter_Archive_Eraser
{
    /// <summary>
    /// Interaction logic for PinWindow.xaml
    /// </summary>
    public partial class PinWindow : Window
    {
        public PinWindow()
        {
            InitializeComponent();
            this.Loaded += PinWindow_Loaded;
        }

        void PinWindow_Loaded(object sender, RoutedEventArgs e)
        {
        }

        public string Pin
        {
            get
            {
                return txtPin.Text;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }
    }
}
