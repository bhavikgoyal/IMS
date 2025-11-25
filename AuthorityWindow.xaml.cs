using IMS.Data;
using IMS.Data.Authority;
using IMS.Models.AuthorityModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
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

        private UserRepository userRepo = new UserRepository();
        private FunctionalSecurity funcRepo = new FunctionalSecurity();
        private CabSecurity cabsecurity = new CabSecurity();
        public ObservableCollection<string> AllUsersWFGroup { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> AddedUsersWFGroup { get; set; } = new ObservableCollection<string>();

        private string CurrentDSGroupName = string.Empty;
        private int CurrentDSGroupID = 0;
        private int NewGroupIDCreated = -1;
        private string PrgLng = "E";
        private string CurrentFunctionalGroupName = string.Empty;
        private int CurrentFunctionalGroupID = 0;
        private int NewFunctionalGroupIDCreated = -1;

        private Dictionary<string, int> _originalUserPositions = new Dictionary<string, int>();

        public AuthorityWindow()
        {
            InitializeComponent();

            WFGroupsAllUsersListBox.ItemsSource = AllUsersWFGroup;
            ScrollWFGAllUsersListBox.ItemsSource = AddedUsersWFGroup;

            FunctionalSecurityAllUsers.ItemsSource = AllUsersWFGroup;
            ScrollAllUsersListBox.ItemsSource = AddedUsersWFGroup;

            CabSecurityAllUsers.ItemsSource = AllUsersWFGroup;
            ScrollDocumentUsersAllListBoxs.ItemsSource = AddedUsersWFGroup;

            DataContext = this;

            LoadUsers();

            LoadFunctionalSecurityGroups();

            LoadDocumentSecurityGroups();
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

        private void ShowPanel(Grid panelToShow)
        {
            panelToShow.Visibility = Visibility.Visible;
        }

        private void LoadUsers()
        {
            var usernames = userRepo.GetAllUsernames()
                                    .OrderBy(u => u)
                                    .ToList();

            AllUsersWFGroup.Clear();
            _originalUserPositions.Clear();

            for (int i = 0; i < usernames.Count; i++)
            {
                AllUsersWFGroup.Add(usernames[i]);
                _originalUserPositions[usernames[i]] = i;
            }
            UsersAvailableListBox.ItemsSource = AllUsersWFGroup;
            //UsersAvailableListBox.ItemsSource = usernames;
            //FunctionalSecurityAllUsers.ItemsSource = usernames;
            //CabSecurityAllUsers.ItemsSource = usernames;
        }

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
            List<FunctionalSecurityGroups> groups = funcRepo.GetFunctionalSecurityGroups();
            comboFunctionalSecurityGroups.ItemsSource = groups;
            comboRelateFunctionalGroups.ItemsSource = groups;
        }

        // Cab Security

        private void LoadDocumentSecurityGroups()
        {
            List<FunctionalSecurityGroups> groups = cabsecurity.GetDocumentSecurityGroups();
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

                if (_originalUserPositions.TryGetValue(user, out int originalIndex))
                {
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
        private void btnMoveAllLeft_Click(object sender, RoutedEventArgs e)
        {
            foreach (var user in AddedUsersWFGroup.ToList())
            {
                AllUsersWFGroup.Add(user);
            }

            AddedUsersWFGroup.Clear();

            var sorted = _originalUserPositions
                            .OrderBy(x => x.Value)
                            .Select(x => x.Key)
                            .Where(AllUsersWFGroup.Contains)
                            .ToList();

            AllUsersWFGroup.Clear();
            foreach (var user in sorted)
                AllUsersWFGroup.Add(user);
        }

        private void comboDocumentSecurityGroups_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (comboDocumentSecurityGroups.ItemsSource == null)
                    return;

                if (comboDocumentSecurityGroups.SelectedItem is FunctionalSecurityGroups sel)
                {
                    CurrentDSGroupID = sel.GroupID;
                    CurrentDSGroupName = sel.DisplayText ?? sel.GroupName;
                }
                else
                {
                    CurrentDSGroupName = (comboDocumentSecurityGroups.Text ?? string.Empty).Trim();
                    CurrentDSGroupID = ExtractGroupIdFromDisplayText(CurrentDSGroupName);
                    if (CurrentDSGroupID == 0)
                        CurrentDSGroupID = NewGroupIDCreated;
                }

                if (string.IsNullOrWhiteSpace(CurrentDSGroupName) || CurrentDSGroupID <= 0)
                    return;

                bool exists = false;
                try { exists = cabsecurity.DoesDocumentGroupExist(GetGroupNameOnly(CurrentDSGroupName)); } catch { exists = false; }

                if (exists)
                {
                    if (CurrentDSGroupID <= 0)
                        CurrentDSGroupID = cabsecurity.GetDocumentGroupIdByName(GetGroupNameOnly(CurrentDSGroupName));

                    if (CurrentDSGroupID > 0)
                    {
                        LoadDSGroupUsersAndUsersNotInGroup(CurrentDSGroupID);
                        LoadDSArchives(CurrentDSGroupID);
                    }
                    else
                    {
                        LoadRemDSAllArchives();
                    }
                    return;
                }

                CreateTheGroupFlow();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading document security group: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private int ExtractGroupIdFromDisplayText(string display)
        {
            if (string.IsNullOrWhiteSpace(display)) return 0;
            try
            {
                int eq = display.LastIndexOf('=');
                if (eq < 0) return 0;
                int close = display.IndexOf(')', eq);
                string part = close > eq ? display.Substring(eq + 1, close - eq - 1) : display.Substring(eq + 1);
                if (int.TryParse(part, out int id)) return id;
            }
            catch { }
            return 0;
        }

        private void CreateTheGroupFlow()
        {
            string prompt = PrgLng == "E"
                ? $"Create New Group Named {CurrentDSGroupName}?"
                : $"ÎáÞ ãÌãæÚÉ ÌÏíÏÉ ÈÇÓã {CurrentDSGroupName}?";

            var result = MessageBox.Show(prompt, "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                CreateNewDSGroup(CurrentDSGroupName);
                LoadDocumentSecurityGroups();
                comboDocumentSecurityGroups.Text = CurrentDSGroupName;
                // Call selection handler to load newly created group
                comboDocumentSecurityGroups_SelectionChanged(null, null);
            }
        }
        private void CreateNewDSGroup(string groupName)
        {
            try
            {
                NewGroupIDCreated = cabsecurity.CreateDocumentGroup(groupName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create group: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetGroupNameOnly(string display)
        {
            if (string.IsNullOrWhiteSpace(display)) return display;
            int p = display.IndexOf('(');
            if (p > 0) return display.Substring(0, p).Trim();
            return display.Trim();
        }

        private void LoadDSGroupUsersAndUsersNotInGroup(int groupId)
        {
            try
            {
                var allUsers = userRepo.GetAllUsernames()?.OrderBy(u => u).ToList() ?? new List<string>();

                var addedUsers = cabsecurity.GetUsersForDocumentGroup(groupId) ?? new List<string>();

                var notAddedUsers = allUsers.Except(addedUsers, StringComparer.OrdinalIgnoreCase).OrderBy(u => u).ToList();
                AllUsersWFGroup.Clear();
                foreach (var u in notAddedUsers)
                    AllUsersWFGroup.Add(u);

                AddedUsersWFGroup.Clear();
                foreach (var u in addedUsers)
                    AddedUsersWFGroup.Add(u);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading group users: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDSArchives(int groupId)
        {
            try
            {
                SearchFilterAllUsers.Items.Clear();
                ScrollAllUsersListBoxs.Items.Clear();

                var groupArchives = cabsecurity.GetArchivesForGroup(groupId) ?? new List<(string ShortIndexName, string LongIndexName)>();

                var allIndexes = cabsecurity.GetAllActiveIndexes() ?? new List<(string ShortIndexName, string LongIndexName)>();

                foreach (var a in groupArchives)
                {
                    ScrollAllUsersListBoxs.Items.Add($"{a.LongIndexName}@{a.ShortIndexName}");
                }

                var assignedShorts = new HashSet<string>(groupArchives.Select(x => x.ShortIndexName), StringComparer.OrdinalIgnoreCase);

                foreach (var idx in allIndexes)
                {
                    if (!assignedShorts.Contains(idx.ShortIndexName))
                    {
                        SearchFilterAllUsers.Items.Add($"{idx.LongIndexName}@{idx.ShortIndexName}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading archives: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void LoadRemDSAllArchives()
        {
            try
            {
                SearchFilterAllUsers.Items.Clear();

                var allIndexes = cabsecurity.GetAllActiveIndexes() ?? new List<(string ShortIndexName, string LongIndexName)>();

                foreach (var idx in allIndexes)
                {
                    SearchFilterAllUsers.Items.Add($"{idx.LongIndexName}@{idx.ShortIndexName}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading all archives: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnLoadCreateGroup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (comboDocumentSecurityGroups.ItemsSource == null)
                    return;

                if (comboDocumentSecurityGroups.SelectedItem is FunctionalSecurityGroups sel)
                {
                    CurrentDSGroupID = sel.GroupID;
                    CurrentDSGroupName = sel.DisplayText ?? sel.GroupName;

                    if (CurrentDSGroupID <= 0)
                    {
                        CurrentDSGroupID = cabsecurity.GetDocumentGroupIdByName(GetGroupNameOnly(CurrentDSGroupName));
                    }

                    if (CurrentDSGroupID > 0)
                    {
                        LoadDSGroupUsersAndUsersNotInGroup(CurrentDSGroupID);
                        LoadDSArchives(CurrentDSGroupID);
                        MessageBox.Show($"Group '{GetGroupNameOnly(CurrentDSGroupName)}' loaded.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Selected group could not be found.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    return;
                }

                var text = (comboDocumentSecurityGroups.Text ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(text))
                {
                    MessageBox.Show("Please select or type a group name.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                CurrentDSGroupName = text;
                CreateTheGroupFlow();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Load/Create failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDeleteGroup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (comboDocumentSecurityGroups.SelectedItem is FunctionalSecurityGroups sel)
                {
                    int gid = sel.GroupID;
                    string name = sel.DisplayText ?? sel.GroupName;

                    if (gid <= 0)
                    {
                        gid = cabsecurity.GetDocumentGroupIdByName(GetGroupNameOnly(name));
                    }

                    if (gid <= 0)
                    {
                        MessageBox.Show("Selected group has no valid ID to delete.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var res = MessageBox.Show($"Delete group '{GetGroupNameOnly(name)}' ? This will remove group and its assignments.", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (res == MessageBoxResult.Yes)
                    {
                        bool ok = false;
                        try
                        {
                            ok = cabsecurity.DeleteDocumentGroup(gid);
                        }
                        catch (Exception ex) { MessageBox.Show($"Delete failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }

                        if (ok)
                        {
                            MessageBox.Show("Group deleted.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadDocumentSecurityGroups();
                            AllUsersWFGroup.Clear();
                            AddedUsersWFGroup.Clear();
                            SearchFilterAllUsers.Items.Clear();
                            ScrollAllUsersListBoxs.Items.Clear();
                            comboDocumentSecurityGroups.Text = string.Empty;
                        }
                        else
                        {
                            MessageBox.Show("Failed to delete group.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Please select a group to delete.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting group: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(CurrentDSGroupName))
                {
                    if (comboDocumentSecurityGroups.SelectedItem is FunctionalSecurityGroups s)
                    {
                        CurrentDSGroupName = s.DisplayText ?? s.GroupName;
                        CurrentDSGroupID = s.GroupID;
                    }
                    else
                    {
                        CurrentDSGroupName = (comboDocumentSecurityGroups.Text ?? string.Empty).Trim();
                        CurrentDSGroupID = ExtractGroupIdFromDisplayText(CurrentDSGroupName);
                    }
                }

                if (string.IsNullOrWhiteSpace(CurrentDSGroupName))
                {
                    MessageBox.Show("No document group selected/typed to save.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (CurrentDSGroupID <= 0)
                {
                    if (cabsecurity.DoesDocumentGroupExist(GetGroupNameOnly(CurrentDSGroupName)))
                    {
                        CurrentDSGroupID = cabsecurity.GetDocumentGroupIdByName(GetGroupNameOnly(CurrentDSGroupName));
                    }
                    else
                    {
                        var result = MessageBox.Show($"Group '{GetGroupNameOnly(CurrentDSGroupName)}' does not exist. Create it now?", "Create Group", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.Yes)
                        {
                            CreateNewDSGroup(CurrentDSGroupName);
                            CurrentDSGroupID = NewGroupIDCreated;
                        }
                    }
                }

                if (CurrentDSGroupID <= 0)
                {
                    MessageBox.Show("Cannot save without a valid group id.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var usersToSave = AddedUsersWFGroup.ToList();
                bool usersSaved = false;
                try
                {
                    usersSaved = cabsecurity.SetUsersForDocumentGroup(CurrentDSGroupID, usersToSave);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Save users failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    usersSaved = false;
                }

                bool archivesSaved = true;
                try
                {
                    var shortNames = ScrollAllUsersListBoxs.Items
                                        .Cast<object>()
                                        .Select(i => i?.ToString() ?? string.Empty)
                                        .Select(s =>
                                        {
                                            var parts = s.Split('@');
                                            return parts.Length > 1 ? parts[1].Trim() : s.Trim();
                                        })
                                        .Where(sn => !string.IsNullOrWhiteSpace(sn))
                                        .Distinct(StringComparer.OrdinalIgnoreCase)
                                        .ToList();

                    archivesSaved = cabsecurity.SetArchivesForDocumentGroup(CurrentDSGroupID, shortNames);
                }
                catch (Exception ex)
                {
                    archivesSaved = false;
                    MessageBox.Show($"Save archives failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                if (usersSaved && archivesSaved)
                {
                    MessageBox.Show("Changes saved.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);

                    LoadDSGroupUsersAndUsersNotInGroup(CurrentDSGroupID);
                    LoadDSArchives(CurrentDSGroupID);
                }
                else if (!usersSaved && !archivesSaved)
                {
                    MessageBox.Show("Failed to save users and archives.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (!usersSaved)
                {
                    MessageBox.Show("Failed to save group users.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (archivesSaved) LoadDSArchives(CurrentDSGroupID);
                }
                else
                {
                    MessageBox.Show("Users saved but failed to save archives.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    LoadDSGroupUsersAndUsersNotInGroup(CurrentDSGroupID);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Apply error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void btnDSMoveOneRight_Click(object sender, RoutedEventArgs e)
        {
            var selected = SearchFilterAllUsers.SelectedItems.Cast<object>()
                    .Select(i => i.ToString()).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            if (selected.Count == 0) return;

            foreach (var item in selected)
            {
                bool exists = ScrollAllUsersListBoxs.Items.Cast<object>().Any(x => string.Equals(x.ToString(), item, StringComparison.OrdinalIgnoreCase));
                if (!exists)
                    ScrollAllUsersListBoxs.Items.Add(item);
                SearchFilterAllUsers.Items.Remove(item);
            }
        }

        private void btnDSMoveAllRight_Click(object sender, RoutedEventArgs e)
        {
            var allLeft = SearchFilterAllUsers.Items.Cast<object>().Select(i => i.ToString()).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            if (allLeft.Count == 0) return;

            foreach (var item in allLeft)
            {
                bool exists = ScrollAllUsersListBoxs.Items.Cast<object>().Any(x => string.Equals(x.ToString(), item, StringComparison.OrdinalIgnoreCase));
                if (!exists)
                    ScrollAllUsersListBoxs.Items.Add(item);
            }
            SearchFilterAllUsers.Items.Clear();
        }

        private void btnDSMoveOneLeft_Click(object sender, RoutedEventArgs e)
        {
            var selected = ScrollAllUsersListBoxs.SelectedItems.Cast<object>()
                    .Select(i => i.ToString()).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            if (selected.Count == 0) return;

            foreach (var item in selected)
            {
                bool exists = SearchFilterAllUsers.Items.Cast<object>().Any(x => string.Equals(x.ToString(), item, StringComparison.OrdinalIgnoreCase));
                if (!exists)
                    SearchFilterAllUsers.Items.Add(item);
                ScrollAllUsersListBoxs.Items.Remove(item);
            }

            SortListBoxItems(SearchFilterAllUsers);
        }

        private void btnDSMoveAllLeft_Click(object sender, RoutedEventArgs e)
        {
            var allRight = ScrollAllUsersListBoxs.Items.Cast<object>().Select(i => i.ToString()).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            if (allRight.Count == 0) return;

            foreach (var item in allRight)
            {
                bool exists = SearchFilterAllUsers.Items.Cast<object>().Any(x => string.Equals(x.ToString(), item, StringComparison.OrdinalIgnoreCase));
                if (!exists)
                    SearchFilterAllUsers.Items.Add(item);
            }
            ScrollAllUsersListBoxs.Items.Clear();

            SortListBoxItems(SearchFilterAllUsers);
        }
        private void SortListBoxItems(ListBox listBox)
        {
            var items = listBox.Items.Cast<object>().Select(i => i.ToString()).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            items.Sort(StringComparer.OrdinalIgnoreCase);
            listBox.Items.Clear();
            foreach (var it in items) listBox.Items.Add(it);
        }

        private void comboFunctionalSecurityGroups_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (comboFunctionalSecurityGroups.ItemsSource == null) return;

                if (comboFunctionalSecurityGroups.SelectedItem is FunctionalSecurityGroups sel)
                {
                    CurrentFunctionalGroupID = sel.GroupID;
                    CurrentFunctionalGroupName = sel.DisplayText ?? sel.GroupName;
                }
                else
                {
                    CurrentFunctionalGroupName = (comboFunctionalSecurityGroups.Text ?? string.Empty).Trim();
                    CurrentFunctionalGroupID = 0;
                }

                if (CurrentFunctionalGroupID <= 0)
                {
                    CurrentFunctionalGroupID = new FunctionalSecurity().GetFunctionalGroupIdByName(CurrentFunctionalGroupName);
                }

                if (CurrentFunctionalGroupID <= 0)
                {
                    AllUsersWFGroup.Clear();
                    AddedUsersWFGroup.Clear();
                    ClearFunctionalRightsCheckboxes();
                    return;
                }

                LoadFunctionalGroupUsersAndUsersNotInGroup(CurrentFunctionalGroupID);
                LoadFunctionalGroupRights(CurrentFunctionalGroupID);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading functional group: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void LoadFunctionalGroupUsersAndUsersNotInGroup(int groupId)
        {
            try
            {
                var allUsers = userRepo.GetAllUsernames()?.OrderBy(u => u).ToList() ?? new List<string>();
                var addedUsers = funcRepo.GetUsersForFunctionalGroup(groupId) ?? new List<string>();

                var notAdded = allUsers.Except(addedUsers, StringComparer.OrdinalIgnoreCase).OrderBy(u => u).ToList();

                AllUsersWFGroup.Clear();
                foreach (var u in notAdded) AllUsersWFGroup.Add(u);

                AddedUsersWFGroup.Clear();
                foreach (var u in addedUsers) AddedUsersWFGroup.Add(u);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading users for functional group: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void LoadFunctionalGroupRights(int groupId)
        {
            try
            {
                var rights = funcRepo.GetFunctionalRights(groupId);

                bool G(string key) => rights.TryGetValue(key, out bool v) && v;

                chkCanAdmin.IsChecked = G("CanAdmin");
                chkCanAnnotate.IsChecked = G("CanAnnotate");
                chkCanApprove.IsChecked = G("CanApprove");
                chkCanApproveAll.IsChecked = G("CanApproveAll");
                chkCanAudit.IsChecked = G("CanAudit");
                chkCanChangePassword.IsChecked = G("CanChangePassword");
                chkCanChangeScanner.IsChecked = G("CanChangeScanner");
                chkCanCheckOutDocs.IsChecked = G("CanCheckOut");
                chkCanCreateNewVers.IsChecked = G("CanCreateNewVersion");
                chkCanDeleteFiles.IsChecked = G("CanDeleteFiles");
                chkCanDeleteDoc.IsChecked = G("CanDeleteDoc");
                chkCanDeleteDocs.IsChecked = G("CanDeleteDocs");
                chkCanEditDocument.IsChecked = G("CanEditDocument");
                chkCanEditUsers.IsChecked = G("CanEditUsers");
                chkCanEmail.IsChecked = G("CanEmail");
                chkCanExport.IsChecked = G("CanExport");
                chkCanExportBatch.IsChecked = G("CanExportBatch");
                chkCanImportBatch.IsChecked = G("CanImportBatch");
                chkCanIntegrate.IsChecked = G("CanIntegrate");
                chkCanMerge.IsChecked = G("CanMerge");
                chkCanORC.IsChecked = G("CanORC");
                chkCanPrint.IsChecked = G("CanPrint");
                chkCanPrintResultList.IsChecked = G("CanPrintResultList");
                chkCanRerout.IsChecked = G("CanRerout");
                chkCanRestoreFiles.IsChecked = G("CanRestoreFiles");
                chkCanSave.IsChecked = G("CanSave");
                chkCanSearch.IsChecked = G("CanSearch");
                chkCanSignStamp.IsChecked = G("CanSignStamp");
                chkCanSplit.IsChecked = G("CanSplit");
                chkCanTakeCopy.IsChecked = G("CanTakeCopy");
                chkCanUpdateInbox.IsChecked = G("CanUpdateInbox");
                chkCanViewCharts.IsChecked = G("CanViewCharts");
                chkCanViewEncrypted.IsChecked = G("CanViewEncrypted");
                chkCanViewTaxonomy.IsChecked = G("CanViewTaxonomy");

                chkCanScan.IsChecked = G("CanScan");
                chkCanChangeScnrSett.IsChecked = G("CanChangeScannerSettings") || G("CanChangeScnrSett");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading functional group rights: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearFunctionalRightsCheckboxes()
        {
            chkCanAdmin.IsChecked = false;
            chkCanAnnotate.IsChecked = false;
            chkCanApprove.IsChecked = false;
            chkCanApproveAll.IsChecked = false;
            chkCanAudit.IsChecked = false;
            chkCanChangePassword.IsChecked = false;
            chkCanChangeScanner.IsChecked = false;
            chkCanCheckOutDocs.IsChecked = false;
            chkCanCreateNewVers.IsChecked = false;
            chkCanDeleteFiles.IsChecked = false;
            chkCanDeleteDoc.IsChecked = false;
            chkCanDeleteDocs.IsChecked = false;
            chkCanEditDocument.IsChecked = false;
            chkCanEditUsers.IsChecked = false;
            chkCanEmail.IsChecked = false;
            chkCanExport.IsChecked = false;
            chkCanExportBatch.IsChecked = false;
            chkCanImportBatch.IsChecked = false;
            chkCanIntegrate.IsChecked = false;
            chkCanMerge.IsChecked = false;
            chkCanORC.IsChecked = false;
            chkCanPrint.IsChecked = false;
            chkCanPrintResultList.IsChecked = false;
            chkCanRerout.IsChecked = false;
            chkCanRestoreFiles.IsChecked = false;
            chkCanSave.IsChecked = false;
            chkCanSearch.IsChecked = false;
            chkCanSignStamp.IsChecked = false;
            chkCanSplit.IsChecked = false;
            chkCanTakeCopy.IsChecked = false;
            chkCanUpdateInbox.IsChecked = false;
            chkCanViewCharts.IsChecked = false;
            chkCanViewEncrypted.IsChecked = false;
            chkCanViewTaxonomy.IsChecked = false;
            chkCanScan.IsChecked = false;
            chkCanChangeScnrSett.IsChecked = false;
        }

        private void btnApplyFunctional_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ensure we have a functional group id (resolve from selected combo or typed text)
                if (CurrentFunctionalGroupID <= 0)
                {
                    if (comboFunctionalSecurityGroups.SelectedItem is FunctionalSecurityGroups s)
                    {
                        CurrentFunctionalGroupID = s.GroupID;
                        CurrentFunctionalGroupName = s.DisplayText ?? s.GroupName;
                    }
                    else
                    {
                        CurrentFunctionalGroupName = (comboFunctionalSecurityGroups.Text ?? string.Empty).Trim();
                        if (!string.IsNullOrWhiteSpace(CurrentFunctionalGroupName))
                        {
                            CurrentFunctionalGroupID = funcRepo.GetFunctionalGroupIdByName(CurrentFunctionalGroupName);
                        }
                    }
                }

                if (CurrentFunctionalGroupID <= 0)
                {
                    MessageBox.Show("Please select or create a functional group before applying changes.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                List<string> usersToSave = AddedUsersWFGroup.ToList(); 

                bool usersSaved = false;
                try
                {
                    usersSaved = funcRepo.SaveFunctionalGroupUsers(CurrentFunctionalGroupID, usersToSave);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save functional group users.\n\nError: {ex.Message}\n\nDetails:\n{ex.ToString()}",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var rights = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
                {
                    ["CanAdmin"] = chkCanAdmin.IsChecked == true,
                    ["CanAnnotate"] = chkCanAnnotate.IsChecked == true,
                    ["CanApprove"] = chkCanApprove.IsChecked == true,
                    ["CanApproveAll"] = chkCanApproveAll.IsChecked == true,
                    ["CanAudit"] = chkCanAudit.IsChecked == true,
                    ["CanChangeLanguage"] = chkCanChangeLang.IsChecked == true,
                    ["CanChangePassword"] = chkCanChangePassword.IsChecked == true,
                    ["CanChangeScanner"] = chkCanChangeScanner.IsChecked == true,
                    ["CanChangeScannerSettings"] = chkCanChangeScnrSett.IsChecked == true,
                    ["CanChangeSettings"] = chkCanChangeSetting.IsChecked == true,
                    ["CanCheckOut"] = chkCanCheckOutDocs.IsChecked == true,
                    ["CanCreateNewVersion"] = chkCanCreateNewVers.IsChecked == true,
                    ["CanDeleteFiles"] = chkCanDeleteFiles.IsChecked == true,
                    ["CanDeleteDoc"] = chkCanDeleteDoc.IsChecked == true,
                    ["CanDeleteDocs"] = chkCanDeleteDocs.IsChecked == true,
                    ["CanEditDocument"] = chkCanEditDocument.IsChecked == true,
                    ["CanEditUsers"] = chkCanEditUsers.IsChecked == true,
                    ["CanEmail"] = chkCanEmail.IsChecked == true,
                    ["CanExport"] = chkCanExport.IsChecked == true,
                    ["CanExportBatch"] = chkCanExportBatch.IsChecked == true,
                    ["CanImportBatch"] = chkCanImportBatch.IsChecked == true,
                    ["CanIntegrate"] = chkCanIntegrate.IsChecked == true,
                    ["CanMerge"] = chkCanMerge.IsChecked == true,
                    ["CanORC"] = chkCanORC.IsChecked == true,
                    ["CanPrint"] = chkCanPrint.IsChecked == true,
                    ["CanPrintResultList"] = chkCanPrintResultList.IsChecked == true,
                    ["CanRerout"] = chkCanRerout.IsChecked == true,
                    ["CanRestoreFiles"] = chkCanRestoreFiles.IsChecked == true,
                    ["CanSave"] = chkCanSave.IsChecked == true,
                    ["CanSearch"] = chkCanSearch.IsChecked == true,
                    ["CanSignStamp"] = chkCanSignStamp.IsChecked == true,
                    ["CanSplit"] = chkCanSplit.IsChecked == true,
                    ["CanTakeCopy"] = chkCanTakeCopy.IsChecked == true,
                    ["CanUpdateInbox"] = chkCanUpdateInbox.IsChecked == true,
                    ["CanViewCharts"] = chkCanViewCharts.IsChecked == true,
                    ["CanViewEncrypted"] = chkCanViewEncrypted.IsChecked == true,
                    ["CanViewTaxonomy"] = chkCanViewTaxonomy.IsChecked == true,
                    ["CanScan"] = chkCanScan.IsChecked == true
                };

                // 3) Save rights using repository; show full exception details on failure
                bool rightsSaved = false;
                try
                {
                    rightsSaved = funcRepo.SaveFunctionalRights(CurrentFunctionalGroupID, rights);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save functional group rights.\n\nError: {ex.Message}\n\nDetails:\n{ex.ToString()}",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 4) Final feedback and reload UI
                if (usersSaved && rightsSaved)
                {
                    MessageBox.Show("Functional group users and rights saved successfully.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);

                    // reload fresh data for UI consistency
                    try
                    {
                        LoadFunctionalGroupUsersAndUsersNotInGroup(CurrentFunctionalGroupID);
                        LoadFunctionalGroupRights(CurrentFunctionalGroupID);
                    }
                    catch
                    {
                        // ignore reload exceptions here (we already saved successfully)
                    }
                }
                else if (!usersSaved && !rightsSaved)
                {
                    MessageBox.Show("Failed to save functional group users and rights.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (!usersSaved)
                {
                    MessageBox.Show("Failed to save functional group users.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else // !rightsSaved
                {
                    MessageBox.Show("Failed to save functional group rights.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                // show full outer exception if something unexpected happens
                MessageBox.Show($"Apply error: {ex.Message}\n\n{ex.ToString()}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnLoadCreateFunctional_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (comboFunctionalSecurityGroups.ItemsSource == null)
                    return;

                if (comboFunctionalSecurityGroups.SelectedItem is FunctionalSecurityGroups sel)
                {
                    CurrentFunctionalGroupID = sel.GroupID;
                    CurrentFunctionalGroupName = sel.DisplayText ?? sel.GroupName;

                    if (CurrentFunctionalGroupID <= 0)
                    {
                        CurrentFunctionalGroupID = funcRepo.GetFunctionalGroupIdByName(CurrentFunctionalGroupName);
                    }

                    if (CurrentFunctionalGroupID > 0)
                    {
                        // load users and rights for this group
                        LoadFunctionalGroupUsersAndUsersNotInGroup(CurrentFunctionalGroupID);
                        LoadFunctionalGroupRights(CurrentFunctionalGroupID);
                        MessageBox.Show($"Group '{CurrentFunctionalGroupName}' loaded.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Selected group could not be found.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    return;
                }

                var text = (comboFunctionalSecurityGroups.Text ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(text))
                {
                    MessageBox.Show("Please select or type a group name.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                CurrentFunctionalGroupName = text;
                // Ask to create
                var res = MessageBox.Show($"Create New Functional Group Named '{CurrentFunctionalGroupName}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                {
                    NewFunctionalGroupIDCreated = funcRepo.CreateFunctionalGroup(CurrentFunctionalGroupName);
                    if (NewFunctionalGroupIDCreated > 0)
                    {
                        // reload combobox
                        LoadFunctionalSecurityGroups();
                        comboFunctionalSecurityGroups.Text = CurrentFunctionalGroupName;
                        // trigger selection flow
                        comboFunctionalSecurityGroups_SelectionChanged(null, null);
                    }
                    else
                    {
                        MessageBox.Show("Failed to create functional group.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Load/Create functional group failed: {ex.Message}\n\n{ex.ToString()}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDeleteFunctional_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (comboFunctionalSecurityGroups.SelectedItem is FunctionalSecurityGroups sel)
                {
                    int gid = sel.GroupID;
                    string name = sel.DisplayText ?? sel.GroupName;

                    if (gid <= 0)
                    {
                        gid = funcRepo.GetFunctionalGroupIdByName(name);
                    }

                    if (gid <= 0)
                    {
                        MessageBox.Show("Selected group has no valid ID to delete.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var res = MessageBox.Show($"Delete functional group '{name}' ? This will remove group and its assignments.", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (res == MessageBoxResult.Yes)
                    {
                        bool ok = false;
                        try
                        {
                            ok = funcRepo.DeleteFunctionalGroup(gid);
                        }
                        catch (Exception ex) { MessageBox.Show($"Delete failed: {ex.Message}\n\n{ex.ToString()}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }

                        if (ok)
                        {
                            MessageBox.Show("Group deleted.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadFunctionalSecurityGroups();
                            AllUsersWFGroup.Clear();
                            AddedUsersWFGroup.Clear();
                            comboFunctionalSecurityGroups.Text = string.Empty;
                            // optionally clear rights UI
                            ResetFunctionalRightsUI();
                        }
                        else
                        {
                            MessageBox.Show("Failed to delete group.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Please select a group to delete.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting functional group: {ex.Message}\n\n{ex.ToString()}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ResetFunctionalRightsUI()
        {
            // Set all right checkboxes to false — list every checkbox you've placed
            chkCanScan.IsChecked = false;
            chkCanApprove.IsChecked = false;
            chkCanApproveAll.IsChecked = false;
            chkCanChangeScanner.IsChecked = false;
            chkCanEditDocument.IsChecked = false;
            chkCanEmail.IsChecked = false;
            chkCanAnnotate.IsChecked = false;
            chkCanSearch.IsChecked = false;
            chkCanAdmin.IsChecked = false;
            chkCanViewEncrypted.IsChecked = false;
            chkCanUpdateInbox.IsChecked = false;
            chkCanChangeSetting.IsChecked = false;
            chkCanExport.IsChecked = false;
            chkCanImportBatch.IsChecked = false;
            chkCanDeleteFiles.IsChecked = false;
        }
    }
}
