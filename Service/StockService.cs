using Domain.Interface.Stock;
using Domain.Stock;
using Repository.Interface;
using Service.Interface;
using Service.Interface.Dto.Stock;

namespace Service
{
    public class StockService(IStockRepository repository) : IStockService
    {
        private IStockRepository Repository { get; set; } = repository;

        public async Task RegisterNewItem(StockItemDto itemDto)
        {
            var item = ToDomain(itemDto);

            if (await CheckIfItemExists(item.Name, item.Brand))
                throw new InvalidOperationException("Item já cadastrado");

            var result = await Repository.RegisterNewItem(item);

            if (result == 0)
                throw new InvalidOperationException("Falha ao cadastrar o item");
        }

        public async Task AddItemAmount(StockItemUpdateDto<int> itemDto)
        {
            var itemDb = await Repository.GetItem(itemDto.Name, itemDto.Brand) ?? throw new InvalidOperationException("Item ainda não cadastrado");

            itemDb.AddAmount(itemDto.Value);

            await UpdateItemAmount(itemDb);
        }

        public async Task RemoveItemAmount(StockItemUpdateDto<int> itemDto)
        {
            var itemDb = await Repository.GetItem(itemDto.Name, itemDto.Brand) ?? throw new InvalidOperationException("Item ainda não cadastrado");

            itemDb.RemoveAmount(itemDto.Value);

            await UpdateItemAmount(itemDb);
        }

        public async Task ReserveItemAmount(StockItemUpdateDto<int> itemDto)
        {
            var itemDb = await Repository.GetItem(itemDto.Name, itemDto.Brand) ?? throw new InvalidOperationException("Item ainda não cadastrado");

            itemDb.ReserveAmount(itemDto.Value);

            await UpdateItemAmount(itemDb);
        }

        public async Task RestoreItemAmount(StockItemUpdateDto<int> itemDto)
        {
            var itemDb = await Repository.GetItem(itemDto.Name, itemDto.Brand) ?? throw new InvalidOperationException("Item ainda não cadastrado");

            itemDb.RestoreAmount(itemDto.Value);

            await UpdateItemAmount(itemDb);
        }

        public async Task<IEnumerable<StockItemDto?>> GetItens()
        {
            var itens = await Repository.GetItens();

            return itens.Select(ToDto);
        }

        public async Task<StockItemDto?> GetItem(string name, string brand)
        {
            var item = await Repository.GetItem(name, brand);

            if (item == null)
                return null;

            return ToDto(item);
        }

        public async Task UpdateItemPrice(StockItemUpdateDto<double> itemDto)
        {
            var itemDb = await Repository.GetItem(itemDto.Name, itemDto.Brand) ?? throw new InvalidOperationException("Item ainda não cadastrado");

            itemDb.UpdatePrice(itemDto.Value);

            var result = await Repository.UpdateItemPrice(itemDb);

            if (result == 0)
                throw new InvalidOperationException("Falha ao atualizar o item");
        }

        public async Task DeleteItem(string name, string brand)
        {
            if (!await CheckIfItemExists(name, brand))
                throw new InvalidOperationException("Item não encontrado no estoque");

            var result = await Repository.DeleteItem(name, brand);

            if (result == 0)
                throw new InvalidOperationException("Falha ao deletar o item");
        }

        private async Task<bool> CheckIfItemExists(string name, string brand)
        {
            return await Repository.GetItem(name, brand) != null;
        }

        private async Task UpdateItemAmount(IStockItem item)
        {
            var result = await Repository.UpdateItemAmount(item);

            if (result == 0)
                throw new InvalidOperationException("Falha ao atualizar o item");
        }

        private static StockItem ToDomain(StockItemDto itemDto) => new(itemDto.Name, itemDto.Brand, itemDto.Price, itemDto.Amount);
        private static StockItemDto? ToDto(IStockItem item) => new(item.Name, item.Brand, item.Price, item.Amount, item.ReservedAmount);
    }
}
