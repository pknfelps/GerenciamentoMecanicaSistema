using Service.Interface.Dto.Stock;

namespace Service.Interface
{
    public interface IStockService
    {
        Task RegisterNewItem(StockItemDto itemDto);
        Task AddItemAmount(StockItemUpdateDto<int> itemDto);
        Task RemoveItemAmount(StockItemUpdateDto<int> itemDto);
        Task ReserveItemAmount(StockItemUpdateDto<int> itemDto);
        Task RestoreItemAmount(StockItemUpdateDto<int> itemDto);
        Task<IEnumerable<StockItemDto?>> GetItens();
        Task<StockItemDto?> GetItem(string name, string brand);
        Task UpdateItemPrice(StockItemUpdateDto<double> itemDto);
        Task DeleteItem(string name, string brand);
    }
}
