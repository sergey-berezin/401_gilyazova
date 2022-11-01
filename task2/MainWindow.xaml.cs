using Microsoft.Win32;
using System;
using System.IO;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using EmotionFerPlus;
using System.Collections.ObjectModel;

namespace emotions_wpf
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] String propertyName = "") =>
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

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            photos.ItemsSource = listImages;
            Clear = new RelayCommand(_ => { HandlerClear(this); }, CanClear);
            Cancel = new RelayCommand(_ => { HandlerCancel(this); }, CanCancel);
        }

        private void OpenImage(object sender, RoutedEventArgs? e = null)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = true;
            ofd.InitialDirectory = Path.GetFullPath("../../../../Images");

            if (ofd.ShowDialog() == true)
            {
                ImagesPath = new string[ofd.FileNames.Length];
                for (int i = 0; i < ImagesPath.Length; i++)
                    ImagesPath[i] = ofd.FileNames[i];
            }
        }

        private async Task GetEmotions(string path, CancellationTokenSource ctn)
        {
            try
            {
                using Image<Rgb24> image = Image.Load<Rgb24>(path);
                var result = await EmotionFerPlusModel.ProcessAsync(image, ctn.Token);
                //var ordered = result.OrderByDescendingOrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
                listImages.Add(new ImageInfo(path, result));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
        }

        private async void Upload(object sender, RoutedEventArgs? e = null)
        {
            try
            {
                if ((ImagesPath != null) && (ImagesPath.Length > 0))
                {


                    IsCalculation = true;
                    foreach (var path in ImagesPath)
                    {
                        await GetEmotions(path, cts);

                    }
                }
            }
            catch { }
            finally
            {
                IsCalculation = false;
                photos.Focus();
            }
        }


        public bool[] possible_emotions { get; private set; } = new bool[] { false, false, false, true, false, false, false, false };
        private string[] emotion_options = new string[] { "surprise", "sadness", "happiness", "neutral", "fear", "contempt", "anger", "disgust"};

        public int SelectedMode
        {
            get { return Array.IndexOf(possible_emotions, true); }
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (IsCalculation == true)
                return;
            int mode = SelectedMode;
            listImages = new ObservableCollection<ImageInfo>(listImages.OrderByDescending(collection => collection.dict[emotion_options[mode]]));
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

      
    }
}
