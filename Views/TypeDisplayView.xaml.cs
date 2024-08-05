using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using GraphComponents.ViewModels;

namespace GraphComponents.Views
{
    public partial class TypeTreeView : UserControl
    {
        public TypeTreeView()
        {
            InitializeComponent();
        }

        public void LoadData(TypeTreeViewModel viewModel)
        {
            // Create TreeView dynamically
            CreateTreeView(viewModel.Types);
        }

        private void CreateTreeView(IEnumerable<Type> types)
        {
            // Create TreeView
            var treeView = new TreeView();

            // Create TreeViewItem for each type
            foreach (var type in types)
            {
                var typeNode = CreateTreeViewItem(type);
                treeView.Items.Add(typeNode);
            }

            // Replace the content of the UserControl with the new TreeView
            (Content as Grid)?.Children.Clear();
            (Content as Grid)?.Children.Add(treeView);
        }

        private TreeViewItem CreateTreeViewItem(Type type)
        {
            var treeViewItem = new TreeViewItem
            {
                Header = type.Name,
                Tag = type
            };

            // Add children for each property of the type
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var childNode = !IsPrimitive(property.PropertyType) 
                    ? CreateTreeViewItem(property.PropertyType)
                    : CreateTreeViewItem(property);
                treeViewItem.Items.Add(childNode);
            }

            return treeViewItem;
        }

        private TreeViewItem CreateTreeViewItem(PropertyInfo property)
        {
            var treeViewItem = new TreeViewItem
            {
                Header = $"{property.Name} ({property.PropertyType.Name}):"
            };

            // Create TextBox if property type is primitive
            if (IsPrimitive(property.PropertyType))
            {
                var stackPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal
                };

                var textBlock = new TextBlock
                {
                    Text = treeViewItem.Header.ToString()
                };

                var textBox = new TextBox
                {
                    Width = 100,
                    Margin = new Thickness(5, 0, 0, 0)
                };

                // Bind TextBox to a dummy property value (you may need to bind to actual property values in a real scenario)
                textBox.SetBinding(TextBox.TextProperty, new Binding("Value") { Mode = BindingMode.TwoWay });

                stackPanel.Children.Add(textBlock);
                stackPanel.Children.Add(textBox);
                treeViewItem.Header = stackPanel;
            }
            else
            {
                treeViewItem.Header = $"{property.Name} ({property.PropertyType.Name})";
            }

            return treeViewItem;
        }

        private bool IsPrimitive(Type type)
        {
            return type.IsPrimitive || type == typeof(string);
        }
    }
}
