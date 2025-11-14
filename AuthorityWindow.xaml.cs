using IMS.Data;
using IMS.Data.Authority;
using IMS.Models.AuthorityModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace IMS
{
    /// <summary>
    /// Interaction logic for AuthorityWindow.xaml
    /// </summary>
    public partial class AuthorityWindow : System.Windows.Window
    {
        // ----- Authority -----

        // Users
        private UserRepository userRepo = new UserRepository();
        // ----- Observable Collections for WF Groups -----
        public ObservableCollection<string> AllUsersWFGroup { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> AddedUsersWFGroup { get; set; } = new ObservableCollection<string>();


    // 🧠 Store original positions (username → index)
    private Dictionary<string, int> _originalUserPositions = new Dictionary<string, int>();

        public AuthorityWindow()
        {
            InitializeComponent();

            // ----- Authority -----

            // Users
            LoadUsers();

            // Functional Security
            LoadFunctionalSecurityGroups();

            // Cab Security
            LoadDocumentSecurityGroups();

            // Bind data to WF Groups ListBoxes
            WFGroupsAllUsersListBox.ItemsSource = AllUsersWFGroup;
            ScrollWFGAllUsersListBox.ItemsSource = AddedUsersWFGroup;

            // Bind ListBoxes FN
            FunctionalSecurityAllUsers.ItemsSource = AllUsersWFGroup;
            ScrollAllUsersListBox.ItemsSource = AddedUsersWFGroup;
            
            CabSecurityAllUsers.ItemsSource = AllUsersWFGroup;
            ScrollDocumentUsersAllListBoxs.ItemsSource = AddedUsersWFGroup;
            
            // (Optional) Set DataContext if you want to use bindings elsewhere
            DataContext = this;
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

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem clickedMenuItem = sender as MenuItem;
            if (clickedMenuItem != null)
            {
                // First, hide all panels
                UsersDiv.Visibility = Visibility.Collapsed;
                FunctionalSecurityDiv.Visibility = Visibility.Collapsed;
                DocumentSecurityGroupDiv.Visibility = Visibility.Collapsed;
                WfGroupsDiv.Visibility = Visibility.Collapsed;

                // Then, show the relevant panel based on the clicked menu item's header
                switch (clickedMenuItem.Header.ToString())
                {
                    case "Users":
                        ShowPanel(UsersDiv);
                        break;
                    case "Functional Security":
                        ShowPanel(FunctionalSecurityDiv);
                        break;
                    case "Cab Security": // This maps to DocumentSecurityGroupDiv as per your description
                        ShowPanel(DocumentSecurityGroupDiv);
                        break;
                    case "WF Groups":
                        ShowPanel(WfGroupsDiv);
                        break;
                }
            }
        }

        // Helper method to make a panel visible
        private void ShowPanel(Grid panelToShow)
        {
            panelToShow.Visibility = Visibility.Visible;
        }

        // --------------- Authority --------------- 

        // Users


        private void LoadUsers()
        {
            var usernames = userRepo.GetAllUsernames()
                                    .OrderBy(u => u) // ensure ascending order
                                    .ToList();

            AllUsersWFGroup.Clear();
            _originalUserPositions.Clear();

            for (int i = 0; i < usernames.Count; i++)
            {
                AllUsersWFGroup.Add(usernames[i]);
                _originalUserPositions[usernames[i]] = i; // store original index
            }

            // Bind same for other listboxes
            UsersAvailableListBox.ItemsSource = usernames;
            FunctionalSecurityAllUsers.ItemsSource = usernames;
            CabSecurityAllUsers.ItemsSource = usernames;
        }

        //private void LoadUsers()
        //{
        //    var usernames = userRepo.GetAllUsernames();
        //    UsersAvailableListBox.ItemsSource = usernames;
        //    FunctionalSecurityAllUsers.ItemsSource = usernames;
        //    CabSecurityAllUsers.ItemsSource = usernames;
        //    // WF groups tab (ObservableCollection binding)
        //    AllUsersWFGroup.Clear();
        //    foreach (var user in usernames)
        //    {
        //        AllUsersWFGroup.Add(user);
        //    }
        //    //WFGroupsAllUsersListBox.ItemsSource = usernames;
        //}

        private void UsersAvailableListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UsersAvailableListBox.SelectedItem == null)
                return;

            string selectedUserName = UsersAvailableListBox.SelectedItem.ToString();

            var userDetails = userRepo.GetUserDetailsByUserName(selectedUserName);

            if (userDetails != null)
            {
                UserIDTextBox.Text = userDetails.UserID.ToString();
                NameTextBox.Text = userDetails.UserName;
                LongnameTextBox.Text = userDetails.UserLongName;
                EmailTextBox.Text = userDetails.UserEmail;
                PasswordBox.Password = userDetails.UserPassword; 
                ManagerTextBox.Text = userDetails.Manager;
                DeptTextBox.Text = userDetails.Department;
                SubDeptTextBox.Text = userDetails.SubDept;
                ExtraInfoTextBox.Text = userDetails.ExtraInfo;

                if (userDetails.UserType == 0)
                {
                    rdoWindowsUser.IsChecked = true;
                    rdoActive.IsChecked = false;
                }

                if (userDetails.UserType == 1)
                {
                    rdoWindowsUser.IsChecked = false;
                    rdoActive.IsChecked = true;
                }
            }
        }

        // Functional Security

        private void LoadFunctionalSecurityGroups()
        {
            FunctionalSecurity security = new FunctionalSecurity();
            List<FunctionalSecurityGroups> groups = security.GetFunctionalSecurityGroups();

            comboFunctionalSecurityGroups.ItemsSource = groups;
            comboRelateFunctionalGroups.ItemsSource = groups;
        }

        // Cab Security

        private void LoadDocumentSecurityGroups()
        {
            CabSecurity cabSecurity = new CabSecurity();
            List<FunctionalSecurityGroups> groups = cabSecurity.GetDocumentSecurityGroups();

            comboDocumentSecurityGroups.ItemsSource = groups;
        }

        private void MoveToAddedUsers(object sender, RoutedEventArgs e)
        {
            var selectedItems = WFGroupsAllUsersListBox.SelectedItems.Cast<string>().ToList();
            foreach (var user in selectedItems)
            {
                if (!AddedUsersWFGroup.Contains(user))
                    AddedUsersWFGroup.Add(user);

                AllUsersWFGroup.Remove(user);
            }
        }

        private void MoveAllToAddedUsers(object sender, RoutedEventArgs e)
        {
            foreach (var user in AllUsersWFGroup.ToList())
            {
                if (!AddedUsersWFGroup.Contains(user))
                    AddedUsersWFGroup.Add(user);
            }
            AllUsersWFGroup.Clear();
        }

        // "<" move selected users back to their original position
        private void MoveToAllUsers(object sender, RoutedEventArgs e)
        {
            var selectedItems = ScrollWFGAllUsersListBox.SelectedItems.Cast<string>().ToList();

            foreach (var user in selectedItems)
            {
                if (AllUsersWFGroup.Contains(user))
                    continue;

                // 🧠 Find original index position
                if (_originalUserPositions.TryGetValue(user, out int originalIndex))
                {
                    // Insert back in the correct order (based on original index)
                    if (originalIndex >= AllUsersWFGroup.Count)
                        AllUsersWFGroup.Add(user);
                    else
                        AllUsersWFGroup.Insert(originalIndex, user);
                }
                else
                {
                    // fallback (if not found, just add at bottom)
                    AllUsersWFGroup.Add(user);
                }

                AddedUsersWFGroup.Remove(user);
            }
        }

        // "<<" move all users back and restore full original order
        private void MoveAllToAllUsers(object sender, RoutedEventArgs e)
        {
            foreach (var user in AddedUsersWFGroup.ToList())
            {
                if (!AllUsersWFGroup.Contains(user))
                    AllUsersWFGroup.Add(user);
            }
            AddedUsersWFGroup.Clear();

            // Sort again according to original order
            var sorted = _originalUserPositions
                .OrderBy(x => x.Value)
                .Select(x => x.Key)
                .Where(u => AllUsersWFGroup.Contains(u))
                .ToList();

            AllUsersWFGroup.Clear();
            foreach (var user in sorted)
                AllUsersWFGroup.Add(user);
        }


        // Move selected items from All Users to Added Users

        private void btnMoveOneRight_Click(object sender, RoutedEventArgs e)
        {
            var selectedfunctional = FunctionalSecurityAllUsers.SelectedItems.Cast<string>().ToList();
            var selectedcab = CabSecurityAllUsers.SelectedItems.Cast<string>().ToList();
            foreach (var user in selectedfunctional)
            {
                if (AllUsersWFGroup.Contains(user)) AllUsersWFGroup.Remove(user);
                if (!AddedUsersWFGroup.Contains(user)) AddedUsersWFGroup.Add(user);
            }
            foreach (var user in selectedcab)
            {
                if (AllUsersWFGroup.Contains(user)) AllUsersWFGroup.Remove(user);
                if (!AddedUsersWFGroup.Contains(user)) AddedUsersWFGroup.Add(user);
            }
        }


        private void btnMoveAllRight_Click(object sender, RoutedEventArgs e)
        {
            foreach (var user in AllUsersWFGroup.ToList())
            {
                if (!AddedUsersWFGroup.Contains(user)) AddedUsersWFGroup.Add(user);
            }
            AllUsersWFGroup.Clear();
        }

        private void btnMoveOneLeft_Click(object sender, RoutedEventArgs e)
        {
            var selectedfunctional = ScrollAllUsersListBox.SelectedItems.Cast<string>().ToList();
            var selectedcab = ScrollDocumentUsersAllListBoxs.SelectedItems.Cast<string>().ToList();
            foreach (var user in selectedfunctional)
            {
                if (AddedUsersWFGroup.Contains(user)) AddedUsersWFGroup.Remove(user);

                if (!_originalUserPositions.ContainsKey(user))
                {
                    AllUsersWFGroup.Add(user);
                }
                else
                {
                    int idx = _originalUserPositions[user];
                    if (idx >= AllUsersWFGroup.Count)
                        AllUsersWFGroup.Add(user);
                    else
                        AllUsersWFGroup.Insert(idx, user);
                }
            }
            foreach (var user in selectedcab)
            {
                if (AddedUsersWFGroup.Contains(user)) AddedUsersWFGroup.Remove(user);

                if (!_originalUserPositions.ContainsKey(user))
                {
                    AllUsersWFGroup.Add(user);
                }
                else
                {
                    int idx = _originalUserPositions[user];
                    if (idx >= AllUsersWFGroup.Count)
                        AllUsersWFGroup.Add(user);
                    else
                        AllUsersWFGroup.Insert(idx, user);
                }
            }
        }

        //private void btnMoveAllLeft_Click(object sender, RoutedEventArgs e)
        //{
        //    foreach (var user in AddedUsersWFGroup.ToList())
        //    {
        //        if (!_originalUserPositions.ContainsKey(user))
        //            AllUsersWFGroup.Add(user);
        //        else
        //            AllUsersWFGroup.Insert(_originalUserPositions[user], user);
        //    }
        //    AddedUsersWFGroup.Clear();

        //    // Optional: reorder all users
        //    var sorted = _originalUserPositions.OrderBy(x => x.Value).Select(x => x.Key).Where(AllUsersWFGroup.Contains).ToList();
        //    AllUsersWFGroup.Clear();
        //    foreach (var u in sorted)
        //        AllUsersWFGroup.Add(u);
        //}
        private void btnMoveAllLeft_Click(object sender, RoutedEventArgs e)
        {
            // Move all users from AddedUsersWFGroup back to AllUsersWFGroup
            foreach (var user in AddedUsersWFGroup.ToList())
            {
                AllUsersWFGroup.Add(user); // Add first, regardless of original position
            }

            AddedUsersWFGroup.Clear();

            // Reorder AllUsersWFGroup according to original positions
            var sorted = _originalUserPositions
                            .OrderBy(x => x.Value)
                            .Select(x => x.Key)
                            .Where(AllUsersWFGroup.Contains)
                            .ToList();

            AllUsersWFGroup.Clear();
            foreach (var user in sorted)
                AllUsersWFGroup.Add(user);
        }

    }
}
