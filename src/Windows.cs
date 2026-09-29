using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace Service1;
internal static class Native
{
    [DllImport("user32.dll")] internal static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr window, int id);
    [DllImport("user32.dll")] internal static extern uint GetClipboardSequenceNumber();
    [DllImport("user32.dll")] internal static extern bool DestroyIcon(IntPtr icon);
}
internal sealed class WindowsClip : IClip
{
    public uint Sequence => Native.GetClipboardSequenceNumber();
    public string Read() => Clipboard.ContainsText() ? Clipboard.GetText() : "";
    public void Write(string text) => Clipboard.SetText(text);
}
internal sealed class Hotkey : NativeWindow, IDisposable
{
    public event Action? Pressed;
    public Hotkey() => CreateHandle(new CreateParams { Caption = "Service1 hotkey", Parent = new IntPtr(-3) });
    public bool Set(int key)
    {
        Native.UnregisterHotKey(Handle, 1);
        return Native.RegisterHotKey(Handle, 1, 0x4000 | 2 | 4, (uint)key); // Ctrl+Shift, no repeat
    }
    protected override void WndProc(ref Message m) { if (m.Msg == 0x312) Pressed?.Invoke(); base.WndProc(ref m); }
    public void Dispose() { Native.UnregisterHotKey(Handle, 1); DestroyHandle(); }
}
internal static class Startup
{
    const string Key = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public static bool Enabled { get { using var k = Registry.CurrentUser.OpenSubKey(Key); return k?.GetValue("Service1") is string; } }
    public static void Set(bool enabled)
    {
        using var k = Registry.CurrentUser.CreateSubKey(Key);
        if (enabled) k.SetValue("Service1", "\"" + Environment.ProcessPath + "\""); else k.DeleteValue("Service1", false);
    }
}
