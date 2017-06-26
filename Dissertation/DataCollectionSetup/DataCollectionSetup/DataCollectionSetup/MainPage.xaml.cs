using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace DataCollectionSetup
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ;
        }

        private void AddPromptElementsButton_Click(object sender, RoutedEventArgs e)
        {
            int count = MyPromptElementsStack.Children.Count;
            PromptElement currentPromptElement = new PromptElement(count);
            currentPromptElement.Name = "myPromptElement" + MyCounter++;

            MyPromptElementsStack.Children.Add(currentPromptElement);
        }

        private void RemovePromptElementsButton_Click(object sender, RoutedEventArgs e)
        {
            var promptElementsToDelete = new List<PromptElement>();
            foreach (PromptElement promptElement in MyPromptElementsStack.Children)
            {
                if (promptElement.IsChecked)
                {
                    promptElementsToDelete.Add(promptElement);
                }
            }

            foreach (var promptElementToDelate in promptElementsToDelete)
            {
                MyPromptElementsStack.Children.Remove(promptElementToDelate);
            }
        }

        private int MyCounter { get; set; }
    }
}
