using System.Collections.ObjectModel;
using System.ComponentModel;
using System;

namespace GraphComponents.ViewModels
{
    public class ToolboxViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Type> AvailableCommands { get; } = new ObservableCollection<Type>
        {

        };

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
