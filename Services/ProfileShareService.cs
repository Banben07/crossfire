using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using CrossfireCrosshair.Models;

namespace CrossfireCrosshair.Services;

public sealed class ProfileShareService
{
    private const string SharePrefix = "CCX1-";

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public string Export(CrosshairProfile profile)
    {
        ShareProfileV1 payload = new()
        {
            Version = 1,
            Name = profile.Name,
            ShowLines = profile.ShowLines,
            ShowCenterDot = profile.ShowCenterDot,
            TStyle = profile.TStyle,
            ShowOutline = profile.ShowOutline,
            LineLength = profile.LineLength,
            LineThickness = profile.LineThickness,
            Gap = profile.Gap,
            DotSize = profile.DotSize,
            OutlineThickness = profile.OutlineThickness,
            ColorHex = profile.ColorHex,
            OutlineColorHex = profile.OutlineColorHex,
            Opacity = profile.Opacity,
            OffsetX = profile.OffsetX,
            OffsetY = profile.OffsetY,
            DynamicSpread = profile.DynamicSpread,
            KeyPressSpreadEnabled = profile.KeyPressSpreadEnabled,
            KeyPressSpreadAmount = profile.KeyPressSpreadAmount
        };

        string json = JsonSerializer.Serialize(payload, _jsonOptions);
        byte[] utf8 = Encoding.UTF8.GetBytes(json);
        byte[] compressed = Compress(utf8);
        return SharePrefix + ToBase64Url(compressed);
    }

    public bool TryImport(string shareCode, out CrosshairProfile? profile, out string? error)
    {
        profile = null;
        error = null;

        if (string.IsNullOrWhiteSpace(shareCode))
        {
            error = "分享码为空。";
            return false;
        }

        string raw = shareCode.Trim();
        if (raw.StartsWith(SharePrefix, StringComparison.OrdinalIgnoreCase))
        {
            raw = raw[SharePrefix.Length..];
        }

        if (!TryFromBase64Url(raw, out byte[] compressed))
        {
            error = "分享码格式无效。";
            return false;
        }

        if (compressed.Length > 8192)
        {
            error = "分享码体积过大。";
            return false;
        }

        try
        {
            byte[] jsonBytes = Decompress(compressed, 65536);
            string json = Encoding.UTF8.GetString(jsonBytes);
            ShareProfileV1? payload = JsonSerializer.Deserialize<ShareProfileV1>(json, _jsonOptions);
            if (payload is null || payload.Version != 1)
            {
                error = "不支持的分享码版本。";
                return false;
            }

            profile = Sanitize(payload);
            return true;
        }
        catch (Exception ex)
        {
            error = $"分享码解析失败：{ex.Message}";
            return false;
        }
    }

    private static CrosshairProfile Sanitize(ShareProfileV1 payload)
    {
        string name = string.IsNullOrWhiteSpace(payload.Name) ? "导入配置" : payload.Name.Trim();
        if (name.Length > 64)
        {
            name = name[..64];
        }

        return new CrosshairProfile
        {
            Name = name,
            ShowLines = payload.ShowLines,
            ShowCenterDot = payload.ShowCenterDot,
            TStyle = payload.TStyle,
            ShowOutline = payload.ShowOutline,
            LineLength = Math.Clamp(payload.LineLength, 0, 30),
            LineThickness = Math.Clamp(payload.LineThickness, 0.5, 8),
            Gap = Math.Clamp(payload.Gap, -10, 25),
            DotSize = Math.Clamp(payload.DotSize, 0, 12),
            OutlineThickness = Math.Clamp(payload.OutlineThickness, 0, 5),
            ColorHex = NormalizeHexColor(payload.ColorHex, "#00FF66"),
            OutlineColorHex = NormalizeHexColor(payload.OutlineColorHex, "#000000"),
            Opacity = Math.Clamp(payload.Opacity, 0.1, 1.0),
            OffsetX = Math.Clamp(payload.OffsetX, -120, 120),
            OffsetY = Math.Clamp(payload.OffsetY, -120, 120),
            DynamicSpread = Math.Clamp(payload.DynamicSpread, 0, 20),
            KeyPressSpreadEnabled = payload.KeyPressSpreadEnabled,
            KeyPressSpreadAmount = Math.Clamp(payload.KeyPressSpreadAmount, 0, 20)
        };
    }

    private static byte[] Compress(byte[] data)
    {
        using MemoryStream output = new();
        using (GZipStream gzip = new(output, CompressionLevel.SmallestSize, true))
        {
            gzip.Write(data, 0, data.Length);
        }

        return output.ToArray();
    }

    private static byte[] Decompress(byte[] data, int maxOutputBytes)
    {
        using MemoryStream input = new(data);
        using GZipStream gzip = new(input, CompressionMode.Decompress, leaveOpen: false);
        using MemoryStream output = new();
        byte[] buffer = new byte[4096];
        int total = 0;

        while (true)
        {
            int read = gzip.Read(buffer, 0, buffer.Length);
            if (read == 0)
            {
                break;
            }

            total += read;
            if (total > maxOutputBytes)
            {
                throw new InvalidOperationException("分享码内容过大。");
            }

            output.Write(buffer, 0, read);
        }

        return output.ToArray();
    }

    private static string ToBase64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static bool TryFromBase64Url(string input, out byte[] bytes)
    {
        bytes = [];
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        string padded = input
            .Replace('-', '+')
            .Replace('_', '/');

        int remainder = padded.Length % 4;
        if (remainder == 2)
        {
            padded += "==";
        }
        else if (remainder == 3)
        {
            padded += "=";
        }
        else if (remainder == 1)
        {
            return false;
        }

        try
        {
            bytes = Convert.FromBase64String(padded);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string NormalizeHexColor(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        string normalized = value.Trim();
        if (!normalized.StartsWith('#'))
        {
            normalized = $"#{normalized}";
        }

        if (normalized.Length != 7)
        {
            return fallback;
        }

        for (int i = 1; i < normalized.Length; i++)
        {
            char ch = normalized[i];
            bool isHex = (ch >= '0' && ch <= '9') ||
                         (ch >= 'a' && ch <= 'f') ||
                         (ch >= 'A' && ch <= 'F');
            if (!isHex)
            {
                return fallback;
            }
        }

        return normalized.ToUpperInvariant();
    }

    private sealed class ShareProfileV1
    {
        public int Version { get; set; } = 1;
        public string Name { get; set; } = "导入配置";
        public bool ShowLines { get; set; } = true;
        public bool ShowCenterDot { get; set; }
        public bool TStyle { get; set; }
        public bool ShowOutline { get; set; } = true;
        public double LineLength { get; set; } = 8;
        public double LineThickness { get; set; } = 2;
        public double Gap { get; set; } = 4;
        public double DotSize { get; set; } = 2;
        public double OutlineThickness { get; set; } = 1;
        public string ColorHex { get; set; } = "#00FF66";
        public string OutlineColorHex { get; set; } = "#000000";
        public double Opacity { get; set; } = 1;
        public double OffsetX { get; set; }
        public double OffsetY { get; set; }
        public double DynamicSpread { get; set; }
        public bool KeyPressSpreadEnabled { get; set; }
        public double KeyPressSpreadAmount { get; set; } = 3;
    }
}
