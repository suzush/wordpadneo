namespace wordpadneo.Models
{
    public class PageSettings
    {
        public double PageWidthCm { get; set; } = 21.0;
        public double PageHeightCm { get; set; } = 29.7;

        public double MarginLeftCm { get; set; } = 3.0;
        public double MarginRightCm { get; set; } = 3.0;
        public double MarginTopCm { get; set; } = 3.0;
        public double MarginBottomCm { get; set; } = 3.0;

        public double PrintableWidthCm => PageWidthCm - MarginLeftCm - MarginRightCm;
    }
}
