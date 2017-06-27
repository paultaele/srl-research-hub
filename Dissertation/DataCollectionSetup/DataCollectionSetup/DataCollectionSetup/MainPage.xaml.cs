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
            PromptElement currentPromptElement = new PromptElement(MyPromptElementsStack.Children.Count, MyCounter);
            currentPromptElement.Name = "myPromptElement" + MyCounter;
            MyCounter++;

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

            if (promptElementsToDelete.Count == 0) { return; }

            foreach (var promptElementToDelate in promptElementsToDelete)
            {
                MyPromptElementsStack.Children.Remove(promptElementToDelate);
            }

            for (int i = 0; i < MyPromptElementsStack.Children.Count; ++i)
            {
                var promptElement = (PromptElement)(MyPromptElementsStack.Children[i]);
                promptElement.PositionName = "" + i;
            }
        }

        private void InsertPromptElementsButton_Click(object sender, RoutedEventArgs e)
        {
            // check if the value is valid
            string value = InsertIndexText.Text;
            int index;
            if (!int.TryParse(value, out index)) {
                InsertIndexText.Text = ""; // clear the text box
                return; // do nothing
            }

            //
            int count = MyPromptElementsStack.Children.Count;

            // case: stack is empty
            if (count == 0)
            {
                PromptElement currentPromptElement = new PromptElement(0, MyCounter);
                currentPromptElement.Name = "myPromptElement" + MyCounter;
                MyCounter++;

                MyPromptElementsStack.Children.Add(currentPromptElement);
            }

            // case: index exceeds stack size
            else if (index >= count)
            {
                PromptElement currentPromptElement = new PromptElement(count, MyCounter);
                currentPromptElement.Name = "myPromptElement" + MyCounter;
                MyCounter++;

                MyPromptElementsStack.Children.Add(currentPromptElement);
            }

            //
            else
            {
                PromptElement currentPromptElement = new PromptElement(index, MyCounter);
                currentPromptElement.Name = "myPromptElement" + MyCounter;
                MyCounter++;

                //
                MyPromptElementsStack.Children.Insert(index, currentPromptElement);
                for (int i = index + 1; i < MyPromptElementsStack.Children.Count; ++i)
                {
                    var promptElement = (PromptElement)MyPromptElementsStack.Children[i];
                    promptElement.PositionName = "" + i;
                }
            }

            this.InsertIndexText.Text = "";
        }

        private int MyCounter { get; set; }
    }
}
