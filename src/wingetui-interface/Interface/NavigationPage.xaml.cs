using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using ModernWindow.Interface.Widgets;
using Microsoft.UI.Composition;
using System.Numerics;
using System.Collections.ObjectModel;
using ModernWindow.PackageEngine;
using System.Threading.Tasks;
using ModernWindow.Structures;
using ModernWindow.Interface.Dialogs;
using ModernWindow.Data;
using System.Security.Cryptography.X509Certificates;
using CommunityToolkit.WinUI.Animations;
using ModernWindow.Interface.Pages;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow.Interface
{
    public sealed partial class NavigationPage : UserControl
    {
        public static AppTools bindings = AppTools.Instance;
        public SettingsInterface SettingsPage;
        public DiscoverPackagesPage DiscoverPage;
        public SoftwareUpdatesPage UpdatesPage;
        public InstalledPackagesPage InstalledPage;
        public HelpDialog HelpPage;
        public Page OldPage;
        public Page CurrentPage;
        public InfoBadge UpdatesBadge;
        public StackPanel OperationStackPanel;
        private Dictionary<Page, NavButton> PageButtonReference = new();

        private AboutWingetUI AboutPage;
        private IgnoredUpdatesManager IgnoredUpdatesPage;

        public NavigationPage()
        {
            this.InitializeComponent();
            UpdatesBadge = __updates_count_badge;
            OperationStackPanel = __operations_list_stackpanel;
            SettingsPage = new SettingsInterface();
            DiscoverPage = new DiscoverPackagesPage();
            UpdatesPage = new SoftwareUpdatesPage();
            InstalledPage = new InstalledPackagesPage();
            AboutPage = new AboutWingetUI();
            HelpPage = new HelpDialog();
            IgnoredUpdatesPage = new IgnoredUpdatesManager();

            int i = 0;
            foreach (Page page in new Page[] { DiscoverPage, UpdatesPage, InstalledPage, SettingsPage })
            {
                Grid.SetColumn(page, 0);
                Grid.SetRow(page, 0);
                MainContentPresenterGrid.Children.Add(page);
                i++;
            }

            PageButtonReference.Add(DiscoverPage, DiscoverNavButton);
            PageButtonReference.Add(UpdatesPage, UpdatesNavButton);
            PageButtonReference.Add(InstalledPage, InstalledNavButton);
            PageButtonReference.Add(SettingsPage, SettingsNavButton);

            DiscoverNavButton.ForceClick();
        }

        private void DiscoverNavButton_Click(object sender, NavButton.NavButtonEventArgs e)
        {
            NavigateToPage(DiscoverPage);
        }

        private void InstalledNavButton_Click(object sender, NavButton.NavButtonEventArgs e)
        {
            NavigateToPage(InstalledPage);
        }

        private void UpdatesNavButton_Click(object sender, NavButton.NavButtonEventArgs e)
        {
            NavigateToPage(UpdatesPage);
        }

        private void MoreNavButton_Click(object sender, NavButton.NavButtonEventArgs e)
        {

            foreach (NavButton button in bindings.App.mainWindow.NavButtonList)
                button.ToggleButton.IsChecked = false;
            MoreNavButton.ToggleButton.IsChecked = true;

            (VersionMenuItem as MenuFlyoutItem).Text = bindings.Translate("WingetUI Version {0}").Replace("{0}", CoreData.VersionName);
            MoreNavButtonMenu.ShowAt(MoreNavButton, new FlyoutShowOptions() { ShowMode = FlyoutShowMode.Standard });

            MoreNavButtonMenu.Closed += (s, e) =>
            {
                foreach (NavButton button in bindings.App.mainWindow.NavButtonList)
                    button.ToggleButton.IsChecked = (button == PageButtonReference[CurrentPage]);
            };
        }

        private void SettingsNavButton_Click(object sender, NavButton.NavButtonEventArgs e)
        {
            NavigateToPage(SettingsPage);
        }

        private async void AboutNavButton_Click(object sender, NavButton.NavButtonEventArgs e)
        {
            ContentDialog AboutDialog = new ContentDialog();
            AboutDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            AboutDialog.XamlRoot = this.XamlRoot;
            AboutDialog.Resources["ContentDialogMaxWidth"] = 1200;
            AboutDialog.Resources["ContentDialogMaxHeight"] = 1000;
            AboutDialog.Content = AboutPage;
            AboutDialog.PrimaryButtonText = bindings.Translate("Close");
            foreach (NavButton button in bindings.App.mainWindow.NavButtonList)
                button.ToggleButton.IsChecked = false;

            await bindings.App.mainWindow.ShowDialog(AboutDialog);

            AboutDialog.Content = null;
            foreach (NavButton button in bindings.App.mainWindow.NavButtonList)
                button.ToggleButton.IsChecked = (button == PageButtonReference[CurrentPage]);
            AboutDialog = null;
        }

        public async Task ManageIgnoredUpdatesDialog()
        {
            ContentDialog UpdatesDialog = new ContentDialog();
            UpdatesDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            UpdatesDialog.XamlRoot = this.XamlRoot;
            UpdatesDialog.Resources["ContentDialogMaxWidth"] = 1200;
            UpdatesDialog.Resources["ContentDialogMaxHeight"] = 1000;
            UpdatesDialog.PrimaryButtonText = bindings.Translate("Close");
            UpdatesDialog.SecondaryButtonText = bindings.Translate("Reset");
            UpdatesDialog.DefaultButton = ContentDialogButton.Primary;
            UpdatesDialog.Title = bindings.Translate("Manage ignored updates");
            UpdatesDialog.SecondaryButtonClick += IgnoredUpdatesPage.ManageIgnoredUpdates_SecondaryButtonClick;
            UpdatesDialog.Content = IgnoredUpdatesPage;

            _ = IgnoredUpdatesPage.UpdateData();
            await bindings.App.mainWindow.ShowDialog(UpdatesDialog);

            UpdatesDialog.Content = null;
            UpdatesDialog = null;
        }

        public async Task<bool> ShowInstallationSettingsForPackageAndContinue(Package package, OperationType Operation)
        {
            var OptionsPage = new InstallOptionsPage(package, Operation);

            ContentDialog OptionsDialog = new ContentDialog();
            OptionsDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            OptionsDialog.XamlRoot = this.XamlRoot;
            OptionsDialog.Resources["ContentDialogMaxWidth"] = 1200;
            OptionsDialog.Resources["ContentDialogMaxHeight"] = 1000;
            if (Operation == OperationType.Install)
                OptionsDialog.SecondaryButtonText = bindings.Translate("Install");
            else if(Operation == OperationType.Update)
                OptionsDialog.SecondaryButtonText = bindings.Translate("Update");
            else 
                OptionsDialog.SecondaryButtonText = bindings.Translate("Uninstall");
            OptionsDialog.PrimaryButtonText = bindings.Translate("Save and close");
            OptionsDialog.DefaultButton = ContentDialogButton.Secondary;
            OptionsDialog.Title = bindings.Translate("{0} installation options").Replace("{0}", package.Name);
            OptionsDialog.Content = OptionsPage;

            var result = await bindings.App.mainWindow.ShowDialog(OptionsDialog);
            OptionsPage.SaveToDisk();

            OptionsDialog.Content = null;
            OptionsDialog = null;

            return result == ContentDialogResult.Secondary;

        }

        private void NavigateToPage(Page TargetPage)
        {
            foreach (Page page in PageButtonReference.Keys)
                if (page.Visibility == Visibility.Visible)
                    OldPage = page;
            if(!PageButtonReference.ContainsKey(TargetPage))
            {
                PageButtonReference.Add(TargetPage, MoreNavButton);
                Grid.SetColumn(TargetPage, 0);
                Grid.SetRow(TargetPage, 0);
                MainContentPresenterGrid.Children.Add(TargetPage);
            }
            foreach (NavButton button in bindings.App.mainWindow.NavButtonList)
            {
                
                button.ToggleButton.IsChecked = (button == PageButtonReference[TargetPage]);
            }

            foreach (Page page in PageButtonReference.Keys)
                page.Visibility = (page == TargetPage) ? Visibility.Visible : Visibility.Collapsed;

            CurrentPage = TargetPage;
        }

        private async void ReleaseNotesMenu_Click(object sender, RoutedEventArgs e)
        {

            ContentDialog NotesDialog = new ContentDialog();
            NotesDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            NotesDialog.XamlRoot = this.XamlRoot;
            NotesDialog.Resources["ContentDialogMaxWidth"] = 12000;
            NotesDialog.Resources["ContentDialogMaxHeight"] = 10000;
            NotesDialog.CloseButtonText = bindings.Translate("Close");
            NotesDialog.Title = bindings.Translate("Release notes");
            var notes = new ReleaseNotes();
            NotesDialog.Content = notes;
            NotesDialog.SizeChanged += (s, e) =>
            {
                notes.MinWidth = ActualWidth - 300;
                notes.MinHeight = ActualHeight- 200;
            };

            await bindings.App.mainWindow.ShowDialog(NotesDialog);

            NotesDialog = null;
        }

        public async Task ShowPackageDetails(Package package, OperationType ActionOperation)
        {
            var DetailsPage = new PackageDetailsPage(package, ActionOperation);

            ContentDialog DetailsDialog = new ContentDialog();
            DetailsDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            DetailsDialog.XamlRoot = this.XamlRoot;
            DetailsDialog.Resources["ContentDialogMaxWidth"] = 8000;
            DetailsDialog.Resources["ContentDialogMaxHeight"] = 4000;
            DetailsDialog.Content = DetailsPage;
            DetailsDialog.SizeChanged += (s, e) =>
            {
                DetailsPage.MinWidth = ActualWidth - 300;
                DetailsPage.MinHeight = ActualHeight - 100;
                DetailsPage.MaxWidth = ActualWidth - 300;
                DetailsPage.MaxHeight = ActualHeight - 100;
            };

            DetailsPage.Close += (s, e) => { DetailsDialog.Hide(); };

            await bindings.App.mainWindow.ShowDialog(DetailsDialog);

            DetailsDialog.Content = null;
            DetailsDialog = null;

        }

        private void OperationHistoryMenu_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage(new LogPage(LogType.OperationHistory));
        }

        private void ManagerLogsMenu_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage(new LogPage(LogType.ManagerLogs));
        }

        private void WingetUILogs_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage(new LogPage(LogType.WingetUILog));
        }


        private void HelpMenu_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }
        public async void ShowHelp()
        { 
            NavigateToPage(HelpPage);
        }

        private void QuitWingetUI_Click(object sender, RoutedEventArgs e)
        {
            bindings.App.DisposeAndQuit();
        }
    }
}
