using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;


namespace emotions_wpf
{
    public class ImageInfo
    {
        public string filename { get; set; }
        public string path { get; set; }
        public Dictionary<string, float> dict { get; set; }

        public ImageInfo(string path, Dictionary<string, float> dict)
        {
            string[] splittingPath = path.Split("\\");
            this.filename = splittingPath[splittingPath.Length - 1];

            this.path = path;
            this.dict = dict.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
        }
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
