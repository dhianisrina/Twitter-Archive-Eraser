using LinqToTwitter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using Newtonsoft.Json;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace Twitter_Archive_Eraser
{
    /// <summary>
    /// Interaction logic for DeleteTweets.xaml
    /// </summary>
    public partial class DeleteTweets : Window
    {
        ObservableRangeCollection<Tweet> tweets = new ObservableRangeCollection<Tweet>();
        string userName = (string)Application.Current.Properties["userName"];

        bool hitReturn = false;
        bool exitRequest = false;

        public DeleteTweets()
        {
            InitializeComponent();
            LoadTweets();
        }

        private void LoadTweets()
        {
            List<string> csvFiles = (List<string>)Application.Current.Properties["csvFiles"];

            foreach (string file in csvFiles)
            {
                if (file.EndsWith(".js"))
                    tweets.AddRange(GetTweetsFromJsFile(file));

                if(file.EndsWith(".csv"))
                    tweets.AddRange(GetTweetsFromCsvFile(file));
            }

            gridTweets.ItemsSource = tweets;
        }

        class tweetTJson
        {
            public string id_str { get; set; }
            public string text { get; set; }
            public string created_at { get; set; }
        }

        List<Tweet> GetTweetsFromJsFile(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            List<tweetTJson> tweets = null;

            try
            {
                string jsonData = File.ReadAllText(filePath);
                jsonData = jsonData.Substring(jsonData.IndexOf('[') <= 0 ? 0 : jsonData.IndexOf('[') - 1); 
                tweets = JsonConvert.DeserializeObject<List<tweetTJson>>(jsonData);
            }
            catch (Exception)   //file is not a suitable json
            {
                
            }

            if (tweets == null)
                return null;

            string datePattern = "ddd MMM dd H:m:s zzz yyyy";

            return tweets.Where(tJson => tJson.created_at != null && tJson.id_str != null && tJson.text != null)
                         .Select(tJson => new Tweet
                                        {
                                            ID = tJson.id_str,
                                            Text = tJson.text,
                                            ToErase = true,
                                            Date = DateTimeOffset.ParseExact(tJson.created_at, datePattern, null).DateTime,
                                            Status = ""
                                        })
                        .ToList();
        }

        List<Tweet> GetTweetsFromCsvFile(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            return File.ReadAllLines(filePath)
                        .Skip(1)   //skip header
                        .Where(line => line.Count(c => c == ',') >= 7) //line must have all CSV entries
                        .Select(line => new Tweet
                                        {
                                            ID = line.Split(new char[] { ',' })[0].Replace("\"", ""),
                                            Text = line.Split(new char[] { ',' })[7].Replace("\"", ""),
                                            ToErase = true,
                                            Date = DateTime.Parse(line.Split(new char[] { ',' })[5].Replace("\"", "")),
                                            Status = ""
                                        })
                        .ToList();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void SelectAllCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var item in tweets)
            {
                item.ToErase = true;
            }
        }

        private void SelectAllCheckBox_UnChecked(object sender, RoutedEventArgs e)
        {
            foreach (var item in tweets)
            {
                item.ToErase = false;
            }
        }

        private void DG_Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Hyperlink link = e.OriginalSource as Hyperlink;
            string tweetID = link.NavigateUri.ToString();

            string url = "https://twitter.com/" + userName + "/statuses/" + tweetID;

            Process.Start(url);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            hitReturn = true;
            this.Close();
        }

        private void Window_Closing_1(object sender, CancelEventArgs e)
        {
            exitRequest = true;

            if (!hitReturn)
                Application.Current.Shutdown();
        }

        private void btnEraseTweets_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (MessageBox.Show("Are you sure you want to delete all the selected tweets.\nThis cannot be undone!", "Twitter Archive Eraser", 
                                MessageBoxButton.OKCancel, MessageBoxImage.Warning)
                == MessageBoxResult.OK)
            {
                stackProgress.Visibility = System.Windows.Visibility.Visible;
                btnBack.IsEnabled = false;
                btnEraseTweets.IsEnabled = false;

                Thread th = new Thread(StartTwitterErase);
                th.Start();
            }
            
            //StartTwitterErase();
        }

        void StartTwitterErase()
        {
            TwitterContext ctx = (TwitterContext)Application.Current.Properties["context"];
            if (ctx == null)
            {
                MessageBox.Show("Error loading twitter authentication info; please try again");
                return;
            }

            int nbDeleted = 0;
            foreach (var tweet in tweets)
            {
                //If application exit, stop working
                if (exitRequest)
                    return;

                if (tweet.ToErase == false)
                    continue;

                //update datagrid
                gridTweets.Dispatcher.BeginInvoke(new Action(delegate()
                {
                    gridTweets.SelectedItem = tweet;
                    gridTweets.ScrollIntoView(tweet);
                }));

                //update progressbar
                progressBar.Dispatcher.BeginInvoke(new Action(delegate()
                {
                    progressBar.Value = nbDeleted * 100 / tweets.Count;
                    txtPrcnt.Text = nbDeleted * 100 / tweets.Count + "%";
                }));

                try
                {
                    Status ret = ctx.DestroyStatus(tweet.ID);
                    tweet.Status = "[DELETED ✔]";
                }
                catch (Exception ex)
                {
                    if(ex.Message.Contains("Sorry, that page does not exist"))
                        tweet.Status = "[NOT FOUND ǃ]";
                    else if(ex.Message.Contains("You may not delete another user's status"))
                        tweet.Status = "[NOT ALLOWED ❌]";
                    else
                        tweet.Status = "[ERROR]";
                }

                ++nbDeleted;
            }

            btnEraseTweets.Dispatcher.BeginInvoke(new Action(delegate()
            {
                progressBar.Value = 100;
                txtPrcnt.Text = "100%";
                btnEraseTweets.IsEnabled = true;
                btnBack.IsEnabled = true;

                MessageBox.Show("Done! Everything is clean ;).\n", "Twitter Archive Eraser", MessageBoxButton.OK, MessageBoxImage.Information);
            }));
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }

    public class Tweet : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string ID { set; get; }
        public string Text { set; get; }
        public DateTime Date { set; get; }

        private string _status;
        public string Status {
            get { return _status; }
            set {
                if (value != _status)
                {
                    _status = value;
                    NotifyPropertyChanged("");
                }
            }
        }

        private bool _toErase;
        public bool ToErase {
            get { return _toErase; }

            set
            {
                if (value != _toErase)
                {
                    _toErase = value;
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
