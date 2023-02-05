namespace ServiceC.Service
{
    public class OrderService : IOrderService
    {
        private readonly IUserService userService;

        public OrderService(IUserService userService)
        {
            this.userService = userService;
        }

        public async Task<bool> AddOrder(Order order)
        {
            Console.WriteLine("================================================================");
            // 根据订单的用户ID获取用户信息
            Console.WriteLine("=============>新增订单信息");
            Console.WriteLine("=============>远程调用用户微服务");
            // 非AOP调用
            var user = await userService.GetById(order.UserId);
            Console.WriteLine($"用户信息: name={user.Name},id={user.Id}");
            if (user == null)
            {
                return false;
            }

            return true;
        }

        public bool AddOrderForAOP(Order order)
        {
            Console.WriteLine("====================================================");
            // 根据订单的用户ID获取用户信息
            Console.WriteLine("AOP=============>新增订单信息");
            // AOP调用
            var user = userService.AOPGetById(order.UserId);
            Console.WriteLine($"用户信息: name={user.Name},id={user.Id}");
            if (user == null)
            {
                return false;
            }

            return true;
        }
    }
}
