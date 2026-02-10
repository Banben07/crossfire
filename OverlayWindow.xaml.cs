using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using CrossfireCrosshair.Models;
using CrossfireCrosshair.Services;
using WpfPoint = System.Windows.Point;

namespace CrossfireCrosshair;

public partial class OverlayWindow : Window
{
    public OverlayWindow()
    {
        InitializeComponent();
    }

    public void SetProfile(CrosshairProfile? profile)
    {
        OverlayCrosshair.Profile = profile;
        OverlayCrosshair.InvalidateVisual();
    }

    public void SetTemporarySpread(double spread)
    {
        OverlayCrosshair.TemporarySpread = Math.Max(0.0, spread);
    }

    public void ApplyMonitor(int targetMonitorIndex)
    {
        Screen[] screens = Screen.AllScreens;
        if (screens.Length == 0)
        {
            return;
        }

        targetMonitorIndex = Math.Clamp(targetMonitorIndex, 0, screens.Length - 1);
        Screen screen = screens[targetMonitorIndex];

        Matrix transform = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
        WpfPoint topLeft = transform.Transform(new WpfPoint(screen.Bounds.Left, screen.Bounds.Top));
        WpfPoint bottomRight = transform.Transform(new WpfPoint(screen.Bounds.Right, screen.Bounds.Bottom));

        Left = topLeft.X;
        Top = topLeft.Y;
        Width = bottomRight.X - topLeft.X;
        Height = bottomRight.Y - topLeft.Y;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        IntPtr handle = new WindowInteropHelper(this).Handle;
        int style = NativeMethods.GetWindowLong(handle, NativeMethods.GWL_EXSTYLE);
        style |= NativeMethods.WS_EX_TRANSPARENT;
        style |= NativeMethods.WS_EX_TOOLWINDOW;
        style |= NativeMethods.WS_EX_NOACTIVATE;

        NativeMethods.SetWindowLong(handle, NativeMethods.GWL_EXSTYLE, style);
        NativeMethods.SetWindowPos(
            handle,
            IntPtr.Zero,
            0,
            0,
            0,
            0,
            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER | NativeMethods.SWP_FRAMECHANGED);
    }
}
