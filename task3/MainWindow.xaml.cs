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
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using EmotionFerPlus;
using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp.Advanced;
using System.Runtime.InteropServices;


namespace emotions_wpf
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
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
            DeleteImageFromDB = new RelayCommand(_ => {DoDelete(this);}, CanDelete);
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
            }
        }

        private async Task GetEmotions(string path, CancellationTokenSource ctn)
        {
            try
            {
                using Image<Rgb24> image = Image.Load<Rgb24>(path);
                int hash = image.GetHashCode();
        
                var _IMemoryGroup = image.GetPixelMemoryGroup();
                var _MemoryGroup = _IMemoryGroup.ToArray()[0];
                var img = MemoryMarshal.AsBytes(_MemoryGroup.Span).ToArray();

                using (var db = new ApplicationContext())
                {
                    var query = db.images.Where(x => x.hash == hash).Include(item => item.value);
                    var item = query.Where(x => Enumerable.SequenceEqual(x.value.data, img))
                                .Include(x => x.emotions)
                                .FirstOrDefault();
                    if ((item != null) && (item.hash == hash))
                        listImages.Add(item);
                    else
                    {
                        var result = await EmotionFerPlusModel.ProcessAsync(image, ctn.Token);
                        var tmpImage = new ImageInfo(path);
                        tmpImage.value = new ImageValue() { data = img, image = tmpImage };
                        tmpImage.hash = hash;

                        foreach (var elem in result)
                        {
                            tmpImage.emotions.Add(new Emotion_() { name = elem.Key, value = elem.Value, image = tmpImage });
                        }
                        listImages.Add(tmpImage);

                        db.images.Add(tmpImage);
                        db.SaveChanges();
                    }

                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
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
            finally
            {
                IsCalculation = false;
                LoadButton.IsEnabled = true;
                photos.Focus();
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
            using (var db = new ApplicationContext())
            {

                var res = await Task.Run(() =>
                {
                    var photos = db.images.Include(item => item.value).Include(item => item.emotions).ToList();
                    return photos;
                }, cts.Token);
                listImages = new ObservableCollection<ImageInfo>(res);
                photos.ItemsSource = listImages;
                photos.Focus();
            }
        }

        private SemaphoreSlim smp = new SemaphoreSlim(1, 1);
        private async void DoDelete(object sender)
        {
            var item = photos.SelectedItem as ImageInfo;
            if (item == null)
                return;
            await smp.WaitAsync();
            using (var db = new ApplicationContext())
            {
                var photo = db.images.Where(x => x.hash == item.hash)
                    .Include(x => x.value)
                    .Where(x => Equals(x.value.data, item.value.data))
                    .Include(x => x.emotions)
                    .FirstOrDefault();
                if (photo == null)
                {
                    return;
                }

                db.values.Remove(photo.value);
                foreach (var elem in photo.emotions)
                {
                    db.emotions.Remove(elem);
                }
                db.images.Remove(photo);
                db.SaveChanges();
                listImages.Remove(item);
            }
            smp.Release();
        }
    }
}
