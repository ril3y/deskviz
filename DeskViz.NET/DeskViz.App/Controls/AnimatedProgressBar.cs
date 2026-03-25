using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace DeskViz.App.Controls
{
    /// <summary>
    /// A ProgressBar that animates smoothly when its value changes via the AnimatedValue property.
    /// </summary>
    public class AnimatedProgressBar : System.Windows.Controls.ProgressBar
    {
        // Dependency Property for the value we bind to
        public static readonly DependencyProperty AnimatedValueProperty =
            DependencyProperty.Register(
                nameof(AnimatedValue),
                typeof(double),
                typeof(AnimatedProgressBar),
                new PropertyMetadata(0.0, OnAnimatedValueChanged));

        /// <summary>
        /// Gets or sets the value that the ProgressBar should animate towards.
        /// Bind your ViewModel property to this instead of the regular Value property.
        /// </summary>
        public double AnimatedValue
        {
            get { return (double)GetValue(AnimatedValueProperty); }
            set { SetValue(AnimatedValueProperty, value); }
        }

        // Dependency Property for animation duration
        public static readonly DependencyProperty AnimationDurationProperty =
            DependencyProperty.Register(
                nameof(AnimationDuration),
                typeof(double),
                typeof(AnimatedProgressBar),
                new PropertyMetadata(500.0)); // Default 500ms for smooth transitions

        /// <summary>
        /// Gets or sets the animation duration in milliseconds.
        /// Set to 0 for instant updates (no animation) - useful when using external interpolation.
        /// </summary>
        public double AnimationDuration
        {
            get { return (double)GetValue(AnimationDurationProperty); }
            set { SetValue(AnimationDurationProperty, value); }
        }

        // Shared easing function for smooth, natural-feeling animations
        private static readonly IEasingFunction _easingFunction = new CubicEase
        {
            EasingMode = EasingMode.EaseOut // Starts fast, slows down at end - feels responsive
        };

        /// <summary>
        /// Callback function triggered when the AnimatedValue property changes.
        /// </summary>
        private static void OnAnimatedValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AnimatedProgressBar progressBar && e.NewValue is double newValue)
            {
                // If duration is 0, update instantly without animation (for external interpolation)
                if (progressBar.AnimationDuration <= 0)
                {
                    progressBar.Value = newValue;
                }
                else
                {
                    // Create the animation with easing for smooth, fluid motion
                    var animation = new DoubleAnimation
                    {
                        To = newValue,
                        Duration = new Duration(TimeSpan.FromMilliseconds(progressBar.AnimationDuration)),
                        EasingFunction = _easingFunction
                    };

                    // Apply the animation to the base Value property
                    progressBar.BeginAnimation(System.Windows.Controls.ProgressBar.ValueProperty, animation);
                }
            }
        }
    }
}
