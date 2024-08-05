using GraphComponents.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using GraphComponents.Models;

namespace GraphComponents.Views
{
    public partial class ToolboxView : UserControl
    {
        private readonly Dictionary<TreeViewItem, Type> _treeViewItemCommandMap = new Dictionary<TreeViewItem, Type>();

        public ToolboxView()
        {
            InitializeComponent();
        }

        private void LoadCommands()
        {
            if (DataContext is ToolboxViewModel viewModel)
            {
                CommandsTreeView.Items.Clear();

                foreach (var commandType in viewModel.AvailableCommands)
                {
                    var treeViewItem = CreateTreeViewItem(commandType);
                    CommandsTreeView.Items.Add(treeViewItem);
                }
            }
        }

        private TreeViewItem CreateTreeViewItem(Type commandType)
        {
            var treeViewItem = new TreeViewItem
            {
                Header = commandType.Name,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top
            };

            var (inputType, outputType) = GetCommandTypes(commandType);
            
            var stackPanel1 = new StackPanel();
            var inputTreeView = new TreeView
            {
                Margin = new Thickness(0),
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            var inputTreeRootItem = CreateTreeViewItem();
            inputTreeRootItem.Header = "Input Properties";
            inputTreeView.Items.Add(inputTreeRootItem);
            ExpandTreeItem(inputTreeRootItem, inputType);
            stackPanel1.Children.Add(inputTreeView);

            var stackPanel2 = new StackPanel();
            var outputTreeView = new TreeView
            {
                Margin = new Thickness(0),
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            var outputTreeRootItem = CreateTreeViewItem();
            outputTreeRootItem.Header = "Output Properties";
            outputTreeView.Items.Add(outputTreeRootItem);
            ExpandTreeItem(outputTreeRootItem, outputType);
            stackPanel2.Children.Add(outputTreeView);

            treeViewItem.Items.Add(stackPanel1);
            treeViewItem.Items.Add(stackPanel2);

            treeViewItem.PreviewMouseLeftButtonDown += TreeViewItem_OnPreviewMouseLeftButtonDown;

            _treeViewItemCommandMap[treeViewItem] = commandType;

            return treeViewItem;
        }

        private void ExpandTreeItem(TreeViewItem parentItem, Type type)
        {
            if (IsBasicType(type))
            {
                var treeViewItem = CreateTreeViewItem();
                treeViewItem.Header = $"{type.Name}";
                parentItem.Items.Add(treeViewItem);
            }
            else
                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var treeViewItem = CreateTreeViewItem();
                    treeViewItem.Header = $"{prop.Name} : {prop.PropertyType.Name}";

                    if (!IsBasicType(prop.PropertyType))
                    {
                        ExpandTreeItem(treeViewItem, prop.PropertyType);
                    }
                    parentItem.Items.Add(treeViewItem);
                }
        }

        private TreeViewItem CreateTreeViewItem()
        {
            return new TreeViewItem
            {
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(2),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
        }

        private (Type inputType, Type outputType) GetCommandTypes(Type commandType)
        {
            var iface = commandType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<,>));

            if (iface != null)
            {
                var genericArguments = iface.GetGenericArguments();
                return (genericArguments[0], genericArguments[1]);
            }

            var baseType = commandType.BaseType;
            while (baseType != null)
            {
                iface = baseType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<,>));

                if (iface != null)
                {
                    var genericArguments = iface.GetGenericArguments();
                    return (genericArguments[0], genericArguments[1]);
                }
                baseType = baseType.BaseType;
            }

            return (null, null);
        }

        private void TreeViewItem_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem treeViewItem &&
                _treeViewItemCommandMap.TryGetValue(treeViewItem, out var commandType))
            {
                DragDrop.DoDragDrop(treeViewItem, commandType, DragDropEffects.Move);
            }
        }

        private bool IsBasicType(Type type)
        {
            return type.IsPrimitive || type.IsEnum || type == typeof(string) ||
                   (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
                    IsBasicType(type.GetGenericArguments()[0]));
        }
    }
}
