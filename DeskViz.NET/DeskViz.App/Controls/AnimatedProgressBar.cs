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
                new PropertyMetadata(0.0, OnAnimatedValueChanged)); // Default value 0.0, Callback on change

        /// <summary>
        /// Gets or sets the value that the ProgressBar should animate towards.
        /// Bind your ViewModel property to this instead of the regular Value property.
        /// </summary>
        public double AnimatedValue
        {
            get { return (double)GetValue(AnimatedValueProperty); }
            set { SetValue(AnimatedValueProperty, value); }
        }

        // Animation duration (can be made a Dependency Property later if needed)
        private static readonly Duration _animationDuration = new Duration(TimeSpan.FromMilliseconds(300));

        /// <summary>
        /// Callback function triggered when the AnimatedValue property changes.
        /// </summary>
        private static void OnAnimatedValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AnimatedProgressBar progressBar && e.NewValue is double newValue)
            {
                // Create the animation
                var animation = new DoubleAnimation
                {
                    To = newValue,
                    Duration = _animationDuration
                };

                // Apply the animation to the base Value property
                progressBar.BeginAnimation(System.Windows.Controls.ProgressBar.ValueProperty, animation);
            }
        }
    }
}
