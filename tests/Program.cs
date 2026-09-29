using Service1;
using System.Net;
int passed=0;
void Assert(bool condition){if(!condition)throw new Exception("Assertion failed");passed++;}
Assert(new Config().Hotkey==(int)System.Windows.Forms.Keys.J && new Config().HotkeyModifiers==3);
Assert(Shortcuts.Valid((int)System.Windows.Forms.Keys.K,6));
Assert(!Shortcuts.Valid((int)System.Windows.Forms.Keys.J,0));
Assert(!Shortcuts.Valid((int)System.Windows.Forms.Keys.Delete,3));
Assert(!Shortcuts.Valid((int)System.Windows.Forms.Keys.F12,3));
Assert(Config.Load("{\"Model\":\"auto\",\"Hotkey\":119}").Hotkey==(int)System.Windows.Forms.Keys.J);
Assert(Config.Load("{\"Model\":\"auto\",\"Hotkey\":118}").HotkeyModifiers==6);
Assert(Config.Load("{\"Model\":\"auto\",\"Hotkey\":75,\"HotkeyModifiers\":6}").Hotkey==75);
var clip=new FakeClip();var processor=new Processor(clip);
try{await processor.Run(_=>throw new HttpRequestException());}catch(HttpRequestException){}
Assert(clip.Text=="original" && clip.Writes==0);
try{await processor.Run(_=>Task.FromResult(""));}catch(InvalidOperationException){}
Assert(clip.Writes==0);
Assert(!await processor.Run(_=>{clip.Text="new copy";clip.Stamp++;return Task.FromResult("old response");}));
Assert(clip.Text=="new copy");
Assert(await processor.Run(_=>Task.FromResult("success")));
Assert(clip.Text=="success");
foreach(var code in new[]{200,403,429}){
 var handler=new FakeHttp(code);var service=new Models(handler);bool failed=false;
 try{Assert(await service.Generate(new Config(),"instruction","sample",CancellationToken.None)=="answer");}catch(ServiceError){failed=true;}
 Assert(failed==(code!=200));Assert(handler.Downloads==2);
 Assert(handler.Posts==(code==403?2:1));
 if(code==200){await service.Generate(new Config(),"instruction","sample",CancellationToken.None);Assert(handler.Downloads==2);}
}
Exception? nativeFailure=null;
var thread=new Thread(()=>{
 try{
  using var first=new Hotkey();using var second=new Hotkey();
  if(!first.Set((int)System.Windows.Forms.Keys.F10,7)||!second.Set((int)System.Windows.Forms.Keys.F11,7))throw new Exception("Test shortcuts unavailable");
  Assert(!first.Set((int)System.Windows.Forms.Keys.F11,7));
  Assert(!second.Set((int)System.Windows.Forms.Keys.F10,7));
  first.Suspend();Assert(second.Set((int)System.Windows.Forms.Keys.F10,7));
  using var recorder=new ShortcutRecorder((int)System.Windows.Forms.Keys.J,3);
  recorder.Begin();Assert(recorder.Recording);
  var method=typeof(ShortcutRecorder).GetMethod("ProcessCmdKey",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
  var args=new object[]{new System.Windows.Forms.Message(),System.Windows.Forms.Keys.Control|System.Windows.Forms.Keys.Shift|System.Windows.Forms.Keys.K};
  Assert((bool)method.Invoke(recorder,args)!);
  Assert(!recorder.Recording && recorder.ShortcutKey==(int)System.Windows.Forms.Keys.K && recorder.ShortcutModifiers==6);
  recorder.Begin();args[1]=System.Windows.Forms.Keys.Escape;method.Invoke(recorder,args);
  Assert(!recorder.Recording && recorder.ShortcutKey==(int)System.Windows.Forms.Keys.K);
 }catch(Exception e){nativeFailure=e;}
});thread.SetApartmentState(ApartmentState.STA);thread.Start();thread.Join();if(nativeFailure!=null)throw nativeFailure;
Console.WriteLine($"PASS: {passed} assertions; no real network or paid model calls.");
sealed class FakeClip:IClip{public string Text="original";public uint Stamp=1;public int Writes;public uint Sequence=>Stamp;public string Read()=>Text;public void Write(string text){Text=text;Stamp++;Writes++;}}
sealed class FakeHttp(int status):HttpMessageHandler{
 public int Downloads,Posts;
 protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken t){
  if(r.RequestUri!.Host=="raw.githubusercontent.com"){Downloads++;return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(new string('x',30)+Downloads)});}
  if(!r.Headers.Contains("x-goog-api-key"))throw new Exception("Missing key header");
  if(r.Method==HttpMethod.Get)return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("{\"models\":[{\"name\":\"models/gemini-test-flash-lite\",\"supportedGenerationMethods\":[\"generateContent\"]}]}")});
  Posts++;return Task.FromResult(new HttpResponseMessage((HttpStatusCode)status){Content=new StringContent("{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"answer\"}]}}]}")});
 }
}
