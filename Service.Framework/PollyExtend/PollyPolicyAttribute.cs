using Castle.DynamicProxy;
using Microsoft.Extensions.Caching.Memory;
using Polly;
using System.Collections.Concurrent;
using System.Reflection;

namespace Service.Framework.PollyExtend
{
    /// <summary>
    /// 定义AOP特性类及封装Polly策略
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class PollyPolicyAttribute : Attribute, IInterceptor
    {
        private static ConcurrentDictionary<MethodInfo, AsyncPolicy> policies
            = new ConcurrentDictionary<MethodInfo, AsyncPolicy>();

        private static readonly IMemoryCache memoryCache
            = new MemoryCache(new MemoryCacheOptions());

        public void Intercept(IInvocation invocation)
        {
            if (!invocation.Method.IsDefined(typeof(PollyPolicyConfigAttribute), true))
            {
                // 直接调用方法本身
                invocation.Proceed();
            }
            else
            {
                PollyPolicyConfigAttribute pollyPolicyConfigAttribute = invocation.Method.GetCustomAttribute<PollyPolicyConfigAttribute>()!;
                //一个PollyPolicyAttribute中保持一个policy对象即可
                //其实主要是CircuitBreaker要求对于同一段代码要共享一个policy对象
                //根据反射原理，同一个方法的MethodInfo是同一个对象，但是对象上取出来的PollyPolicyAttribute
                //每次获取的都是不同的对象，因此以MethodInfo为Key保存到policies中，确保一个方法对应一个policy实例
                policies.TryGetValue(invocation.Method, out AsyncPolicy? policy);
                //把本地调用的AspectContext传递给Polly，主要给FallbackAsync中使用
                // 创建Polly上下文对象(字典)
                Context pollyCtx = new();
                pollyCtx["invocation"] = invocation;

                lock (policies)//因为Invoke可能是并发调用，因此要确保policies赋值的线程安全
                {
                    if (policy == null)
                    {
                        policy = Policy.NoOpAsync();//创建一个空的Policy
                        if (pollyPolicyConfigAttribute.IsEnableCircuitBreaker)
                        {
                            policy = policy.WrapAsync(Policy.Handle<Exception>()
                                .CircuitBreakerAsync(pollyPolicyConfigAttribute.ExceptionsAllowedBeforeBreaking,
                                TimeSpan.FromMilliseconds(pollyPolicyConfigAttribute.MillisecondsOfBreak),
                                onBreak: (ex, ts) =>
                                {
                                    Console.WriteLine($"熔断器打开 熔断{pollyPolicyConfigAttribute.MillisecondsOfBreak / 1000}s.");
                                },
                                onReset: () =>
                                {
                                    Console.WriteLine("熔断器关闭，流量正常通行");
                                },
                                onHalfOpen: () =>
                                {
                                    Console.WriteLine("熔断时间到，熔断器半开，放开部分流量进入");
                                }));
                        }
                        if (pollyPolicyConfigAttribute.TimeOutMilliseconds > 0)
                        {
                            policy = policy.WrapAsync(Policy.TimeoutAsync(() =>
                                TimeSpan.FromMilliseconds(pollyPolicyConfigAttribute.TimeOutMilliseconds),
                                Polly.Timeout.TimeoutStrategy.Pessimistic));
                        }
                        if (pollyPolicyConfigAttribute.MaxRetryTimes > 0)
                        {
                            policy = policy.WrapAsync(Policy.Handle<Exception>()
                                .WaitAndRetryAsync(pollyPolicyConfigAttribute.MaxRetryTimes, i =>
                                TimeSpan.FromMilliseconds(pollyPolicyConfigAttribute.RetryIntervalMilliseconds)));
                        }
                        // 定义降级测试
                        var policyFallBack = Policy.Handle<Exception>().FallbackAsync((fallbackContent, token) =>
                        {
                            // 必须从Polly的Context种获取IInvocation对象
                            IInvocation iv = (IInvocation)fallbackContent["invocation"];
                            var fallBackMethod = iv.TargetType.GetMethod(pollyPolicyConfigAttribute.FallBackMethod!);
                            var fallBackResult = fallBackMethod!.Invoke(iv.InvocationTarget, iv.Arguments);
                            iv.ReturnValue = fallBackResult;
                            return Task.CompletedTask;
                        }, (ex, t) =>
                        {
                            Console.WriteLine("====================>触发服务降级");
                            return Task.CompletedTask;
                        });

                        policy = policyFallBack.WrapAsync(policy);
                        //放入到缓存
                        policies.TryAdd(invocation.Method, policy);
                    }
                }

                // 是否启用缓存
                if (pollyPolicyConfigAttribute.CacheTTLMilliseconds > 0)
                {
                    //用类名+方法名+参数的下划线连接起来作为缓存key
                    string cacheKey = "PollyMethodCacheManager_Key_" + invocation.Method.DeclaringType
                                                                       + "." + invocation.Method + string.Join("_", invocation.Arguments);
                    //尝试去缓存中获取。如果找到了，则直接用缓存中的值做返回值
                    if (memoryCache.TryGetValue(cacheKey, out var cacheValue))
                    {
                        invocation.ReturnValue = cacheValue;
                    }
                    else
                    {
                        //如果缓存中没有，则执行实际被拦截的方法
                        Task task = policy.ExecuteAsync(
                            async (context) =>
                            {
                                invocation.Proceed();
                                await Task.CompletedTask;
                            },
                            pollyCtx
                        );
                        task.Wait();

                        //存入缓存中
                        using var cacheEntry = memoryCache.CreateEntry(cacheKey);
                        {
                            cacheEntry.Value = invocation.ReturnValue;
                            cacheEntry.AbsoluteExpiration = DateTime.Now + TimeSpan.FromMilliseconds(pollyPolicyConfigAttribute.CacheTTLMilliseconds);
                        }
                    }
                }
                else//如果没有启用缓存，就直接执行业务方法
                {
                    Task task = policy.ExecuteAsync(
                            async (context) =>
                            {
                                invocation.Proceed();
                                await Task.CompletedTask;
                            },
                            pollyCtx
                        );
                    task.Wait();
                }
            }
        }

    }
}
