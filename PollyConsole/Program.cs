using Newtonsoft.Json;
using Polly;
using Polly.CircuitBreaker;
using Service.Framework.Model;


#region 超时策略
//var memberJson = await Policy.TimeoutAsync(5, Polly.Timeout.TimeoutStrategy.Optimistic, (t, s, y) =>
//{
//    Console.WriteLine("超时了~~~~");
//    return Task.CompletedTask;
//}).ExecuteAsync(async () =>
//{
//    // 业务逻辑
//    using var httpClient = new HttpClient();
//    httpClient.BaseAddress = new Uri($"http://localhost:5179");
//    var memberResult = await httpClient.GetAsync("/api/polly/timeout");
//    memberResult.EnsureSuccessStatusCode();
//    var json = await memberResult.Content.ReadAsStringAsync();
//    Console.WriteLine(json);

//    return json;
//});
#endregion

#region Polly 重试策略
//当发生 HttpRequestException 的时候触发 RetryAsync 重试，并且最多重试3次。
//var retry = await Policy.Handle<HttpRequestException>().RetryAsync(3, (t, s, y) =>
//                    {
//                        Console.WriteLine($"开始第 {s} 次重试：");
//                    })
//                    .ExecuteAsync(async () =>
//                    {
//                        Console.WriteLine("执行中.....");
//                        using var httpClient = new HttpClient();
//                        httpClient.BaseAddress = new Uri($"http://localhost:5179");
//                        var memberResult = await httpClient.GetAsync("/polly/1");
//                        memberResult.EnsureSuccessStatusCode();
//                        var json = await memberResult.Content.ReadAsStringAsync();

//                        return json;
//                    });

//使用 Polly 在出现当请求结果为 http status_code 500 的时候进行3次重试。 HttpResponseMessage - 响应结果
//var retryReult = await Policy.HandleResult<HttpResponseMessage>
//    (x => (int)x.StatusCode == 500).RetryAsync(3, (t, s, y) =>
//                    {
//                        Console.WriteLine($"开始第 {s} 次重试：");
//                    }).ExecuteAsync(async () =>
//{
//    Thread.Sleep(1000);
//    Console.WriteLine("响应状态码重试中.....");
//    using var httpClient = new HttpClient();
//    httpClient.BaseAddress = new Uri($"http://localhost:5179");
//    var memberResult = await httpClient.GetAsync("/api/polly/500");

//    return memberResult;
//});
#endregion

#region 服务降级
// 首先我们使用 Policy 的 FallbackAsync("FALLBACK") 方法设置降级的返回值。当我们服务需要降级的时候会返回 "FALLBACK" 的固定值。
// 同时使用 WrapAsync 方法把重试策略包裹起来。这样我们就可以达到当服务调用失败的时候重试3次，如果重试依然失败那么返回值降级为固定的 "FALLBACK" 值。
//var fallback = Policy<string>.Handle<HttpRequestException>().Or<Exception>().FallbackAsync("FALLBACK", (x) =>
//{
//    Console.WriteLine($"进行了服务降级 -- {x.Exception.Message}");
//    return Task.CompletedTask;
//}).WrapAsync(Policy.Handle<HttpRequestException>().RetryAsync(3));

//var downgrade = await fallback.ExecuteAsync(async () =>
//{
//    using var httpClient = new HttpClient();
//    httpClient.BaseAddress = new Uri("http://localhost:5179");
//    var result = await httpClient.GetAsync("/api/user/" + 1);
//    result.EnsureSuccessStatusCode();
//    var json = await result.Content.ReadAsStringAsync();
//    return json;

//});
//Console.WriteLine(downgrade);
//if (downgrade != "FALLBACK")
//{
//    var member = JsonConvert.DeserializeObject<User>(downgrade);
//    Console.WriteLine($"{member!.Id}---{member.Name}");
//}
#endregion

#region 服务熔断
//定义熔断策略
var circuitBreaker = Policy.Handle<Exception>().CircuitBreakerAsync(
   exceptionsAllowedBeforeBreaking: 2, // 出现几次异常就熔断
   durationOfBreak: TimeSpan.FromSeconds(10), // 熔断10秒    （出现2次异常就熔断10秒）
   onBreak: (ex, ts) =>
   {
       Console.WriteLine("打开断路器."); // 打开断路器
   },
   onReset: () =>
   {
       Console.WriteLine("关闭断路器"); 
   },
   onHalfOpen: () =>
   {
       Console.WriteLine("断路器半开（尝试放一部分请求去服务端）"); 
   }
);

// 定义重试策略  （默认悲观）
var retry = Policy.Handle<HttpRequestException>().RetryAsync(3, (t, s, y) =>
                    {
                        Console.WriteLine($"开始第 {s} 次重试：");
                    });
// 定义降级策略
var fallbackPolicy = Policy<string>.Handle<HttpRequestException>().Or<BrokenCircuitException>()
    .FallbackAsync("FALLBACK", (x) =>
    {
        Console.WriteLine($"服务降级 -- {x.Exception.Message}");
        return Task.CompletedTask;
    })
    .WrapAsync(circuitBreaker.WrapAsync(retry));//降级策略可以包裹熔断策略，熔断策略又可以包裹重试策略  执行循序 （先执行重试-熔断-降级）
string memberJsonResult = "";

do
{
    memberJsonResult = await fallbackPolicy.ExecuteAsync(async () =>
    {
        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri($"http://localhost:5179");
        var result = await httpClient.GetAsync("/api/user/" + 1);
        result.EnsureSuccessStatusCode();
        var json = await result.Content.ReadAsStringAsync();
        return json;
    });
    Thread.Sleep(1000);
} while (memberJsonResult == "FALLBACK");

if (memberJsonResult != "FALLBACK")
{
    var member = JsonConvert.DeserializeObject<User>(memberJsonResult);
    Console.WriteLine($"{member!.Id}---{member.Name}");
}
#endregion


Console.WriteLine("Hello，World");