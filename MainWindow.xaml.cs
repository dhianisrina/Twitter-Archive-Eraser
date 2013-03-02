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
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Configuration;

using LinqToTwitter;
using System.Diagnostics;
using System.Net;

using System.Resources;
using System.IO;
using System.Reflection;

namespace Twitter_Archive_Eraser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string twitterConsumerKey = ConfigurationManager.AppSettings["twitterConsumerKey"];
        private string twitterConsumerSecret = ConfigurationManager.AppSettings["twitterConsumerSecret"];

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        ITwitterAuthorizer PerformAuthorization()
        {
            // validate that credentials are present
            if (string.IsNullOrWhiteSpace(twitterConsumerKey) ||
                string.IsNullOrWhiteSpace(twitterConsumerSecret))
            {
                MessageBox.Show(@"Error while setting " +
                                    "App.config/appSettings. \n\n" +
                                    "You need to provide your twitterConsumerKey and twitterConsumerSecret in App.config \n" +
                                    "Please visit http://dev.twitter.com/apps for more info.\n");

                return null;
            }

            // configure the OAuth object
            var auth = new PinAuthorizer
            {
                Credentials = new InMemoryCredentials
                {
                    ConsumerKey = twitterConsumerKey,
                    ConsumerSecret = twitterConsumerSecret
                },
                UseCompression = true,
                GoToTwitterAuthorization = pageLink => Process.Start(pageLink),
                GetPin = () =>
                {
                    // this executes after user authorizes, which begins with the call to auth.Authorize() below.
                    
                    PinWindow pinw = new PinWindow();
                    pinw.Owner = this;
                    if (pinw.ShowDialog() == true)
                        return pinw.Pin;
                    else
                        return "";
                }
            };

            // start the authorization process (launches Twitter authorization page).
            try
            {
                auth.Authorize();
            }
            catch (WebException ex)
            {
                MessageBox.Show("Unable to authroize with Twitter right now. Please check pin number", "Twitter Archive Eraser",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                return null;
            }

            return auth;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void btnAuthorize_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            ITwitterAuthorizer auth = PerformAuthorization();

            if (auth == null)
                return;

            var ctx = new TwitterContext(auth);

            Application.Current.Properties["context"] = ctx;
            Application.Current.Properties["userName"] = ctx.UserName;

            userName.Text = "@" + ctx.UserName;
            stackWelcome.Visibility = System.Windows.Visibility.Visible;
            btnAuthorize.IsEnabled = false;
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            ArchiveFiles page = new ArchiveFiles();
            this.Hide();
            page.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            page.ShowDialog();
            //Application.Current.Shutdown();
        }
    }
}
