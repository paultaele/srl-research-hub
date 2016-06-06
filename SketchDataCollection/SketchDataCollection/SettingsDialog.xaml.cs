using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SketchDataCollection
{
    public sealed partial class SettingsDialog : ContentDialog
    {
        public SettingsDialog()
        {
            this.InitializeComponent();
        }

        private void MySettingsDialog_Loaded(object sender, RoutedEventArgs e)
        {
            MyIterationsCountText.Text = "Count: " + MyIterationsSlider.Value;
        }

        private void MySaveSettingsButton_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            IterationsCount = (int)MyIterationsSlider.Value;
            IsSquareArea = MySquareAreaRadio.IsChecked.Value;
            CanDisplayTraceImage = MyDisplayTraceImageToggle.IsOn;
            CanDisplayPreviewImage = MyDisplayPreviewImageToggle.IsOn;
            CanDisplayRandomImages = MyDisplayRandomImagesToggle.IsOn;
        }

        private void MyCancelSettingsButton_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            Application.Current.Exit();
        }

        private async void MyLoadDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            LoadFolder = await folderPicker.PickSingleFolderAsync();
            if (LoadFolder != null)
            {
                Debug.WriteLine("Picked folder: " + LoadFolder.Name);

                // Application now has read/write access to all contents in the picked folder (including other sub-folder contents)
                StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", LoadFolder);
                MyLoadDirectoryText.Text = LoadFolder.DisplayName;

                //
                HasLoadFolder = true;
                if (HasLoadFolder && HasSaveFolder) { IsPrimaryButtonEnabled = true; }
            }
            else
            {
                Debug.WriteLine("Operation cancelled.");
            }
        }

        private async void MySaveDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            SaveFolder = await folderPicker.PickSingleFolderAsync();
            if (SaveFolder != null)
            {
                Debug.WriteLine("Picked folder: " + SaveFolder.Name);

                // Application now has read/write access to all contents in the picked folder (including other sub-folder contents)
                StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", SaveFolder);
                MySaveDirectoryText.Text = SaveFolder.DisplayName;

                //
                HasSaveFolder = true;
                if (HasLoadFolder && HasSaveFolder) { IsPrimaryButtonEnabled = true; }
            }
            else
            {
                Debug.WriteLine("Operation cancelled.");
            }
        }

        #region Properties

        public StorageFolder LoadFolder { get; private set; }
        public StorageFolder SaveFolder { get; private set; }
        public int IterationsCount { get; private set; }
        public bool IsSquareArea { get; private set; }
        public bool CanDisplayTraceImage { get; private set; }
        public bool CanDisplayPreviewImage { get; private set; }
        public bool CanDisplayRandomImages { get; private set; }
        public bool HasLoadFolder { get; private set; }
        public bool HasSaveFolder { get; private set; }

        #endregion

        private void MyIterationsSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            MyIterationsCountText.Text = "Count: " + MyIterationsSlider.Value;
        }
    }
}
