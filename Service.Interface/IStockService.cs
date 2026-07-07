using Service.Interface.Commands.Stock;
using Service.Interface.Results.Stock;

namespace Service.Interface
{
    public interface IStockService
    {
        Task RegisterNewMaterial(CreateMaterialCommand material);
        Task<IEnumerable<MaterialResult>> GetMaterials(Guid? id = null, string name = "", string brand = "");
        Task<MaterialResult?> GetMaterial(Guid? id = null, string name = "", string brand = "");
        Task AddMaterialAmount(Guid id, int value);
        Task RemoveMaterialAmount(Guid id, int value);
        Task ReserveMaterialAmount(Guid id, int value);
        Task RestoreMaterialAmount(Guid id, int value);
        Task ConsumeReservedAmount(Guid id, int value);
        Task UpdateMaterialPrice(Guid id, decimal value);
        Task DeleteMaterial(Guid id);
    }
}
