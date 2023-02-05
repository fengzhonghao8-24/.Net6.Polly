using Autofac.Extras.DynamicProxy;
using Service.Framework.PollyExtend;

namespace ServiceC.Service
{
    [Intercept(typeof(PollyPolicyAttribute))]//表示要polly生效
    public interface IUserService
    {
        [PollyPolicyConfig(FallBackMethod = "UserServiceFallback",
            IsEnableCircuitBreaker = true,
            ExceptionsAllowedBeforeBreaking = 3,
            MillisecondsOfBreak = 1000 * 5,
            CacheTTLMilliseconds = 1000 * 20)]
        User AOPGetById(int id);

        Task<User> GetById(int id);
    }

    public record User(int Id, string Name, string Account, string Password);
}
