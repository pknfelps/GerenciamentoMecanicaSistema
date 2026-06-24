using Service.Interface.Dto.Stock;

namespace Service.Interface
{
    public interface IStockService
    {
        Task RegisterNewMaterial(CreateMaterialDto partDto);
        Task<IEnumerable<MaterialDto>> GetMaterials(Guid? id = null, string name = "", string brand = "");
        Task<MaterialDto?> GetMaterial(Guid? id = null, string name = "", string brand = "");
        Task AddMaterialAmount(Guid id, int value);
        Task RemoveMaterialAmount(Guid id, int value);
        Task ReserveMaterialAmount(Guid id, int value);
        Task RestoreMaterialAmount(Guid id, int value);
        Task ConsumeReservedAmount(Guid id, int value);
        Task UpdateMaterialPrice(Guid id, double value);
        Task DeleteMaterial(Guid id);
    }
}
