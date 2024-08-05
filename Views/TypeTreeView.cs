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
            CreateTreeView(viewModel.Types, viewModel.PropertyValues);
        }

        private void CreateTreeView(IEnumerable<Type> types, Dictionary<string, object> propertyValues)
        {
            // Create TreeView
            var treeView = new TreeView();

            // Create TreeViewItem for each type
            foreach (var type in types)
            {
                var typeNode = CreateTreeViewItem(type, propertyValues);
                treeView.Items.Add(typeNode);
            }

            // Replace the content of the UserControl with the new TreeView
            (Content as Grid)?.Children.Clear();
            (Content as Grid)?.Children.Add(treeView);
        }

        private TreeViewItem CreateTreeViewItem(Type type, Dictionary<string, object> propertyValues)
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
                    ? CreateTreeViewItem(property.PropertyType, propertyValues)
                    : CreateTreeViewItem(property, propertyValues);
                treeViewItem.Items.Add(childNode);
            }

            return treeViewItem;
        }

        private TreeViewItem CreateTreeViewItem(PropertyInfo property, Dictionary<string, object> propertyValues)
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

                if (propertyValues.TryGetValue(property.Name, out var value))
                {
                    textBox.Text = value?.ToString();
                }

                textBox.TextChanged += (s, e) =>
                {
                    propertyValues[property.Name] = textBox.Text;
                };

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