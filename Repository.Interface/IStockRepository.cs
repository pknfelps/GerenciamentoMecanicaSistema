using Domain.Interface.Stock;

namespace Repository.Interface
{
    public interface IStockRepository
    {
        Task<int> RegisterNewPart(IPart part);
        Task<IEnumerable<IPart>> GetParts(Guid? id = null, string name = "", string brand = "");
        Task<IPart?> GetPart(Guid? id = null, string name = "", string brand = "");
        Task<int> UpdatePartPrice(IPart part);
        Task<int> UpdatePartAmount(IPart part);
        Task<int> DeletePart(Guid partId);
    }
}
