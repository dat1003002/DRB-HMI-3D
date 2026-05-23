using DRB_HMI_3D.Models;

namespace DRB_HMI_3D.Services
{
    public interface IPressGroupService
    {
        Task<IEnumerable<PressGroup>> GetAllAsync(int workshopId);

        Task<PressGroup?> GetByIdAsync(int id);

        Task<PressGroup> CreateAsync(PressGroup pressGroup);

        Task<PressGroup> UpdateAsync(PressGroup pressGroup);

        Task<bool> DeleteAsync(int id);
    }
}