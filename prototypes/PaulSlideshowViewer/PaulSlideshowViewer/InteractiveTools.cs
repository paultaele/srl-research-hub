using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace PaulSlideshowViewer
{
    public class InteractiveTools
    {
        public static StorageFolder GetFolder()
        {
            FolderPicker picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.ViewMode = PickerViewMode.List;
            picker.FileTypeFilter.Add("*");

            StorageFolder folder = null;
            Task task = Task.Run(async () => folder = await picker.PickSingleFolderAsync());
            task.Wait();

            return folder;
        }

        public void GetFile()
        {

        }
    }
}
