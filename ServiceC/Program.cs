using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Extras.DynamicProxy;
using Service.Framework.PollyExtend;
using ServiceC.Service;

var builder = WebApplication.CreateBuilder(args);

#region ʹ��Autofac
{
    builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
    builder.Host.ConfigureContainer<ContainerBuilder>((context, buider) =>
    {
        // ����ʹ�õ���ע��
        buider.RegisterType<UserService>()
        .As<IUserService>().SingleInstance().EnableInterfaceInterceptors();

        // buider.RegisterType<OrderService>().As<IOrderService>();

        buider.RegisterType<PollyPolicyAttribute>();

    });
}
#endregion 

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();
