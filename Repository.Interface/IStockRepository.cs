using Domain.Interface.Stock;

namespace Repository.Interface
{
    public interface IStockRepository
    {
        Task<int> RegisterNewPart(IPart part);
        Task<IEnumerable<IPart?>> GetParts();
        Task<IPart?> GetPart(string name, string brand);
        Task<IPart?> GetPart(Guid partId);
        Task<int> UpdatePartPrice(IPart part);
        Task<int> UpdatePartAmount(IPart part);
        Task<int> DeletePart(Guid partId);
    }
}
