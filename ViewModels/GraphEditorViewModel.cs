using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Collections.Specialized;
using System.Xml.Linq;
using GraphComponents.Models;

namespace GraphComponents.ViewModels
{
    public class GraphEditorViewModel
    {
        private Graph _graph;

        public PendingConnectionViewModel PendingConnection { get; private set; }
        public ICommand DisconnectConnectorCommand { get; private set; }
        public ObservableCollection<NodeViewModel> Nodes { get; private set; } = new ObservableCollection<NodeViewModel>();
        public ObservableCollection<ConnectionViewModel> Connections { get; private set; } = new ObservableCollection<ConnectionViewModel>();

        public GraphEditorViewModel(Graph graph)
        {
            PendingConnection = new PendingConnectionViewModel(this);

            DisconnectConnectorCommand = new DelegateCommand<ConnectorViewModel>(connector =>
            {
                var connection = Connections.FirstOrDefault(x => x.Source == connector || x.Target == connector);
                if (connection != null)
                {
                    connection.Source.IsConnected = false;
                    connection.Target.IsConnected = false;
                    Connections.Remove(connection);
                }
            });

            _graph = graph;
            _graph.Nodes.CollectionChanged += (o, e) =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        this.Nodes.Clear();
                        break;
                }
            };

            _graph.Edges.CollectionChanged += (o, e) =>
            {

            };
        }

        protected bool IsPrimitiveType(Type type) =>
            type.IsPrimitive || type == typeof(string) || type == typeof(object);
    }

    public class NodeViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private const int SnapSize = 20;

        private Point _location;
        public Point Location
        {
            get => _location;
            set
            {
                _location = SnapToGrid(value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Location)));
            }
        }

        public string Title { get; set; }

        private ICollection<ConnectorViewModel> _inputConnectorViewModels;

        public ICollection<ConnectorViewModel> InputConnectorViewModels => _inputConnectorViewModels;

        private ICollection<ConnectorViewModel> _outputConnectorViewModels;

        public ICollection<ConnectorViewModel> OutputConnectors => _outputConnectorViewModels;

        public NodeViewModel(string title, Point location, IEnumerable<ConnectorViewModel> inputConnectorViewModels, IEnumerable<ConnectorViewModel> outputConnectorViewModels)
        {
            this.Title = title;
            this.Location = location;
            this._inputConnectorViewModels = new List<ConnectorViewModel>(inputConnectorViewModels);
            this._outputConnectorViewModels = new List<ConnectorViewModel>(outputConnectorViewModels);
        }

        private Point SnapToGrid(Point point)
        {
            return new Point(Math.Round(point.X / SnapSize) * SnapSize, Math.Round(point.Y / SnapSize) * SnapSize);
        }
    }

    public class ConnectorViewModel : INotifyPropertyChanged
    {
        private Point _anchor;
        public Point Anchor
        {
            set
            {
                _anchor = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Anchor)));
            }
            get => _anchor;
        }

        private bool _isConnected = false;
        public bool IsConnected
        {
            set
            {
                _isConnected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsConnected)));
            }
            get => _isConnected;
        }

        private string _title;
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
            }
        }

        private object _value;
        public object Value
        {
            get => _value;
            set
            {
                if (!Equals(_value, value))
                {
                    _value = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ConnectorViewModel(Point anchor = default, bool isConnected = false, string title = default, object value = default)
        {
            _anchor = anchor;
            _isConnected = isConnected;
            _title = title;
            _value = value;
        }
    }

    public class ConnectionViewModel
    {
        private Graph _graph;

        public ConnectorViewModel Source { get; }

        public ConnectorViewModel Target { get; }

        public ConnectionViewModel(ConnectorViewModel source, ConnectorViewModel target)
        {
            Source = source;
            Target = target;

            Source.IsConnected = true;
            Target.IsConnected = true;
        }
    }

    public class PendingConnectionViewModel
    {
        private readonly GraphEditorViewModel _editor;
        private ConnectorViewModel _source;

        public PendingConnectionViewModel(GraphEditorViewModel editor)
        {
            _editor = editor;
            StartCommand = new DelegateCommand<ConnectorViewModel>(source => _source = source);
            FinishCommand = new DelegateCommand<ConnectorViewModel>(target =>
            {
                if (target != null && _source != null)
                {
                    //_editor.Connect(_source, target);
                    var existingConnection = editor.Connections.FirstOrDefault(c => c.Source == _source && c.Target == target);
                    if (existingConnection == null)
                    {
                        editor.Connections.Add(new ConnectionViewModel(_source, target));
                    }
                }
            });
        }

        public ICommand StartCommand { get; }
        public ICommand FinishCommand { get; }
    }
}