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

        public async Task RegisterNewPart(CreatePartDto partDto)
        {
            var part = partDto.ToDomain();

            if (await CheckIfItemExists(part.Name, part.Brand))
                throw new InvalidOperationException("Item já cadastrado");

            var registry = await Repository.RegisterNewPart(part);

            if (registry == 0)
                throw new InvalidOperationException("Falha ao cadastrar o part");
        }

        public async Task AddPartAmount(PartUpdateDto<int> partDto)
        {
            var partDb = await Repository.GetPart(partDto.Id) ?? throw new InvalidOperationException("Item ainda não cadastrado");

            partDb.AddAmount(partDto.Value);

            await UpdateItemAmount(partDb);
        }

        public async Task RemovePartAmount(PartUpdateDto<int> partDto)
        {
            var partDb = await Repository.GetPart(partDto.Id) ?? throw new InvalidOperationException("Item ainda não cadastrado");

            partDb.RemoveAmount(partDto.Value);

            await UpdateItemAmount(partDb);
        }

        public async Task ReservePartAmount(PartUpdateDto<int> partDto)
        {
            var partDb = await Repository.GetPart(partDto.Id) ?? throw new InvalidOperationException("Item ainda não cadastrado");

            partDb.ReserveAmount(partDto.Value);

            await UpdateItemAmount(partDb);
        }

        public async Task RestorePartAmount(PartUpdateDto<int> partDto)
        {
            var partDb = await Repository.GetPart(partDto.Id) ?? throw new InvalidOperationException("Item ainda não cadastrado");

            partDb.RestoreAmount(partDto.Value);

            await UpdateItemAmount(partDb);
        }

        public async Task ConsumeReservedAmount(PartUpdateDto<int> partDto)
        {
            var partDb = await Repository.GetPart(partDto.Id) ?? throw new InvalidOperationException("Item ainda não cadastrado");

            partDb.ConsumeReservedAmount(partDto.Value);

            await UpdateItemAmount(partDb);
        }

        public async Task<IEnumerable<PartDto?>> GetParts()
        {
            var itens = await Repository.GetParts();

            return itens.Select(PartDto.Create);
        }

        public async Task<PartDto?> GetPart(string name, string brand)
        {
            var part = await Repository.GetPart(name, brand);

            if (part == null)
                return null;

            return PartDto.Create(part);
        }

        public async Task<PartDto?> GetPart(Guid partId)
        {
            var part = await Repository.GetPart(partId);

            if (part == null)
                return null;

            return PartDto.Create(part);
        }

        public async Task UpdatePartPrice(PartUpdateDto<double> partDto)
        {
            var partDb = await Repository.GetPart(partDto.Id) ?? throw new InvalidOperationException("Item ainda não cadastrado");

            partDb.UpdatePrice(partDto.Value);

            var result = await Repository.UpdatePartPrice(partDb);

            if (result == 0)
                throw new InvalidOperationException("Falha ao atualizar o part");
        }

        public async Task DeletePart(string name, string brand)
        {
            var part = await Repository.GetPart(name, brand) ?? throw new InvalidOperationException("Item não encontrado no estoque");

            var result = await Repository.DeletePart(part.Id);

            if (result == 0)
                throw new InvalidOperationException("Falha ao deletar o part");
        }

        private async Task<bool> CheckIfItemExists(string name, string brand)
        {
            return await Repository.GetPart(name, brand) != null;
        }

        private async Task UpdateItemAmount(IPart part)
        {
            var result = await Repository.UpdatePartAmount(part);

            if (result == 0)
                throw new InvalidOperationException("Falha ao atualizar o part");
        }
    }
}
