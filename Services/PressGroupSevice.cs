using DRB_HMI_3D.Models;
using DRB_HMI_3D.Repository;

namespace DRB_HMI_3D.Services
{
    public class PressGroupService : IPressGroupService
    {
        private readonly IPressGroupRepository _repository;

        public PressGroupService(IPressGroupRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<PressGroup>> GetAllAsync(int workshopId)
        {
            return await _repository.GetAllAsync(workshopId);
        }

        public async Task<PressGroup?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<PressGroup> CreateAsync(PressGroup pressGroup)
        {
            pressGroup.Id = 0;
            pressGroup.Label = pressGroup.Label.Trim();
            pressGroup.Icon = string.IsNullOrWhiteSpace(pressGroup.Icon)
                ? "fa-solid fa-sliders-h"
                : pressGroup.Icon.Trim();

            pressGroup.Workshop = null;
            pressGroup.PressItems = new List<PressItem>();

            return await _repository.AddAsync(pressGroup);
        }

        public async Task<PressGroup> UpdateAsync(PressGroup pressGroup)
        {
            pressGroup.Label = pressGroup.Label.Trim();
            pressGroup.Icon = string.IsNullOrWhiteSpace(pressGroup.Icon)
                ? "fa-solid fa-sliders-h"
                : pressGroup.Icon.Trim();

            pressGroup.Workshop = null;
            pressGroup.PressItems = new List<PressItem>();

            return await _repository.UpdateAsync(pressGroup);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }
    }
}