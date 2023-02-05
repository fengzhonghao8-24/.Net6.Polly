using Service.Framework.Model;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<ConsulRegisterOptions>(builder.Configuration.GetSection("ConsulRegisterOptions"));
builder.Services.AddConsulRegister();

var app = builder.Build();

app.Services.GetService<IConsulRegister>()!.ConsulRegistAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHealthCheckMiddleware();

app.UseHttpsRedirection();

app.MapGet("/test", (IConfiguration configuration) =>
{
    return $"{Assembly.GetExecutingAssembly().FullName};当前时间：{DateTime.Now:G};Port：{configuration["ConsulRegisterOptions:Port"]}";
});

// 定义超时调用的APi
app.MapGet("/api/polly/timeout", () =>
{
    Thread.Sleep(6000);
    return "Polly Timeout";
});

// 定义500结果的APi
app.MapGet("/api/polly/500", (HttpContext context) =>
{
    context.Response.StatusCode = 500;
    return "fail";
});

// 定义/api/user
app.MapGet("/api/user/1", () =>
{
    var user = new User
    {
        Id = 20001,
        Name = "Mamba24",
    };

    return user;
});

app.Run();

