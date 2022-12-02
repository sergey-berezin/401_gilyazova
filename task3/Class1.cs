using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Windows.Input;
using System.IO;


namespace emotions_wpf
{
    public class ImageInfo
    {
        public int ImageInfoId { get; set; }
        public string filename { get; set; }
        public string path { get; set; }
        public int hash { get; set; }
        public ImageValue value { get; set; }
        public ICollection<Emotion_> emotions { get; set; }
        public ImageInfo(string path)
        {
            //string[] splittingPath = path.Split("\\");
            filename = Path.GetFileName(path);
            this.path = path;
            emotions = new List<Emotion_>();
        }
    }

    public class ImageValue
    {
        [Key]
        public int ImageInfoId { get; set; }
        public byte[] data { get; set; }
        public ImageInfo image { get; set; }
    }

    public class Emotion_
    {
        public int Emotion_Id { get; set; }
        public float value { get; set; }
        public string name { get; set; }
        public int ImageInfoId { get; set; }
        public ImageInfo image { get; set; }
    }

    public class ApplicationContext : DbContext
    {
        public DbSet<ImageInfo> images { get; set; }
        public DbSet<Emotion_> emotions { get; set; }
        public DbSet<ImageValue> values { get; set; }

        public ApplicationContext() => Database.EnsureCreated();
        protected override void OnConfiguring(DbContextOptionsBuilder o) =>
            o.UseSqlite("Data Source=images.db");
    }

    class RelayCommand : ICommand
    {
        private readonly Action<object> execute;
        private readonly Func<object, bool> canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return canExecute(parameter);
        }
        public void Execute(object parameter)
        {
            execute?.Invoke(parameter);
        }
    }
}
