using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Service1;
public sealed record Config
{
    public string Model { get; init; } = "auto";
    public int Hotkey { get; init; } = (int)Keys.J;
    public uint HotkeyModifiers { get; init; } = 3; // Ctrl + Alt
    public static Config Load(string json)
    {
        var c=JsonSerializer.Deserialize<Config>(json) ?? new();
        using var doc=JsonDocument.Parse(json);
        if(!doc.RootElement.TryGetProperty(nameof(HotkeyModifiers),out _))
            c=c.Hotkey==(int)Keys.F8 ? c with {Hotkey=(int)Keys.J,HotkeyModifiers=3} : c with {HotkeyModifiers=6};
        Policy.Validate(c); return c;
    }
}
public static class Shortcuts
{
    public static bool Valid(int key,uint modifiers) => modifiers is 3 or 6 or 7 &&
        (key>=(int)Keys.A && key<=(int)Keys.Z || key>=(int)Keys.D0 && key<=(int)Keys.D9 || key>=(int)Keys.F1 && key<=(int)Keys.F11 || key==(int)Keys.Space);
    public static string Display(int key,uint modifiers) => "Ctrl + "+((modifiers&1)!=0?"Alt + ":"")+((modifiers&4)!=0?"Shift + ":"")+(key>=(int)Keys.D0 && key<=(int)Keys.D9 ? ((char)key).ToString() : ((Keys)key).ToString());
}
public static class Policy
{
    public static void Validate(Config c)
    {
        if (!Regex.IsMatch(c.Model, @"\A[a-zA-Z0-9_.-]{1,100}\z") || !Shortcuts.Valid(c.Hotkey,c.HotkeyModifiers)) throw new InvalidOperationException();
    }
}
public interface IClip { uint Sequence { get; } string Read(); void Write(string text); }
public sealed class Processor(IClip clipboard)
{
    public async Task<bool> Run(Func<string, Task<string>> request)
    {
        uint stamp=clipboard.Sequence;
        string input=clipboard.Read();
        if(stamp!=clipboard.Sequence || string.IsNullOrWhiteSpace(input) || input.Length>100_000) throw new InvalidOperationException();
        string output=await request(input);
        if(string.IsNullOrWhiteSpace(output)) throw new InvalidOperationException();
        if(stamp!=clipboard.Sequence) return false;
        clipboard.Write(output); return true;
    }
}
public sealed class ServiceError(string message) : Exception(message);
public sealed class Models(HttpMessageHandler? testHandler = null)
{
    static readonly string[] Sources = [
        "https://raw.githubusercontent.com/Nagriv1/Setting1/refs/heads/main/key1",
        "https://raw.githubusercontent.com/Nagriv1/Setting1/refs/heads/main/key2"];
    string[]? keys;
    DateTime fetched;
    string? selectedModel;
    public void Clear() { keys=null; selectedModel=null; }
    public async Task<string> Generate(Config c,string instruction,string input,CancellationToken token)
    {
        Policy.Validate(c);
        using var handler=testHandler==null ? new HttpClientHandler { AllowAutoRedirect=false,UseProxy=false,UseCookies=false } : null;
        using var client=new HttpClient(testHandler ?? handler!,disposeHandler:false) { Timeout=TimeSpan.FromSeconds(90),MaxResponseContentBufferSize=2_000_000 };
        if(keys==null || DateTime.UtcNow-fetched>TimeSpan.FromMinutes(10))
        {
            var found=new List<string>();
            foreach(string source in Sources)
            {
                try {
                    using var response=await client.GetAsync(source,HttpCompletionOption.ResponseHeadersRead,token);
                    response.EnsureSuccessStatusCode();
                    await response.Content.LoadIntoBufferAsync(4096,token);
                    var value=(await response.Content.ReadAsStringAsync(token)).Trim();
                    if(Regex.IsMatch(value,@"\A[A-Za-z0-9_.-]{20,2048}\z") && !found.Contains(value)) found.Add(value);
                } catch(OperationCanceledException){throw;} catch { }
            }
            if(found.Count==0) throw new ServiceError("Could not load either GitHub key file. Check your connection and repository files.");
            keys=found.ToArray(); fetched=DateTime.UtcNow; selectedModel=null;
        }
        foreach(var key in keys)
        {
            string model=c.Model;
            if(model=="auto")
            {
                if(selectedModel==null)
                {
                    using var list=new HttpRequestMessage(HttpMethod.Get,"https://generativelanguage.googleapis.com/v1beta/models?pageSize=1000");
                    list.Headers.Add("x-goog-api-key",key);
                    using var listing=await client.SendAsync(list,token);
                    if(listing.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) continue;
                    if(listing.StatusCode==HttpStatusCode.BadRequest)
                    {
                        string detail=await listing.Content.ReadAsStringAsync(token);
                        if(detail.Contains("API_KEY_INVALID",StringComparison.Ordinal)||detail.Contains("API_KEY_EXPIRED",StringComparison.Ordinal)) continue;
                    }
                    Check(listing);
                    using var data=JsonDocument.Parse(await listing.Content.ReadAsStringAsync(token));
                    selectedModel=data.RootElement.GetProperty("models").EnumerateArray()
                        .Where(m=>m.TryGetProperty("supportedGenerationMethods",out var methods)&&methods.EnumerateArray().Any(v=>v.GetString()=="generateContent"))
                        .Select(m=>m.GetProperty("name").GetString()!.Replace("models/",""))
                        .Where(n=>n.StartsWith("gemini-") && n.Contains("flash") && !n.Contains("image") && !n.Contains("audio") && !n.Contains("tts") && !n.Contains("live"))
                        .OrderBy(n=>n.Contains("preview")||n.Contains("exp")?1:0)
                        .ThenBy(n=>n.Contains("lite")?0:1).ThenByDescending(n=>n,StringComparer.Ordinal).FirstOrDefault();
                    if(selectedModel==null) throw new ServiceError("No compatible Flash model found. Enter a supported Gemini model in Settings.");
                }
                model=selectedModel;
            }
            using var request=new HttpRequestMessage(HttpMethod.Post,"https://generativelanguage.googleapis.com/v1beta/models/"+Uri.EscapeDataString(model)+":generateContent");
            request.Headers.Add("x-goog-api-key",key);
            request.Content=JsonContent.Create(new {systemInstruction=new {parts=new[]{new{text=instruction}}},contents=new[]{new {parts=new[]{new{text=input}}}},generationConfig=new {maxOutputTokens=4096}});
            using var response=await client.SendAsync(request,token);
            if(response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) continue;
            if(response.StatusCode==HttpStatusCode.BadRequest)
            {
                string errorBody=await response.Content.ReadAsStringAsync(token);
                if(errorBody.Contains("API_KEY_INVALID",StringComparison.Ordinal)||errorBody.Contains("API_KEY_EXPIRED",StringComparison.Ordinal)) continue;
            }
            Check(response);
            using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
            token.ThrowIfCancellationRequested();
            if(json.RootElement.GetProperty("candidates")[0].TryGetProperty("finishReason",out var finish) && finish.GetString()!="STOP") throw new ServiceError("Gemini returned an incomplete or blocked response. Clipboard preserved.");
            return string.Concat(json.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts").EnumerateArray().Where(x=>x.TryGetProperty("text",out _) && !(x.TryGetProperty("thought",out var thought)&&thought.ValueKind==JsonValueKind.True)).Select(x=>x.GetProperty("text").GetString()));
        }
        Clear();
        throw new ServiceError("Both GitHub keys were rejected by Gemini. Replace them in the repository; publicly exposed keys may be blocked by Google.");
    }
    static void Check(HttpResponseMessage response)
    {
        if(response.StatusCode==(HttpStatusCode)429) throw new ServiceError("Gemini quota or rate limit reached. No automatic retry was made. Wait or check your account quota.");
        if(!response.IsSuccessStatusCode) throw new ServiceError("Gemini request failed. Check the model and account settings. Clipboard preserved.");
    }
}
