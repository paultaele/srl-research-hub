﻿using Srl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Shapes;

namespace PaulAnimationViewer
{
    public class Helper
    {
        /// <summary>
        /// This method creates the animations for tracing over the sketch.
        /// </summary>
        /// <param name="canvas">The canvas to show the animation.</param>
        /// <param name="strokesCollection">The strokes of the sketch.</param>
        /// <param name="timesCollection">The times of the sketch.</param>
        public static List<Storyboard> Trace(Canvas canvas, List<InkStroke> strokesCollection, List<List<long>> timesCollection, SolidColorBrush color, int duration)
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

            // iterate through each stroke
            List<Storyboard> storyboards = new List<Storyboard>();
            for (int i = 0; i < strokesCollection.Count; ++i)
            {
                // set the visuals of the stroke's corresponding tracer
                Ellipse tracer = new Ellipse()
                {
                    Width = 50,
                    Height = 50,
                    Fill = color
                };

                // add the tracer to the canvas
                // note: the tracer is moved up and left its radius to center
                Canvas.SetLeft(tracer, -tracer.Width / 2);
                Canvas.SetTop(tracer, -tracer.Height / 2);
                canvas.Children.Add(tracer);

                // initialize the storyboard and animations
                tracer.RenderTransform = new CompositeTransform();
                Storyboard storyboard = new Storyboard();
                DoubleAnimationUsingKeyFrames translateXAnimation = new DoubleAnimationUsingKeyFrames();
                DoubleAnimationUsingKeyFrames translateYAnimation = new DoubleAnimationUsingKeyFrames();
                DoubleAnimationUsingKeyFrames fadeAnimation = new DoubleAnimationUsingKeyFrames();

                // --------------------------------------------------

                // get the current stroke and times
                InkStroke stroke = strokesCollection[i];
                List<long> times = newTimesCollection[i];
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
                Storyboard.SetTarget(translateXAnimation, tracer);
                Storyboard.SetTarget(translateYAnimation, tracer);
                Storyboard.SetTarget(fadeAnimation, tracer);

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

        public static List<Storyboard> Trace(Canvas canvas, List<InkStroke> strokesCollection, List<List<long>> timesCollection, SolidColorBrush color, int duration, Sketch model)
        {
            // set the input duration
            int numModelPoints = 0;
            int numInputPoints = 0;
            foreach (InkStroke stroke in model.Strokes) { numModelPoints += stroke.GetInkPoints().Count; }
            foreach (InkStroke stroke in strokesCollection) { numInputPoints += stroke.GetInkPoints().Count; }
            int modelDuration = 30000;
            int modelTotalDuration = modelDuration * numModelPoints;
            int newDuration = modelTotalDuration / numInputPoints;

            return Trace(canvas, strokesCollection, timesCollection, color, newDuration);
        }
    }
}
