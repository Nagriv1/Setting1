using Service1;
using System.Net;
int passed=0;
void Assert(bool condition){if(!condition)throw new Exception("Assertion failed");passed++;}
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
