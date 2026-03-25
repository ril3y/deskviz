using DeskViz.Plugins.Base;

namespace DeskViz.Widgets.Logo
{
    public class LogoWidgetSettings : BaseWidgetSettings
    {
        public override string WidgetId => "LogoWidget";

        public string ImagePath { get; set; } = "";
        public double? ImageWidth { get; set; }
        public double? ImageHeight { get; set; }
        public string Stretch { get; set; } = "Uniform";
        public string HorizontalAlignment { get; set; } = "Center";
        public string VerticalAlignment { get; set; } = "Center";
        public double UpdateIntervalSeconds { get; set; } = 60;

        public override object Clone()
        {
            return new LogoWidgetSettings
            {
                ImagePath = this.ImagePath,
                ImageWidth = this.ImageWidth,
                ImageHeight = this.ImageHeight,
                Stretch = this.Stretch,
                HorizontalAlignment = this.HorizontalAlignment,
                VerticalAlignment = this.VerticalAlignment,
                UpdateIntervalSeconds = this.UpdateIntervalSeconds
            };
        }

        public override void Reset()
        {
            ImagePath = "";
            ImageWidth = null;
            ImageHeight = null;
            Stretch = "Uniform";
            HorizontalAlignment = "Center";
            VerticalAlignment = "Center";
            UpdateIntervalSeconds = 60;
        }

        protected override BaseWidgetSettings CreateDefault() => new LogoWidgetSettings();
    }
}
