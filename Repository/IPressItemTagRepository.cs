using DRB_HMI_3D.Models;

namespace DRB_HMI_3D.Repository
{
    public interface IPressItemTagRepository
    {
        Task<List<PressItem>> GetAllAsync();
        Task<PressItem?> GetByIdAsync(int id);
        Task<List<PressGroup>> GetPressGroupsAsync();
        Task<PressTag?> GetTagByIdAsync(int id);
        Task<bool> PressGroupExistsAsync(int id);
        Task AddAsync(PressItem item);
        void Remove(PressItem item);
        void RemoveTag(PressTag tag);
        Task SaveChangesAsync();
    }
}