using Domain.Interface.Stock;

namespace Repository.Interface
{
    public interface IStockRepository
    {
        Task<int> RegisterNewMaterial(IMaterial material);
        Task<IEnumerable<IMaterial>> GetMaterials(Guid? id = null, string name = "", string brand = "");
        Task<IMaterial?> GetMaterial(Guid? id = null, string name = "", string brand = "");
        Task<int> UpdateMaterialPrice(IMaterial material);
        Task<int> UpdateMaterialAmount(IMaterial material);
        Task<int> DeleteMaterial(Guid materialId);
    }
}
