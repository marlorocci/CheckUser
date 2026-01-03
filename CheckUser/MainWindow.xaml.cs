using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace CheckUser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<string> currentGroups = new List<string>();
        private string currentUsername = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnCheck_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Please enter a username.");
                return;
            }

            lstGroups.Items.Clear();
            currentGroups.Clear();
            txtStatus.Text = "Querying Active Directory...";
            txtSaveInfo.Text = string.Empty;
            btnSave.IsEnabled = false;

            try
            {
                using (PrincipalContext context = new PrincipalContext(ContextType.Domain))
                {
                    UserPrincipal user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);

                    if (user == null)
                    {
                        txtStatus.Text = "User not found in the domain.";
                        return;
                    }

                    PrincipalSearchResult<Principal> groups = user.GetAuthorizationGroups();

                    currentGroups = groups
                        .Select(g => g.DisplayName ?? g.Name ?? g.SamAccountName ?? g.DistinguishedName)
                        .OrderBy(name => name)
                        .ToList();

                    foreach (string group in currentGroups)
                    {
                        lstGroups.Items.Add(group);
                    }

                    currentUsername = username;
                    txtStatus.Text = $"Found {currentGroups.Count} groups for user '{username}'.";
                    btnSave.IsEnabled = true; // Enable save button only after successful query
                }
            }
            catch (Exception ex)
            {
                txtStatus.Text = "Error: " + ex.Message;
                MessageBox.Show(ex.Message, "Error querying Active Directory", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (currentGroups.Count == 0)
            {
                MessageBox.Show("No groups to save.");
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                FileName = $"{currentUsername}_AD_Groups.txt",
                DefaultExt = ".txt",
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                Title = "Save Group Membership"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (StreamWriter writer = new StreamWriter(saveFileDialog.FileName))
                    {
                        writer.WriteLine($"Active Directory Group Membership for: {currentUsername}");
                        writer.WriteLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                        writer.WriteLine($"Total Groups: {currentGroups.Count}");
                        writer.WriteLine(new string('-', 50));

                        foreach (string group in currentGroups)
                        {
                            writer.WriteLine(group);
                        }
                    }

                    txtSaveInfo.Text = $"Groups saved to: {saveFileDialog.FileName}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}