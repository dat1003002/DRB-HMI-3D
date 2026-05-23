using DRB_HMI_3D.Models;

namespace DRB_HMI_3D.Services
{
    public interface IWorkshopService
    {
        Task<IEnumerable<Workshop>> GetAllAsync();
        Task<Workshop?> GetByIdAsync(int id);
        Task<Workshop> CreateAsync(Workshop workshop);
        Task UpdateAsync(Workshop workshop);
        Task DeleteAsync(int id);
    }
}