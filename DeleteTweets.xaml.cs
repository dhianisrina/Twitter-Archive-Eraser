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
using System.Globalization;
using System.Windows.Data;
using System.Threading.Tasks;

namespace Twitter_Archive_Eraser
{
    /// <summary>
    /// Interaction logic for DeleteTweets.xaml
    /// </summary>
    public partial class DeleteTweets : Window
    {
        ObservableRangeCollection<Tweet> tweets = new ObservableRangeCollection<Tweet>();

        //Used for filtering the tweets
        ICollectionView tweetsCollectionView;

        string userName = (string)Application.Current.Properties["userName"];

        bool hitReturn = false;
        bool isErasing = false;


        public DeleteTweets()
        {
            InitializeComponent();
            this.Loaded += DeleteTweets_Loaded;
        }

        void DeleteTweets_Loaded(object sender, RoutedEventArgs e)
        {
            LoadTweets();
            //Thread.CurrentThread.CurrentCulture = new CultureInfo("es-PE"); 
        }


        private void LoadTweets()
        {
            List<string> jsFiles = (List<string>)Application.Current.Properties["jsFiles"];

            foreach (string file in jsFiles)
            {
                if (file.EndsWith(".js"))
                    tweets.AddRange(GetTweetsFromJsFile(file));

                /*if(file.EndsWith(".csv"))
                    tweets.AddRange(GetTweetsFromCsvFile(file));*/
            }

            tweetsCollectionView = CollectionViewSource.GetDefaultView(tweets);
            gridTweets.ItemsSource = tweetsCollectionView;

            txtTotalTweetsNB.Text = String.Format("(Total tweets: {0})", tweets.Count);
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

            //string datePattern = "ddd MMM dd H:m:s zzz yyyy";
            string datePattern = "yyyy-MM-dd H:m:s zzz";
                               //"2013-06-06 00:16:40 +0000"

            List<Tweet> result = new List<Tweet>();
            Tweet tmp = null;
            DateTimeOffset dto;

            foreach (var item in tweets)
            {
                tmp = new Tweet { ID = item.id_str, Text = item.text, ToErase = true, Status = "" };

                //We use this to prevent the app from crashing if twitter changes the date-time format, again!
                if (DateTimeOffset.TryParseExact(item.created_at, datePattern, CultureInfo.InvariantCulture, DateTimeStyles.None, out dto))
                {
                    tmp.Date = dto.DateTime;
                }

                result.Add(tmp);
            }

            return result;

        }

        //This is absolete, we do not use CSV files anymore
        /*List<Tweet> GetTweetsFromCsvFile(string filePath)
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
                                            Date = DateTime.Parse(line.Split(new char[] { ',' })[5].Replace("\"", ""), CultureInfo.InvariantCulture),
                                            Status = ""
                                        })
                        .ToList();
        }*/


        private void SelectAllCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (tweetsCollectionView == null)
                return;

            foreach (var item in tweetsCollectionView)
            {
                Tweet t = item as Tweet;
                if (t != null)
                    t.ToErase = true;
            }
        }

        private void SelectAllCheckBox_UnChecked(object sender, RoutedEventArgs e)
        {
            if (tweetsCollectionView == null)
                return;

            foreach (var item in tweetsCollectionView)
            {
                Tweet t = item as Tweet;
                if (t != null)
                    t.ToErase = false;
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
            cancellationSource.Cancel();

            //TODO! cancel threads on exiting

            if (!hitReturn)
                Application.Current.Shutdown();
        }

        void DisableControls()
        {
            //Could be called from a different thread
            btnEraseTweetsLabel.Dispatcher.BeginInvoke(new Action(delegate()
            {
                btnEraseTweetsLabel.Text = "Stop";

                stackProgress.Visibility = System.Windows.Visibility.Visible;
                btnBack.IsEnabled = false;

                grpFilterTweets.IsEnabled = false;
                grpParallelConnections.IsEnabled = false;
            }));
        }

        void EnableControls()
        {
            btnEraseTweetsLabel.Dispatcher.BeginInvoke(new Action(delegate()
            {
                btnEraseTweetsLabel.Text = "Erase selected tweets";
                btnEraseTweets.IsEnabled = true;
                btnBack.IsEnabled = true;

                grpFilterTweets.IsEnabled = true;
                grpParallelConnections.IsEnabled = true;
            }));
        }

        private void btnEraseTweets_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            if (!isErasing)
            {
                if (MessageBox.Show("Are you sure you want to delete all the selected tweets.\nThis cannot be undone!", "Twitter Archive Eraser",
                                    MessageBoxButton.OKCancel, MessageBoxImage.Warning)
                    == MessageBoxResult.OK)
                {
                    new Thread(StartTwitterErase).Start((int)sliderParallelConnections.Value);
                    isErasing = true;

                    DisableControls();
                }
            }
            else
            {
                if (MessageBox.Show("Are you sure you want to cancel?", "Twitter Archive Eraser",
                                    MessageBoxButton.OKCancel, MessageBoxImage.Warning)
                    == MessageBoxResult.OK)
                {
                    cancellationSource.Cancel();
                    isErasing = false;

                    btnEraseTweets.IsEnabled = false;
                    btnEraseTweetsLabel.Text = "Stopping...";
                }
            }
            
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }


        void ApplyFilterToCollectionView()
        {
            tweetsCollectionView.Filter = t =>
            {
                Tweet tweet = t as Tweet;
                if (tweet == null) return false;

                return tweet.Text.ToLower().Contains(txtFilterTweets.Text.ToLower());
            };
            tweetsCollectionView.Refresh();
        }


        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            //CollectionViewSource tweetsDataView = this.Resources["tweetsDataView"] as CollectionViewSource;
            ApplyFilterToCollectionView();
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            tweetsCollectionView.Filter = t => { return true; };
            tweetsCollectionView.Refresh();
        }


        #region Erasing Tweets Logic

        //shared state variables
        int nextTweetID = 0;
        Object _lockerNextTweetID = new Object();

        int nbTweetsDeleted = 0;
        Object _lockerNbTweetsDeleted = new Object();

        //In case of a cancelation
        CancellationTokenSource cancellationSource = new CancellationTokenSource();

        //The number of the tweets to erase
        int nbTweetsToErase;

#if DEBUG
        int sleepFakeWaitMilliseconds;
#endif

        void onDeletingTweetUIUpdate(Tweet tweet)
        {
            lock (_lockerNbTweetsDeleted)
            {
                nbTweetsDeleted++;
            }

            //update datagrid
            gridTweets.Dispatcher.BeginInvoke(new Action(delegate()
            {
                gridTweets.SelectedItem = tweet;
                gridTweets.ScrollIntoView(tweet);
            }));

            //update progressbar
            progressBar.Dispatcher.BeginInvoke(new Action(delegate()
            {
                progressBar.Value = nbTweetsDeleted * 100 / nbTweetsToErase;
                txtPrcnt.Text = nbTweetsDeleted * 100 / nbTweetsToErase + "%";
            }));
        }

        //Fetched the index (in the tweets collection) of the next tweet to be deleted
        int getNextTweetIDSync()
        {
            //return the next val, increment
            lock (_lockerNextTweetID)
            {
                //As long as we have more tweets to erase
                while (nextTweetID < tweets.Count 
                       && (tweets[nextTweetID].ToErase == false || !String.IsNullOrEmpty(tweets[nextTweetID].Status)))
                {
                    nextTweetID++;
                }

                //Have we reached the end?
                if(nextTweetID == tweets.Count)
                {
                    return Int32.MinValue;
                }
                else //We have got a new tweet to erase
                {
                    //Prepare the next call to fetch the next tweet
                    nextTweetID++;
                    return nextTweetID - 1;
                }
            }
        }


        //We start multiple actions in parallel to delete tweets
        void EraseTweetsAction(TwitterContext ctx, CancellationToken cancelToken) {

            int nextTweetID = getNextTweetIDSync();

            //Are we done?
            while (nextTweetID != Int32.MinValue)
            {
                //We can't cancel here, we have already fetched a new ID and if we cancel here it will never be deteled

                Tweet tweet = tweets[nextTweetID];

                //Clear Tweets logic here
                try
                {
#if DEBUG
                    Thread.Sleep(sleepFakeWaitMilliseconds);
#else
                    Status ret = ctx.DestroyStatus(tweet.ID);
#endif
                    tweet.Status = "[DELETED ✔]";
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("Sorry, that page does not exist"))
                        tweet.Status = "[NOT FOUND ǃ]";
                    else if (ex.Message.Contains("You may not delete another user's status"))
                        tweet.Status = "[NOT ALLOWED ❌]";
                    else
                        tweet.Status = "[ERROR]";
                }

                onDeletingTweetUIUpdate(tweet);

                //We cancel once a tweet is completely handeled, we make sure not to request for a new one
                if (cancelToken.IsCancellationRequested)
                    return;

                nextTweetID = getNextTweetIDSync();
            }
        }

        void StartTwitterErase(object nbParallelConnectionsObj)
        {
            int nbParallelConnections = (int)nbParallelConnectionsObj;

            TwitterContext ctx = (TwitterContext)Application.Current.Properties["context"];

            //No need to synchronize here, all tasks are (supposed?) not started yet.
            nbTweetsToErase = tweets.Where(t => t.ToErase == true || !String.IsNullOrEmpty(t.Status)).Count();

#if !DEBUG
            if (ctx == null)
            {
                MessageBox.Show("Error loading twitter authentication info; please try again");
                isErasing = false;
                EnableControls();
                return;
            }
#endif

#if DEBUG
            sleepFakeWaitMilliseconds = 5000 / nbParallelConnections;
#endif

            ParallelOptions options = new ParallelOptions();
            options.MaxDegreeOfParallelism = nbParallelConnections;
            cancellationSource = new CancellationTokenSource();
            //options.CancellationToken = cancellationSource.Token;

            //Action[] eraseActions = new Action[nbParallelConnections];
            Task[] tasks = new Task[nbParallelConnections];

            for (int i = 0; i < nbParallelConnections; i++)
            {
                //eraseActions[i] = () => EraseTweetsAction(ctx, cancellationSource.Token);
                tasks[i] = Task.Factory.StartNew(() => EraseTweetsAction(ctx, cancellationSource.Token));
            }

            try
            {
                //Parallel.Invoke(options, eraseActions);
                Task.WaitAll(tasks);
                EnableControls();
            }
            catch (Exception e)
            {
                nextTweetID = 0;
            }

            if (nbTweetsDeleted >= (nbTweetsToErase - nbParallelConnections))
            {
                progressBar.Dispatcher.BeginInvoke(new Action(delegate()
                {
                    progressBar.Value = 100;
                    txtPrcnt.Text = "100%";
                    
                    EnableControls();

                    MessageBox.Show("Done! Everything is clean ;).\n", "Twitter Archive Eraser", MessageBoxButton.OK, MessageBoxImage.Information);
                }));
            }
        }

        #endregion

        private void sliderParallelConnections_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            grpParallelConnections.Header = "Number of parallel connections: " + sliderParallelConnections.Value + " ";
        }

        private void txtFilterTweets_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                ApplyFilterToCollectionView();
            }
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
