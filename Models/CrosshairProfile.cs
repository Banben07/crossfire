namespace CrossfireCrosshair.Models;

public sealed class CrosshairProfile : ObservableObject
{
    private string _name = "Default";
    private bool _showLines = true;
    private bool _showCenterDot;
    private bool _tStyle;
    private bool _showOutline = true;
    private double _lineLength = 8.0;
    private double _lineThickness = 2.0;
    private double _gap = 4.0;
    private double _dotSize = 2.0;
    private double _outlineThickness = 1.0;
    private string _colorHex = "#00FF66";
    private string _outlineColorHex = "#000000";
    private double _opacity = 1.0;
    private double _offsetX;
    private double _offsetY;
    private double _dynamicSpread;
    private bool _keyPressSpreadEnabled;
    private double _keyPressSpreadAmount = 3.0;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public bool ShowLines
    {
        get => _showLines;
        set => SetProperty(ref _showLines, value);
    }

    public bool ShowCenterDot
    {
        get => _showCenterDot;
        set => SetProperty(ref _showCenterDot, value);
    }

    public bool TStyle
    {
        get => _tStyle;
        set => SetProperty(ref _tStyle, value);
    }

    public bool ShowOutline
    {
        get => _showOutline;
        set => SetProperty(ref _showOutline, value);
    }

    public double LineLength
    {
        get => _lineLength;
        set => SetProperty(ref _lineLength, value);
    }

    public double LineThickness
    {
        get => _lineThickness;
        set => SetProperty(ref _lineThickness, value);
    }

    public double Gap
    {
        get => _gap;
        set => SetProperty(ref _gap, value);
    }

    public double DotSize
    {
        get => _dotSize;
        set => SetProperty(ref _dotSize, value);
    }

    public double OutlineThickness
    {
        get => _outlineThickness;
        set => SetProperty(ref _outlineThickness, value);
    }

    public string ColorHex
    {
        get => _colorHex;
        set => SetProperty(ref _colorHex, value);
    }

    public string OutlineColorHex
    {
        get => _outlineColorHex;
        set => SetProperty(ref _outlineColorHex, value);
    }

    public double Opacity
    {
        get => _opacity;
        set => SetProperty(ref _opacity, value);
    }

    public double OffsetX
    {
        get => _offsetX;
        set => SetProperty(ref _offsetX, value);
    }

    public double OffsetY
    {
        get => _offsetY;
        set => SetProperty(ref _offsetY, value);
    }

    public double DynamicSpread
    {
        get => _dynamicSpread;
        set => SetProperty(ref _dynamicSpread, value);
    }

    public bool KeyPressSpreadEnabled
    {
        get => _keyPressSpreadEnabled;
        set => SetProperty(ref _keyPressSpreadEnabled, value);
    }

    public double KeyPressSpreadAmount
    {
        get => _keyPressSpreadAmount;
        set => SetProperty(ref _keyPressSpreadAmount, value);
    }

    public CrosshairProfile Clone()
    {
        return new CrosshairProfile
        {
            Name = Name,
            ShowLines = ShowLines,
            ShowCenterDot = ShowCenterDot,
            TStyle = TStyle,
            ShowOutline = ShowOutline,
            LineLength = LineLength,
            LineThickness = LineThickness,
            Gap = Gap,
            DotSize = DotSize,
            OutlineThickness = OutlineThickness,
            ColorHex = ColorHex,
            OutlineColorHex = OutlineColorHex,
            Opacity = Opacity,
            OffsetX = OffsetX,
            OffsetY = OffsetY,
            DynamicSpread = DynamicSpread,
            KeyPressSpreadEnabled = KeyPressSpreadEnabled,
            KeyPressSpreadAmount = KeyPressSpreadAmount
        };
    }
}
