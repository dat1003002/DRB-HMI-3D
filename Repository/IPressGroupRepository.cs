using DRB_HMI_3D.Models;

namespace DRB_HMI_3D.Repository
{
    public interface IPressGroupRepository
    {
        Task<IEnumerable<PressGroup>> GetAllAsync(int workshopId);

        Task<PressGroup?> GetByIdAsync(int id);

        Task<PressGroup> AddAsync(PressGroup pressGroup);

        Task<PressGroup> UpdateAsync(PressGroup pressGroup);

        Task<bool> DeleteAsync(int id);

        Task<bool> ExistsAsync(int id);
    }
}