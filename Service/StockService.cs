using Domain.Interface.Stock;
using Repository.Interface;
using Service.Interface;
using Service.Interface.Dto.Stock;

namespace Service
{
    public class StockService(IStockRepository repository) : IStockService
    {
        private IStockRepository Repository { get; set; } = repository;

        public async Task RegisterNewPart(CreatePartDto partDto)
        {
            if (await Repository.GetPart(name: partDto.Name, brand: partDto.Brand) != null)
                throw new InvalidOperationException("Item já cadastrado");

            var registry = await Repository.RegisterNewPart(partDto.ToDomain());

            if (registry == 0)
                throw new InvalidOperationException("Falha ao cadastrar o item");
        }

        public async Task<IEnumerable<PartDto>> GetParts(Guid? id = null, string name = "", string brand = "")
        {
            var itens = await Repository.GetParts(id, name, brand);

            return itens.Select(PartDto.Create);
        }

        public async Task<PartDto?> GetPart(Guid? id = null, string name = "", string brand = "")
        {
            if (id == null && string.IsNullOrEmpty(name) && string.IsNullOrEmpty(brand))
                throw new InvalidOperationException("Falha ao procurar item. Nenhum argumento fornecido");

            var part = await Repository.GetPart(id, name, brand);

            if (part == null)
                return null;

            return PartDto.Create(part);
        }

        public async Task AddPartAmount(Guid id, int value)
        {
            var partDb = await Repository.GetPart(id) ?? throw new InvalidOperationException("Item ainda não cadastrado");

            partDb.AddAmount(value);

            await UpdateItemAmount(partDb);
        }

        public async Task RemovePartAmount(Guid id, int value)
        {
            var partDb = await Repository.GetPart(id) ?? throw new InvalidOperationException("Item ainda não cadastrado");

            partDb.RemoveAmount(value);

            await UpdateItemAmount(partDb);
        }

        public async Task ReservePartAmount(Guid id, int value)
        {
            var partDb = await Repository.GetPart(id) ?? throw new InvalidOperationException("Item ainda não cadastrado");

            partDb.ReserveAmount(value);

            await UpdateItemAmount(partDb);
        }

        public async Task RestorePartAmount(Guid id, int value)
        {
            var partDb = await Repository.GetPart(id) ?? throw new InvalidOperationException("Item ainda não cadastrado");

            partDb.RestoreAmount(value);

            await UpdateItemAmount(partDb);
        }

        public async Task ConsumeReservedAmount(Guid id, int value)
        {
            var partDb = await Repository.GetPart(id) ?? throw new InvalidOperationException("Item ainda não cadastrado");

            partDb.ConsumeReservedAmount(value);

            await UpdateItemAmount(partDb);
        }

        public async Task UpdatePartPrice(Guid id, double value)
        {
            var partDb = await Repository.GetPart(id) ?? throw new InvalidOperationException("Item ainda não cadastrado");

            partDb.UpdatePrice(value);

            var result = await Repository.UpdatePartPrice(partDb);

            if (result == 0)
                throw new InvalidOperationException("Falha ao atualizar o item");
        }

        public async Task DeletePart(Guid id)
        {
            var part = await Repository.GetPart(id: id) ?? throw new InvalidOperationException("Item não encontrado no estoque");

            var result = await Repository.DeletePart(part.Id);

            if (result == 0)
                throw new InvalidOperationException("Falha ao deletar o item");
        }

        private async Task UpdateItemAmount(IPart part)
        {
            var result = await Repository.UpdatePartAmount(part);

            if (result == 0)
                throw new InvalidOperationException("Falha ao atualizar o item");
        }
    }
}
