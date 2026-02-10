using CrossfireCrosshair.Models;

namespace CrossfireCrosshair.Services;

public static class ProfileFactory
{
    public static List<CrosshairProfile> CreatePresetPack()
    {
        return
        [
            CreateCsStyle(),
            CreateValorantStyle(),
            CreateMinimalDot()
        ];
    }

    public static CrosshairProfile CreateCsStyle()
    {
        return new CrosshairProfile
        {
            Name = "CS 经典绿色",
            ShowLines = true,
            ShowCenterDot = false,
            TStyle = false,
            ShowOutline = true,
            LineLength = 8,
            LineThickness = 2,
            Gap = 4,
            DotSize = 2,
            OutlineThickness = 1.1,
            ColorHex = "#00FF66",
            OutlineColorHex = "#000000",
            Opacity = 1,
            OffsetX = 0,
            OffsetY = 0,
            DynamicSpread = 0
        };
    }

    public static CrosshairProfile CreateValorantStyle()
    {
        return new CrosshairProfile
        {
            Name = "瓦罗兰特 青色",
            ShowLines = true,
            ShowCenterDot = true,
            TStyle = false,
            ShowOutline = true,
            LineLength = 7,
            LineThickness = 2,
            Gap = 3,
            DotSize = 2,
            OutlineThickness = 1,
            ColorHex = "#00FFFF",
            OutlineColorHex = "#000000",
            Opacity = 0.95,
            OffsetX = 0,
            OffsetY = 0,
            DynamicSpread = 0
        };
    }

    public static CrosshairProfile CreateMinimalDot()
    {
        return new CrosshairProfile
        {
            Name = "极简中心点",
            ShowLines = false,
            ShowCenterDot = true,
            TStyle = false,
            ShowOutline = true,
            LineLength = 5,
            LineThickness = 2,
            Gap = 3,
            DotSize = 3,
            OutlineThickness = 1,
            ColorHex = "#FFFFFF",
            OutlineColorHex = "#000000",
            Opacity = 1,
            OffsetX = 0,
            OffsetY = 0,
            DynamicSpread = 0
        };
    }
}
