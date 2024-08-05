using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;

namespace GraphComponents.ViewModels
{
    public class TypeTreeViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Type> _types;
        public ObservableCollection<Type> Types
        {
            get => _types;
            set
            {
                if (_types != value)
                {
                    _types = value;
                    OnPropertyChanged(nameof(Types));
                }
            }
        }

        public TypeTreeViewModel(IEnumerable<Type> types)
        {
            Types = new ObservableCollection<Type>(types);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
