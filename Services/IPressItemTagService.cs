using DRB_HMI_3D.Models;

namespace DRB_HMI_3D.Services
{
    public interface IPressItemTagService
    {
        Task<List<PressItem>> GetAllAsync();
        Task<List<PressGroup>> GetPressGroupsAsync();
        Task SaveAsync(PressItem model);
        Task DeleteItemAsync(int id);
        Task DeleteTagAsync(int id);
    }
}