using System.Windows;
using System.Windows.Media;

// Alias conflicting namespaces if System.Drawing or System.Windows.Forms are needed elsewhere,
// otherwise, remove the unused using statements. For now, let's assume they might be needed
// and use explicit qualification.
using WpfControls = System.Windows.Controls;
using WpfMedia = System.Windows.Media;
// using System.Drawing; // Potentially conflicting
// using System.Windows.Forms; // Potentially conflicting

namespace DeskViz.App.Controls
{
    /// <summary>
    /// Interaction logic for DeskVizProgressBar.xaml
    /// A reusable progress bar combining AnimatedProgressBar and optional text display.
    /// </summary>
    public partial class DeskVizProgressBar : WpfControls.UserControl
    {
        public DeskVizProgressBar()
        {
            InitializeComponent();
        }

        // Value Dependency Property
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(DeskVizProgressBar),
            new PropertyMetadata(0.0, OnValuePropertyChanged));

        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        // Maximum Dependency Property
        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(DeskVizProgressBar), new PropertyMetadata(100.0));

        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        // BarHeight Dependency Property
        public static readonly DependencyProperty BarHeightProperty =
            DependencyProperty.Register(nameof(BarHeight), typeof(double), typeof(DeskVizProgressBar), new PropertyMetadata(12.0));

        public double BarHeight
        {
            get { return (double)GetValue(BarHeightProperty); }
            set { SetValue(BarHeightProperty, value); }
        }

        // ProgressBrush Dependency Property
        public static readonly DependencyProperty ProgressBrushProperty =
            DependencyProperty.Register(nameof(ProgressBrush), typeof(WpfMedia.Brush), typeof(DeskVizProgressBar), new PropertyMetadata(WpfMedia.Brushes.LimeGreen)); // Default Green

        public WpfMedia.Brush ProgressBrush
        {
            get { return (WpfMedia.Brush)GetValue(ProgressBrushProperty); }
            set { SetValue(ProgressBrushProperty, value); }
        }

        // BackgroundBrush Dependency Property
        public static readonly DependencyProperty BackgroundBrushProperty =
             DependencyProperty.Register(nameof(BackgroundBrush), typeof(WpfMedia.Brush), typeof(DeskVizProgressBar), new PropertyMetadata(new WpfMedia.SolidColorBrush((WpfMedia.Color)WpfMedia.ColorConverter.ConvertFromString("#252525")))); // Default Dark Gray

        public WpfMedia.Brush BackgroundBrush
        {
            get { return (WpfMedia.Brush)GetValue(BackgroundBrushProperty); }
            set { SetValue(BackgroundBrushProperty, value); }
        }

        // ShowText Dependency Property
        public static readonly DependencyProperty ShowTextProperty =
            DependencyProperty.Register(nameof(ShowText), typeof(bool), typeof(DeskVizProgressBar), new PropertyMetadata(true));

        public bool ShowText
        {
            get { return (bool)GetValue(ShowTextProperty); }
            set { SetValue(ShowTextProperty, value); }
        }

        // ValueText Dependency Property (Read-only from outside, set internally)
        private static readonly DependencyPropertyKey ValueTextPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(ValueText), typeof(string), typeof(DeskVizProgressBar), new PropertyMetadata("0.0%"));

        public static readonly DependencyProperty ValueTextProperty = ValueTextPropertyKey.DependencyProperty;

        public string ValueText
        {
            get { return (string)GetValue(ValueTextProperty); }
            private set { SetValue(ValueTextPropertyKey, value); } // Make setter private
        }

        // TextBrush Dependency Property
        public static readonly DependencyProperty TextBrushProperty =
            DependencyProperty.Register(nameof(TextBrush), typeof(WpfMedia.Brush), typeof(DeskVizProgressBar), new PropertyMetadata(WpfMedia.Brushes.White)); // Default White

        public WpfMedia.Brush TextBrush
        {
            get { return (WpfMedia.Brush)GetValue(TextBrushProperty); }
            set { SetValue(TextBrushProperty, value); }
        }

        // AnimationDuration Dependency Property
        public static readonly DependencyProperty AnimationDurationProperty =
            DependencyProperty.Register(nameof(AnimationDuration), typeof(double), typeof(DeskVizProgressBar), new PropertyMetadata(500.0));

        /// <summary>
        /// Gets or sets the animation duration in milliseconds. Set to 0 for instant updates (no animation).
        /// </summary>
        public double AnimationDuration
        {
            get { return (double)GetValue(AnimationDurationProperty); }
            set { SetValue(AnimationDurationProperty, value); }
        }

        // Update ValueText when Value or Maximum changes
        private static void OnValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DeskVizProgressBar progressBar)
            {
                progressBar.UpdateValueText();
            }
        }

        // Method to update the displayed text
        private void UpdateValueText()
        {
            double percentage = (Maximum > 0) ? (Value / Maximum * 100) : 0;
            ValueText = $"{percentage:F1}%"; // Format to one decimal place
        }

        // Override OnApplyTemplate or use Loaded event if needed for initial setup
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdateValueText(); // Ensure text is correct on load
        }
    }
}
