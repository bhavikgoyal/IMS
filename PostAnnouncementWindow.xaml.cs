using System.Windows;
using System.Windows.Controls; // Add this for ListBox
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace IMS
{
    public partial class PostAnnouncementWindow : Window
    {
        public PostAnnouncementWindow()
        {
            InitializeComponent();
            this.Loaded += PostAnnouncementWindow_Loaded;
        }

        private void PostAnnouncementWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Set initial states: UncheckButton true, CheckButton false
            if (UncheckButton != null)
                UncheckButton.IsChecked = true;
            if (CheckButton != null)
                CheckButton.IsChecked = false;

            // Apply initial selection based on UncheckButton being checked by default
            ApplySelectionToListBox(false); // Initially all unselected
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            var currentToggle = sender as ToggleButton;
            if (currentToggle == null) return;

            if (currentToggle == CheckButton && currentToggle.IsChecked == true)
            {
                if (UncheckButton != null && UncheckButton.IsChecked == true)
                {
                    UncheckButton.IsChecked = false;
                }
                //MessageBox.Show("Show Groups is Active (All Selected)");
                ApplySelectionToListBox(true); // Select all items
            }
            else if (currentToggle == UncheckButton && currentToggle.IsChecked == true)
            {
                if (CheckButton != null && CheckButton.IsChecked == true)
                {
                    CheckButton.IsChecked = false;
                }
                //MessageBox.Show("Hide Groups is Active (All Unselected)");
                ApplySelectionToListBox(false); // Deselect all items
            }
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            var currentToggle = sender as ToggleButton;
            if (currentToggle == null) return;

            if (currentToggle == CheckButton && currentToggle.IsChecked == false)
            {
                if (UncheckButton != null)
                {
                    UncheckButton.IsChecked = true;
                }
                ApplySelectionToListBox(false); // Deselect all if CheckButton is unchecked
            }
            else if (currentToggle == UncheckButton && currentToggle.IsChecked == false)
            {
                if (CheckButton != null)
                {
                    CheckButton.IsChecked = true;
                }
                ApplySelectionToListBox(true); // Select all if UncheckButton is unchecked
            }
        }

        // Helper method to apply selection to the ListBox
        private void ApplySelectionToListBox(bool selectAll)
        {
            if (Announcment == null) return; // Defensive check
            if (GroupsAnnouncment == null) return; // Defensive check

            // Change SelectionMode to Multiple if it's not already
            // This is important for selecting all items programmatically
            Announcment.SelectionMode = SelectionMode.Multiple;
            GroupsAnnouncment.SelectionMode = SelectionMode.Multiple;
            Announcment.UnselectAll(); // Start by clearing existing selection
            GroupsAnnouncment.UnselectAll();

            if (selectAll)
            {
                // Iterate through items and select them
                foreach (var item in Announcment.Items)
                {
                    // If your ListBox items are complex objects, you might need to find the ListBoxItem container
                    // For simple string items, calling SelectAll() is often easier
                    // Announcment.SelectedItems.Add(item); // Alternative for specific items
                    // Or, if items are ListBoxItem directly: ((ListBoxItem)Announcment.ItemContainerGenerator.ContainerFromItem(item)).IsSelected = true;
                }
                Announcment.SelectAll(); // Easier way to select all if SelectionMode is Multiple
                GroupsAnnouncment.SelectAll();
            }
            else
            {
                Announcment.UnselectAll(); // Deselect all items
                GroupsAnnouncment.UnselectAll();
            }
            // You might want to switch back to Single if that's the desired interactive mode
            // Or keep it as Multiple if users can select multiple items manually after toggling
        }

        private void GroupToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            // जब toggle ON (checked) हो → Groups दिखाओ, Announcment छिपाओ
            Announcment.Visibility = Visibility.Collapsed;
            GroupsAnnouncment.Visibility = Visibility.Visible;
        }

        private void GroupToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            // जब toggle OFF (unchecked) हो → Groups छिपाओ, Announcment दिखाओ
            Announcment.Visibility = Visibility.Visible;
            GroupsAnnouncment.Visibility = Visibility.Collapsed;
        }


    }
}