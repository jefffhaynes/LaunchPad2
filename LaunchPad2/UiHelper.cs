using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace LaunchPad2
{
    public static class UiHelper
    {
        public static T FindAncestor<T>(DependencyObject reference) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(reference);

            while (parent != null)
            {
                if (parent is T)
                    return parent as T;

                parent = VisualTreeHelper.GetParent(parent);
            }

            return null;
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject reference) where T : DependencyObject
        {
            if (reference != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(reference); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(reference, i);
                    if (child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
    }
}
