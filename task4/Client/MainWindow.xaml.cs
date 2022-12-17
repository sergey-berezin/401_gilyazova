using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using EmotionFerPlus;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IO;
using System.Collections.Generic;
using Polly;
using Polly.Retry;
using System.Windows.Shapes;


namespace Client
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string url = "http://localhost:5021";
        public event PropertyChangedEventHandler? PropertyChanged;
        private AsyncRetryPolicy _RetryPolicy;
        private int MaxRetries = 3;
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private Emotion EmotionFerPlusModel = new Emotion();
        private CancellationTokenSource cts = new CancellationTokenSource();
        private ObservableCollection<ImageInfo> listImages = new ObservableCollection<ImageInfo>();

        public string[]? ImagesPath = null;
        private bool IsCalculationInProgress = false;
        public bool IsCalculation
        {
            get
            {
                return IsCalculationInProgress;
            }
            set
            {
                IsCalculationInProgress = value;
                RaisePropertyChanged(nameof(IsCalculation));
            }
        }


        public ICommand Clear { get; private set; }
        public ICommand Cancel { get; private set; }
        public ICommand DeleteImageFromDB { get; private set; }


        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            photos.ItemsSource = listImages;
            Clear = new RelayCommand(_ => { HandlerClear(this); }, CanClear);
            Cancel = new RelayCommand(_ => { HandlerCancel(this); }, CanCancel);
            DeleteImageFromDB = new RelayCommand(_ => { DoDelete(this); }, CanDelete);
        }


        private void OpenImage(object sender, RoutedEventArgs? e = null)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = true;


            if (ofd.ShowDialog() == true)
            {
                ImagesPath = new string[ofd.FileNames.Length];
                for (int i = 0; i < ImagesPath.Length; i++)
                    ImagesPath[i] = ofd.FileNames[i];
                _RetryPolicy = Policy.Handle<HttpRequestException>().WaitAndRetryAsync(MaxRetries, times =>
                TimeSpan.FromMilliseconds(3000));
            }
            
        }


        private async Task GetEmotions(string path, CancellationTokenSource ctn)
        {
            try
            {
                await _RetryPolicy.ExecuteAsync(async () =>
                {
                    var img = await File.ReadAllBytesAsync(path, ctn.Token);
                    var httpClient = new HttpClient();
                    httpClient.BaseAddress = new Uri($"{url}/images");
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var response = await HttpClientJsonExtensions.PostAsJsonAsync(httpClient, "", img);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private async void Upload(object sender, RoutedEventArgs? e = null)
        {
            try
            {
                if ((ImagesPath != null) && (ImagesPath.Length > 0))
                {
                    IsCalculation = true;
                    LoadButton.IsEnabled = false;
                    foreach (var path in ImagesPath)
                    {
                        await GetEmotions(path, cts);
                    }
                    ImagesPath = null;
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                IsCalculation = false;
                LoadButton.IsEnabled = true;
                photos.Focus();
                //MessageBox.Show(ImageInfoId)
            }
        }


        public bool[] possible_emotions { get; private set; } = new bool[] { false, false, false, true, false, false, false, false };
        private string[] emotion_options = new string[] { "surprise", "sadness", "happiness", "neutral", "fear", "contempt", "anger", "disgust" };


        public int SelectedMode
        {
            get { return Array.IndexOf(possible_emotions, true); }
        }


        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (IsCalculation == true)
                return;
            int mode = SelectedMode;
            //listImages = new ObservableCollection<ImageInfo>(listImages.OrderByDescending(collection => collection.dict[emotion_options[mode]]));
            //photos.ItemsSource = listImages;
            listImages = new ObservableCollection<ImageInfo>(
                listImages.OrderByDescending(p => p.emotions.Where(p => p.name == emotion_options[mode]).Max(p => p.value))
                );
            photos.ItemsSource = listImages;
        }

        private void HandlerClear(object sender)
        {
            listImages.Clear();
        }

        private void HandlerCancel(object sender)
        {
            cts.Cancel();
        }

        private bool CanClear(object sender)
        {
            return !IsCalculation;
        }

        private bool CanCancel(object sender)
        {
            return IsCalculation;
        }

        private bool CanDelete(object sender)
        {
            if (photos.SelectedItem != null)
                return true;
            return false;
        }

        private async void UploadFromDB(object sender, RoutedEventArgs e)
        {
            try
            {
                await _RetryPolicy.ExecuteAsync(async () =>
                {
                    var httpClient = new HttpClient();
                    var response = await httpClient.GetAsync($"{url}/images");

                    if (response.IsSuccessStatusCode)
                    {
                        List<int> values = await response.Content.ReadFromJsonAsync<List<int>>();
                        if (values.Count == 0)
                        {
                            throw new Exception("No images in database yet!");
                        }
                        foreach (int val in values)
                        {
                            var response_inner = await httpClient.GetAsync($"{url}/images/{val}");
                            ImageInfo item = await response_inner.Content.ReadFromJsonAsync<ImageInfo>();
                            listImages.Add(item);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Fail!");
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private async void DoDelete(object sender)
        {
            try
            {
                await _RetryPolicy.ExecuteAsync(async () =>
                {
                    var httpClient = new HttpClient();
                    var response = await httpClient.DeleteAsync($"{url}/images");
                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Success!");
                    }
                    else
                    {
                        MessageBox.Show("Fail!");
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}

