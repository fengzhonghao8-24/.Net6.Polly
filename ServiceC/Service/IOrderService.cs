namespace ServiceC.Service
{
    public interface IOrderService
    {
        Task<bool> AddOrder(Order order);

        bool AddOrderForAOP(Order order);
    }

    public record Order(long OrderId, int ProductId, int UserId, int Amount);
}
