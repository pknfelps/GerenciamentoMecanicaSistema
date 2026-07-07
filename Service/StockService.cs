using Domain.Interface.Stock;
using Domain.Stock;
using Repository.Interface;
using Service.Interface;
using Service.Interface.Exceptions;
using Service.Interface.Commands.Stock;
using Service.Interface.Results.Stock;

namespace Service
{
    public class StockService(IStockRepository repository) : IStockService
    {
        private IStockRepository Repository { get; set; } = repository;

        public async Task RegisterNewMaterial(CreateMaterialCommand material)
        {
            if (await Repository.GetMaterial(name: material.Name, brand: material.Brand) != null)
                throw new ConflictException("Item já cadastrado");

            var registry = await Repository.RegisterNewMaterial(CreateDomain(material));

            if (registry == 0)
                throw new ApplicationFailureException("Falha ao cadastrar o item");
        }

        public async Task<IEnumerable<MaterialResult>> GetMaterials(Guid? id = null, string name = "", string brand = "")
        {
            var itens = await Repository.GetMaterials(id, name, brand);

            return itens.Select(MaterialResult.Create);
        }

        public async Task<MaterialResult?> GetMaterial(Guid? id = null, string name = "", string brand = "")
        {
            if (id == null && string.IsNullOrEmpty(name) && string.IsNullOrEmpty(brand))
                throw new InvalidRequestException("Falha ao procurar item. Nenhum argumento fornecido");

            var material = await Repository.GetMaterial(id, name, brand);

            if (material == null)
                return null;

            return MaterialResult.Create(material);
        }

        public async Task AddMaterialAmount(Guid id, int value)
        {
            var materialDb = await Repository.GetMaterial(id) ?? throw new NotFoundException("Item ainda não cadastrado");

            materialDb.AddAmount(value);

            await UpdateItemAmount(materialDb);
        }

        public async Task RemoveMaterialAmount(Guid id, int value)
        {
            var materialDb = await Repository.GetMaterial(id) ?? throw new NotFoundException("Item ainda não cadastrado");

            materialDb.RemoveAmount(value);

            await UpdateItemAmount(materialDb);
        }

        public async Task ReserveMaterialAmount(Guid id, int value)
        {
            var materialDb = await Repository.GetMaterial(id) ?? throw new NotFoundException("Item ainda não cadastrado");

            materialDb.ReserveAmount(value);

            await UpdateItemAmount(materialDb);
        }

        public async Task RestoreMaterialAmount(Guid id, int value)
        {
            var materialDb = await Repository.GetMaterial(id) ?? throw new NotFoundException("Item ainda não cadastrado");

            materialDb.RestoreAmount(value);

            await UpdateItemAmount(materialDb);
        }

        public async Task ConsumeReservedAmount(Guid id, int value)
        {
            var materialDb = await Repository.GetMaterial(id) ?? throw new NotFoundException("Item ainda não cadastrado");

            materialDb.ConsumeReservedAmount(value);

            await UpdateItemAmount(materialDb);
        }

        public async Task UpdateMaterialPrice(Guid id, decimal value)
        {
            var materialDb = await Repository.GetMaterial(id) ?? throw new NotFoundException("Item ainda não cadastrado");

            materialDb.UpdatePrice(value);

            var result = await Repository.UpdateMaterialPrice(materialDb);

            if (result == 0)
                throw new ApplicationFailureException("Falha ao atualizar o item");
        }

        public async Task DeleteMaterial(Guid id)
        {
            var material = await Repository.GetMaterial(id: id) ?? throw new NotFoundException("Item não encontrado no estoque");

            var result = await Repository.DeleteMaterial(material.Id);

            if (result == 0)
                throw new ApplicationFailureException("Falha ao deletar o item");
        }

        private static IMaterial CreateDomain(CreateMaterialCommand material) => new Material(material.Name, material.Brand, material.Price, material.Amount);

        private async Task UpdateItemAmount(IMaterial material)
        {
            var result = await Repository.UpdateMaterialAmount(material);

            if (result == 0)
                throw new ApplicationFailureException("Falha ao atualizar o item");
        }
    }
}
