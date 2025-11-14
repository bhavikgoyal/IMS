using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media; // Required for VisualTreeHelper

namespace IMS // <--- IMPORTANT: Ensure this 'IMS' matches your project's root namespace
{
    public class TreeViewItemIndentVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // The 'value' here is typically the TreeViewItem itself, due to how it's bound in XAML.
            if (value is TreeViewItem item)
            {
                int level = 0;
                // Start checking from the parent of the current item to determine its level
                // The direct parent of the TreeViewItem's content is usually its HeaderPresenter,
                // so we need to traverse up the visual tree to find a parent TreeViewItem or TreeView.
                DependencyObject parent = VisualTreeHelper.GetParent(item);

                // Loop until we reach the TreeView itself or no parent is found
                while (parent != null && !(parent is TreeView))
                {
                    if (parent is TreeViewItem)
                    {
                        level++; // Increment level for each TreeViewItem parent found
                    }
                    parent = VisualTreeHelper.GetParent(parent);
                }

                // If level is greater than 0, it means it's a child item, so show indentation.
                return (level > 0) ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed; // Default to collapsed if not a TreeViewItem
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException(); // Not used for this converter
        }
    }
}