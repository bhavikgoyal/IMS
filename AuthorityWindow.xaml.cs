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

        private UserRepository userRepo = new UserRepository();
        public ObservableCollection<string> AllUsersWFGroup { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> AddedUsersWFGroup { get; set; } = new ObservableCollection<string>();


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
            var security = new FunctionalSecurity();
            List<FunctionalSecurityGroups> groups = security.GetFunctionalSecurityGroups();

            comboFunctionalSecurityGroups.ItemsSource = groups;
            comboRelateFunctionalGroups.ItemsSource = groups;
        }

        // Cab Security

        private void LoadDocumentSecurityGroups()
        {
            var cabSecurity = new CabSecurity();
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


        private string CurrentDSGroupName = string.Empty;
        private int CurrentDSGroupID = 0;
        private int NewGroupIDCreated = -1;
        private string PrgLng = "E";
        private int AuditEnabled = 0;
        private string CurrentModules = "";
        private string TheComputerName = Environment.MachineName;
        private string LOUName = Environment.UserName;


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

                // If exists load, else create flow (existing logic)
                bool exists = false;
                try { exists = cabRepo.DoesDocumentGroupExist(GetGroupNameOnly(CurrentDSGroupName)); } catch { exists = false; }

                if (exists)
                {
                    if (CurrentDSGroupID <= 0)
                        CurrentDSGroupID = cabRepo.GetDocumentGroupIdByName(GetGroupNameOnly(CurrentDSGroupName));

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
        private readonly CabSecurity cabRepo = new CabSecurity();

        private void CreateNewDSGroup(string groupName)
        {
            try
            {
                NewGroupIDCreated = cabRepo.CreateDocumentGroup(groupName);
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

                var addedUsers = cabRepo.GetUsersForDocumentGroup(groupId) ?? new List<string>();

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

                var groupArchives = cabRepo.GetArchivesForGroup(groupId) ?? new List<(string ShortIndexName, string LongIndexName)>();

                var allIndexes = cabRepo.GetAllActiveIndexes() ?? new List<(string ShortIndexName, string LongIndexName)>();

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

                var allIndexes = cabRepo.GetAllActiveIndexes() ?? new List<(string ShortIndexName, string LongIndexName)>();

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
                        CurrentDSGroupID = cabRepo.GetDocumentGroupIdByName(GetGroupNameOnly(CurrentDSGroupName));
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
                        gid = cabRepo.GetDocumentGroupIdByName(GetGroupNameOnly(name));
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
                            ok = cabRepo.DeleteDocumentGroup(gid);
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
                    if (cabRepo.DoesDocumentGroupExist(GetGroupNameOnly(CurrentDSGroupName)))
                    {
                        CurrentDSGroupID = cabRepo.GetDocumentGroupIdByName(GetGroupNameOnly(CurrentDSGroupName));
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
                    usersSaved = cabRepo.SetUsersForDocumentGroup(CurrentDSGroupID, usersToSave);
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

                    archivesSaved = cabRepo.SetArchivesForDocumentGroup(CurrentDSGroupID, shortNames);
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
    }
}
