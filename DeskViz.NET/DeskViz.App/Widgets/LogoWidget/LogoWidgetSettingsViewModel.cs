using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;
using System.Windows;

namespace DeskViz.App.Widgets
{
    public class LogoWidgetSettingsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _imagePath = string.Empty;
        public string ImagePath
        {
            get => _imagePath;
            set { _imagePath = value; OnPropertyChanged(nameof(ImagePath)); }
        }

        private double? _imageWidth;
        public double? ImageWidth
        {
            get => _imageWidth;
            set { _imageWidth = value; OnPropertyChanged(nameof(ImageWidth)); }
        }

        private double? _imageHeight;
        public double? ImageHeight
        {
            get => _imageHeight;
            set { _imageHeight = value; OnPropertyChanged(nameof(ImageHeight)); }
        }

        // Properties for ComboBox bindings
        public List<System.Windows.Media.Stretch> StretchOptions { get; } = System.Enum.GetValues(typeof(System.Windows.Media.Stretch)).Cast<System.Windows.Media.Stretch>().ToList();
        public List<System.Windows.HorizontalAlignment> HorizontalAlignmentOptions { get; } = System.Enum.GetValues(typeof(System.Windows.HorizontalAlignment)).Cast<System.Windows.HorizontalAlignment>().ToList();
        public List<System.Windows.VerticalAlignment> VerticalAlignmentOptions { get; } = System.Enum.GetValues(typeof(System.Windows.VerticalAlignment)).Cast<System.Windows.VerticalAlignment>().ToList();

        private System.Windows.Media.Stretch _selectedStretch = System.Windows.Media.Stretch.Uniform;
        public System.Windows.Media.Stretch SelectedStretch
        {
            get => _selectedStretch;
            set { _selectedStretch = value; OnPropertyChanged(nameof(SelectedStretch)); }
        }

        private System.Windows.HorizontalAlignment _selectedHorizontalAlignment = System.Windows.HorizontalAlignment.Center;
        public System.Windows.HorizontalAlignment SelectedHorizontalAlignment
        {
            get => _selectedHorizontalAlignment;
            set { _selectedHorizontalAlignment = value; OnPropertyChanged(nameof(SelectedHorizontalAlignment)); }
        }

        private System.Windows.VerticalAlignment _selectedVerticalAlignment = System.Windows.VerticalAlignment.Center;
        public System.Windows.VerticalAlignment SelectedVerticalAlignment
        {
            get => _selectedVerticalAlignment;
            set { _selectedVerticalAlignment = value; OnPropertyChanged(nameof(SelectedVerticalAlignment)); }
        }
    }
}
