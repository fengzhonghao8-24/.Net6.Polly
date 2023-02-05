using Microsoft.Extensions.DependencyInjection;

namespace Service.Framework.HttpApiExtend
{
    /// <summary>
    /// IOC注册扩展
    /// </summary>
    public static class HttpAPIInvokerExtension
    {
        public static void AddHttpInvoker(this IServiceCollection services, Action<HttpAPIInvokerOptions> action)
        {
            services.Configure(action);//配置给IOC  其他字段用默认值

            services.AddTransient<IHttpAPIInvoker, HttpAPIInvoker>();
            //如果还有其他注册，就一并完成
        }

        public static void AddHttpInvoker(this IServiceCollection services)
        {
            services.AddHttpInvoker(null);
        }
    }
}
