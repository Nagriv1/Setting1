using System.Text.Json;

namespace Service1;
internal static class Program
{
    [STAThread] static void Main(string[] args)
    {
        using var single = new Mutex(true, @"Local\Service1.Tray", out bool first);
        if (!first) return;
        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApp());
    }
}
internal sealed class TrayApp : ApplicationContext
{
    static readonly string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Service1", "Config");
    static readonly string ConfigPath = Path.Combine(Folder, "gemini-settings.json");
    Config config = new();
    readonly Models models = new();
    readonly NotifyIcon tray;
    readonly Hotkey hotkey = new();
    readonly Icon icon;
    bool paused, busy;
    string status = "Ready. Gemini keys load from GitHub on your first explicit action.";
    CancellationTokenSource? pending;
    Form? settings, mini;
    public TrayApp()
    {
        try { if (File.Exists(ConfigPath)) config = Config.Load(File.ReadAllText(ConfigPath)); }
        catch { status = "Settings could not be read. Default Gemini settings are active."; }
        icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!)!;
        var menu = new ContextMenuStrip();
        var pause = new ToolStripMenuItem("Pause") { CheckOnClick = true };
        pause.CheckedChanged += (_, _) => { paused = pause.Checked; pending?.Cancel(); tray!.Text = paused ? "Service1 — Paused" : "Service1"; };
        menu.Items.Add(pause);
        menu.Items.Add("Process clipboard", null, (_, _) => _ = Process("Improve the clipboard text for clarity. Preserve meaning. Return only the replacement text."));
        menu.Items.Add("Settings", null, (_, _) => OpenSettings());
        menu.Items.Add("Open Mini Prompt", null, (_, _) => OpenMini());
        var startup = new ToolStripMenuItem("Start with Windows") { Checked = Startup.Enabled };
        startup.Click += (_, _) => { try { Startup.Set(!Startup.Enabled); startup.Checked = Startup.Enabled; } catch { status = "Could not change startup. Check Windows policy permissions in Settings."; } };
        menu.Items.Add(startup);
        menu.Opening += (_, _) => startup.Checked = Startup.Enabled;
        menu.Items.Add("Exit", null, (_, _) => ExitThread());
        tray = new NotifyIcon { Icon = icon, Text = "Service1", ContextMenuStrip = menu, Visible = true };
        tray.DoubleClick += (_, _) => OpenSettings();
        hotkey.Pressed += () => { if(settings==null) _ = Process("Improve the clipboard text for clarity. Preserve meaning. Return only the replacement text."); };
        if (!hotkey.Set(config.Hotkey,config.HotkeyModifiers)) status = "Hotkey unavailable. Select another hotkey in Settings.";
    }
    async Task Process(string instruction)
    {
        if (paused || busy) return;
        busy = true;
        pending = new CancellationTokenSource();
        try
        {
            Policy.Validate(config);
            bool replaced = await new Processor(new WindowsClip()).Run(text => models.Generate(config, instruction, text, pending.Token));
            status = replaced ? "Last request succeeded." : "Clipboard changed during processing; the newer clipboard was preserved.";
        }
        catch (ServiceError error) { status = error.Message; }
        catch (OperationCanceledException) { status = "Request cancelled or timed out. Original clipboard preserved."; }
        catch { status = "Request failed. Clipboard preserved. Check your connection, GitHub key files and Gemini model settings."; }
        finally { pending.Dispose(); pending = null; busy = false; }
    }
    static Form Window(string title, int height) => new() { Text = title, ClientSize = new Size(580, height), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, ShowInTaskbar = false, StartPosition = FormStartPosition.CenterScreen, AutoScaleMode = AutoScaleMode.Dpi };
    void OpenSettings()
    {
        if(settings!=null){settings.Activate();return;}
        var f=settings=Window("Service1 Settings - Gemini",510); f.Icon=icon;
        var intro=new Label {Text="GEMINI CLOUD ONLY\nClipboard text is sent to Google after your hotkey.\nDefault keys: Nagriv1/Setting1 - key1 and key2 (public).",Location=new(20,20),Size=new(540,65)};
        var model=new TextBox {Text=config.Model,Location=new(190,110),Width=360};
        var shortcut=new ShortcutRecorder(config.Hotkey,config.HotkeyModifiers) {Location=new(190,160),Width=360};
        var record=new Button {Text="Record shortcut",Location=new(190,195),Width=170};
        var reset=new Button {Text="Use Ctrl + Alt + J",Location=new(370,195),Width=180};
        record.Click+=(_,_)=>shortcut.Begin();
        reset.Click+=(_,_)=>shortcut.ShortcutsSet((int)Keys.J,3);
        var start=new CheckBox {Text="Start with Windows (current user)",Checked=Startup.Enabled,Location=new(20,245),AutoSize=true};
        var note=new Label {Text="Model 'auto' prefers a supported Flash Lite model.\nKeys stay in memory for up to 10 minutes. Save refreshes them on\nthe next action. No history, notifications or automatic retries on quota errors.",Location=new(20,285),Size=new(540,65)};
        var state=new Label {Text=status,Location=new(20,355),Size=new(540,65)};
        var save=new Button {Text="Save",Location=new(450,460),Width=100};
        shortcut.Feedback+=message=>state.Text=message;
        shortcut.RecordingChanged+=recording=>{if(recording)hotkey.Suspend();else if(!hotkey.Set(config.Hotkey,config.HotkeyModifiers))state.Text="Previous shortcut became unavailable. Choose another or use the tray action.";};
        f.Controls.AddRange([record,reset,intro,new Label {Text="Gemini model",Location=new(20,114),AutoSize=true},model,new Label {Text="Hotkey",Location=new(20,164),AutoSize=true},shortcut,start,note,state,save]);
        save.Click+=(_,_)=>{
            try{
                if(shortcut.Recording){state.Text="Finish recording your shortcut before saving.";return;}
                var next=new Config {Model=model.Text.Trim(),Hotkey=shortcut.ShortcutKey,HotkeyModifiers=shortcut.ShortcutModifiers};Policy.Validate(next);
                if(!hotkey.Set(next.Hotkey,next.HotkeyModifiers)){hotkey.Set(config.Hotkey,config.HotkeyModifiers);state.Text="Hotkey in use. Choose another.";return;}
                pending?.Cancel();models.Clear();
                if(start.Checked!=Startup.Enabled)Startup.Set(start.Checked);
                Directory.CreateDirectory(Folder);File.WriteAllText(ConfigPath+".new",JsonSerializer.Serialize(next));File.Move(ConfigPath+".new",ConfigPath,true);
                config=next;status="Settings saved.";f.Close();
            }catch{hotkey.Set(config.Hotkey,config.HotkeyModifiers);state.Text="Could not save. Check model, hotkey and Windows permissions.";}
        };
        f.FormClosed+=(_,_)=>{settings=null;f.Dispose();};f.Show();
    }
    void OpenMini()
    {
        if (mini != null) { mini.Activate(); return; }
        var f = mini = Window("Service1 Mini Prompt", 240);
        f.Icon = icon;
        var text = new TextBox { Multiline=true, Location=new(20,20), Size=new(540,140), PlaceholderText="One-time instruction for the current clipboard. Not saved." };
        var run = new Button { Text="Process clipboard", Location=new(390,185), Size=new(170,32) };
        run.Click += (_, _) => { if (string.IsNullOrWhiteSpace(text.Text)) return; string instruction=text.Text; text.Clear(); f.Close(); _ = Process(instruction); };
        f.Controls.AddRange([text,run]);
        f.FormClosed += (_, _) => { text.Clear(); mini=null; f.Dispose(); };
        f.Show();
    }
    protected override void ExitThreadCore()
    {
        pending?.Cancel(); settings?.Close(); mini?.Close(); tray.Visible=false; tray.Dispose(); hotkey.Dispose(); icon.Dispose(); base.ExitThreadCore();
    }
}
