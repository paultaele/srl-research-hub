using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace Srl
{
    public class InteractionTools
    {
        public async static Task<StorageFolder> PickFolder()
        {
            FolderPicker picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.ViewMode = PickerViewMode.List;
            picker.FileTypeFilter.Add("*");

            StorageFolder folder = await picker.PickSingleFolderAsync();

            return folder;
        }

        public async static Task<List<StorageFile>> PickFiles()
        {
            StorageFolder folder = await PickFolder();
            if (folder == null) { return null; }

            List<StorageFile> files = (await folder.GetFilesAsync()).ToList();
            return files;
        }

        public async static Task<StorageFile> PickFile()
        {
            return await PickFile("*");
        }

        public async static Task<StorageFile> PickFile(string type)
        {
            return await PickFile(new List<string>() { type });
        }

        public async static Task<StorageFile> PickFile(List<string> types)
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.ViewMode = PickerViewMode.List;
            foreach (string type in types) { picker.FileTypeFilter.Add(type); }

            StorageFile file = await picker.PickSingleFileAsync();

            return file;
        }

        public async static void SetImage(Image image, StorageFile file)
        {
            IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read);
            BitmapImage bitmap = new BitmapImage();
            bitmap.SetSource(fileStream);
            image.Source = bitmap;
        }
    }
}
