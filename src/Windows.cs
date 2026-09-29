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
    int activeId, currentKey;
    uint currentModifiers;
    public event Action? Pressed;
    public void Suspend(){if(activeId!=0)Native.UnregisterHotKey(Handle,activeId);activeId=0;}
    public Hotkey() => CreateHandle(new CreateParams { Caption = "Service1 hotkey", Parent = new IntPtr(-3) });
    public bool Set(int key,uint modifiers)
    {
        if(activeId!=0 && key==currentKey && modifiers==currentModifiers) return true;
        int next=activeId==1?2:1;
        if(!Native.RegisterHotKey(Handle,next,0x4000|modifiers,(uint)key)) return false;
        if(activeId!=0) Native.UnregisterHotKey(Handle,activeId);
        activeId=next;currentKey=key;currentModifiers=modifiers;return true;
    }
    protected override void WndProc(ref Message m) { if (m.Msg == 0x312) Pressed?.Invoke(); base.WndProc(ref m); }
    public void Dispose() { if(activeId!=0) Native.UnregisterHotKey(Handle, activeId); DestroyHandle(); }
}
internal sealed class ShortcutRecorder : TextBox
{
    public int ShortcutKey {get;private set;}
    public uint ShortcutModifiers {get;private set;}
    public bool Recording {get;private set;}
    public event Action<string>? Feedback;
    public event Action<bool>? RecordingChanged;
    public ShortcutRecorder(int key,uint modifiers){ReadOnly=true;ShortcutsSet(key,modifiers);AccessibleName="Custom shortcut";}
    public void ShortcutsSet(int key,uint modifiers){bool wasRecording=Recording;ShortcutKey=key;ShortcutModifiers=modifiers;Recording=false;Text=Shortcuts.Display(key,modifiers);if(wasRecording)RecordingChanged?.Invoke(false);}
    public void Begin(){Recording=true;RecordingChanged?.Invoke(true);Text="Press your shortcut (Esc cancels)";Focus();}
    protected override bool ProcessCmdKey(ref Message msg,Keys keyData)
    {
        if(!Recording)return base.ProcessCmdKey(ref msg,keyData);
        var key=keyData&Keys.KeyCode;
        if(key==Keys.Escape){ShortcutsSet(ShortcutKey,ShortcutModifiers);Feedback?.Invoke("Recording cancelled.");return true;}
        if(key is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin)return true;
        uint modifiers=((keyData&Keys.Control)!=0?2u:0u)|((keyData&Keys.Alt)!=0?1u:0u)|((keyData&Keys.Shift)!=0?4u:0u);
        if(!Shortcuts.Valid((int)key,modifiers)){Feedback?.Invoke("Hold Ctrl plus Alt or Shift, then press a letter, number, Space or F1-F11.");return true;}
        ShortcutsSet((int)key,modifiers);Feedback?.Invoke("Shortcut captured. Click Save to apply it.");return true;
    }
    protected override void OnLostFocus(EventArgs e){if(Recording)ShortcutsSet(ShortcutKey,ShortcutModifiers);base.OnLostFocus(e);}
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
