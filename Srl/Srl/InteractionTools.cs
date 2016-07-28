using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

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

        public async static Task Delay(int delay)
        {
            // note: 10000 is to convert from ticks to milliseconds
            await Task.Delay(delay / 10000);
        }

        public static List<Storyboard> Trace(Canvas canvas, List<InkStroke> strokesCollection, List<List<long>> timesCollection, double size, SolidColorBrush brush, int duration)
        {
            // set the timings of the animation
            List<List<long>> newTimesCollection = new List<List<long>>();
            int time = 0;
            for (int i = 0; i < timesCollection.Count; ++i)
            {
                List<long> times = new List<long>();
                for (int j = 0; j < timesCollection[i].Count; ++j)
                {
                    time += duration;
                    times.Add(time);
                }

                newTimesCollection.Add(times);
            }

            return AnimateDot(canvas, strokesCollection, newTimesCollection, size, brush);
        }

        public static List<Storyboard> Trace(Canvas canvas, List<InkStroke> strokesCollection, List<List<long>> timesCollection, double size, SolidColorBrush brush, int duration, Sketch model)
        {
            // set the input duration
            int numModelPoints = 0;
            int numInputPoints = 0;
            foreach (InkStroke stroke in model.Strokes) { numModelPoints += stroke.GetInkPoints().Count; }
            foreach (InkStroke stroke in strokesCollection) { numInputPoints += stroke.GetInkPoints().Count; }
            int modelTotalDuration = duration * numModelPoints;
            int newDuration = modelTotalDuration / numInputPoints;

            return Trace(canvas, strokesCollection, timesCollection, size, brush, newDuration);
        }

        public static List<Storyboard> Playback(Canvas canvas, List<InkStroke> strokesCollection, List<List<long>> timesCollection, double size, SolidColorBrush brush)
        {
            // set the timings of the animation
            List<List<long>> newTimesCollection = new List<List<long>>();
            long offset = timesCollection[0][0];
            for (int i = 0; i < timesCollection.Count; ++i)
            {
                List<long> times = new List<long>();
                for (int j = 0; j < timesCollection[i].Count; ++j)
                {
                    long time = timesCollection[i][j] - offset;
                    times.Add(time);
                }

                newTimesCollection.Add(times);
            }

            return AnimateDot(canvas, strokesCollection, newTimesCollection, size, brush);
        }

        private static List<Storyboard> AnimateDot(Canvas canvas, List<InkStroke> strokesCollection, List<List<long>> timesCollection, double size, Brush brush)
        {
            // iterate through each stroke
            List<Storyboard> storyboards = new List<Storyboard>();
            for (int i = 0; i < strokesCollection.Count; ++i)
            {
                // set the visuals of the stroke's corresponding tracer
                Ellipse animator = new Ellipse()
                {
                    Width = size,
                    Height = size,
                    Fill = brush,
                    Stroke = new SolidColorBrush(Colors.DarkGray),
                    StrokeThickness = 5,
                };

                // add the tracer to the canvas
                // note: the tracer is moved up and left its radius to center
                Canvas.SetLeft(animator, -animator.Width / 2);
                Canvas.SetTop(animator, -animator.Height / 2);
                canvas.Children.Add(animator);

                // initialize the storyboard and animations
                animator.RenderTransform = new CompositeTransform();
                Storyboard storyboard = new Storyboard();
                DoubleAnimationUsingKeyFrames translateXAnimation = new DoubleAnimationUsingKeyFrames();
                DoubleAnimationUsingKeyFrames translateYAnimation = new DoubleAnimationUsingKeyFrames();
                DoubleAnimationUsingKeyFrames fadeAnimation = new DoubleAnimationUsingKeyFrames();

                // --------------------------------------------------

                // get the current stroke and times
                InkStroke stroke = strokesCollection[i];
                List<long> times = timesCollection[i];
                int pointsCount = stroke.GetInkPoints().Count;
                int count = pointsCount < times.Count ? pointsCount : times.Count;

                // get the tracer's starting position
                double startX = stroke.GetInkPoints()[0].Position.X;
                double startY = stroke.GetInkPoints()[0].Position.Y;

                // create the tracer's translation animations
                KeyTime keyTime;
                EasingDoubleKeyFrame frameX, frameY;
                double x, y;
                for (int j = 0; j < count; ++j)
                {
                    keyTime = new TimeSpan(times[j]);
                    x = stroke.GetInkPoints()[j].Position.X;
                    y = stroke.GetInkPoints()[j].Position.Y;

                    frameX = new EasingDoubleKeyFrame() { KeyTime = keyTime, Value = x };
                    frameY = new EasingDoubleKeyFrame() { KeyTime = keyTime, Value = y };

                    translateXAnimation.KeyFrames.Add(frameX);
                    translateYAnimation.KeyFrames.Add(frameY);
                }

                // create the tracer's fade animations
                long firstTime = times[0];
                long lastTime = times[times.Count - 1];
                fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(0), Value = 1 });
                fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(0), Value = 0 });
                fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(firstTime), Value = 0 });
                fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(firstTime), Value = 1 });
                fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(lastTime), Value = 1 });
                fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(lastTime), Value = 0 });

                // assign the animations to the tracer
                Storyboard.SetTarget(translateXAnimation, animator);
                Storyboard.SetTarget(translateYAnimation, animator);
                Storyboard.SetTarget(fadeAnimation, animator);

                // assign the animations to their behavior's properties
                Storyboard.SetTargetProperty(translateXAnimation, "(UIElement.RenderTransform).(CompositeTransform.TranslateX)");
                Storyboard.SetTargetProperty(translateYAnimation, "(UIElement.RenderTransform).(CompositeTransform.TranslateY)");
                Storyboard.SetTargetProperty(fadeAnimation, "(UIElement.Opacity)");

                // add the animations to the storyboard
                storyboard.Children.Add(translateXAnimation);
                storyboard.Children.Add(translateYAnimation);
                storyboard.Children.Add(fadeAnimation);

                // add the storyboard to the collection
                storyboards.Add(storyboard);
            }

            return storyboards;
        }

        public static List<Storyboard> DisplayPaths(Canvas canvas, List<InkStroke> strokesCollection, Brush brush, double size, int duration)
        {
            // set the initial time
            long time = 0;

            // iterate through each stroke
            List<Storyboard> storyboards = new List<Storyboard>();
            for (int i = 0; i < strokesCollection.Count; ++i)
            {
                List<InkPoint> points = new List<InkPoint>(strokesCollection[i].GetInkPoints());
                for (int j = 0; j < points.Count; ++j)
                {
                    InkPoint point = points[j];

                    // set the visuals of the stroke's corresponding dot
                    Ellipse dot = new Ellipse()
                    {
                        Width = size,
                        Height = size,
                        Fill = brush,
                        Stroke = brush,
                        //StrokeThickness = 5,
                    };

                    // add the dot to the canvas
                    // note: the tracer is moved up and left its radius to center
                    Canvas.SetLeft(dot, (-dot.Width / 2) + point.Position.X);
                    Canvas.SetTop(dot, (-dot.Height / 2) + point.Position.Y);
                    canvas.Children.Add(dot);

                    // initialize the storyboard and animations
                    dot.RenderTransform = new CompositeTransform();
                    Storyboard storyboard = new Storyboard();
                    DoubleAnimationUsingKeyFrames fadeAnimation = new DoubleAnimationUsingKeyFrames();

                    // --------------------------------------------------

                    // create the animator's fade animations
                    long appear = time;
                    long disappear = time + duration;
                    fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(0), Value = 1 });               // visible
                    fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(0), Value = 0 });               // inivisible
                    fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(appear), Value = 0 });            // inivisible
                    fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(appear), Value = 1 });            // visible
                    fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(disappear), Value = 1 });   // visible
                    fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(disappear), Value = 0 });   // inivisible

                    // assign the animations to the animator
                    Storyboard.SetTarget(fadeAnimation, dot);

                    // assign the animations to their behavior's properties
                    Storyboard.SetTargetProperty(fadeAnimation, "(UIElement.Opacity)");

                    // add the animations to the storyboard
                    storyboard.Children.Add(fadeAnimation);

                    // add the storyboard to the collection
                    storyboards.Add(storyboard);
                }

                // update the time for the next stroke
                time += duration;
            }

            return storyboards;
        }

        public static List<Storyboard> DisplayPaths(Canvas canvas, List<InkStroke> strokesCollection, Brush brush, double size, int duration, Sketch sketch)
        {
            int newDuration = duration * sketch.Strokes.Count / strokesCollection.Count;

            return DisplayPaths(canvas, strokesCollection, brush, size, newDuration);
        }

        public static List<Storyboard> DisplayEndpoints(Canvas canvas, List<InkStroke> strokesCollection, Brush brush, double size, int duration)
        {
            // set the initial time
            long time = 0;

            // iterate through each stroke
            List<Storyboard> storyboards = new List<Storyboard>();
            for (int i = 0; i < strokesCollection.Count; ++i)
            {
                List<InkPoint> points = new List<InkPoint>(strokesCollection[i].GetInkPoints());
                for (int j = 0; j < points.Count; j = j + points.Count - 1)
                {
                    InkPoint point = points[j];

                    // set the visuals of the stroke's corresponding dot
                    Ellipse dot = new Ellipse()
                    {
                        Width = size,
                        Height = size,
                        Fill = brush,
                        Stroke = new SolidColorBrush(Colors.DarkGray),
                        StrokeThickness = 5,
                    };

                    // add the dot to the canvas
                    // note: the tracer is moved up and left its radius to center
                    Canvas.SetLeft(dot, (-dot.Width / 2) + point.Position.X);
                    Canvas.SetTop(dot, (-dot.Height / 2) + point.Position.Y);
                    canvas.Children.Add(dot);

                    // initialize the storyboard and animations
                    dot.RenderTransform = new CompositeTransform();
                    Storyboard storyboard = new Storyboard();
                    DoubleAnimationUsingKeyFrames fadeAnimation = new DoubleAnimationUsingKeyFrames();

                    // --------------------------------------------------

                    // create the animator's fade animations
                    long appear = time;
                    long disappear = time + duration;
                    fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(0), Value = 1 });               // visible
                    fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(0), Value = 0 });               // inivisible
                    fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(appear), Value = 0 });            // inivisible
                    fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(appear), Value = 1 });            // visible
                    fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(disappear), Value = 1 });   // visible
                    fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(disappear), Value = 0 });   // inivisible

                    // assign the animations to the animator
                    Storyboard.SetTarget(fadeAnimation, dot);

                    // assign the animations to their behavior's properties
                    Storyboard.SetTargetProperty(fadeAnimation, "(UIElement.Opacity)");

                    // add the animations to the storyboard
                    storyboard.Children.Add(fadeAnimation);

                    // add the storyboard to the collection
                    storyboards.Add(storyboard);
                }

                // update the time for the next stroke
                time += duration;
            }

            return storyboards;
        }

        public static List<Storyboard> DisplayEndpoints(Canvas canvas, List<InkStroke> strokesCollection, Brush brush, double size, int duration, Sketch sketch)
        {
            int newDuration = duration * sketch.Strokes.Count / strokesCollection.Count;

            return DisplayEndpoints(canvas, strokesCollection, brush, size, newDuration);
        }
    }
}
