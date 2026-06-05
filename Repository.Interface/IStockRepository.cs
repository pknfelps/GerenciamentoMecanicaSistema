using Domain.Interface.Stock;

namespace Repository.Interface
{
    public interface IStockRepository
    {
        Task<int> RegisterNewItem(IStockItem item);
        Task<IEnumerable<IStockItem?>> GetItens();
        Task<IStockItem?> GetItem(string name, string brand);
        Task<int> UpdateItemPrice(IStockItem item);
        Task<int> UpdateItemAmount(IStockItem item);
        Task<int> DeleteItem(string name, string brand);
    }
}
