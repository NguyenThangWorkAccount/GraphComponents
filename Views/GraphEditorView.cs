using GraphComponents.ViewModels;
using Nodify;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GraphComponents.Views
{
    public partial class GraphEditorView : UserControl
    {
        public GraphEditorView(GraphEditorViewModel graphEditor)
        {
            InitializeComponent();
            DataContext = graphEditor;            
        }

        private void OnDropNode(object sender, DragEventArgs e)
        {
            
        }

        private bool IsPrimitiveType(Type type) =>
            type.IsPrimitive || type == typeof(string) || type == typeof(object);
    }
}