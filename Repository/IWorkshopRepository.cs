using DRB_HMI_3D.Models;

namespace DRB_HMI_3D.Repository
{
    public interface IWorkshopRepository
    {
        Task<IEnumerable<Workshop>> GetAllAsync();
        Task<Workshop?> GetByIdAsync(int id);
        Task<Workshop> AddAsync(Workshop workshop);
        Task UpdateAsync(Workshop workshop);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}