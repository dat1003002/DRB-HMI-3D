using DRB_HMI_3D.Models;
using DRB_HMI_3D.Repository;

namespace DRB_HMI_3D.Services
{
    public class WorkshopService : IWorkshopService
    {
        private readonly IWorkshopRepository _repository;

        public WorkshopService(IWorkshopRepository repository)
        {
            _repository = repository;
        }

        public Task<IEnumerable<Workshop>> GetAllAsync() => _repository.GetAllAsync();

        public Task<Workshop?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);

        public async Task<Workshop> CreateAsync(Workshop workshop)
        {
            if (string.IsNullOrWhiteSpace(workshop.Name) || string.IsNullOrWhiteSpace(workshop.Channel))
                throw new ArgumentException("Name and Channel are required.");

            return await _repository.AddAsync(workshop);
        }

        public async Task UpdateAsync(Workshop workshop)
        {
            if (!await _repository.ExistsAsync(workshop.Id))
                throw new KeyNotFoundException("Workshop not found.");

            await _repository.UpdateAsync(workshop);
        }

        public async Task DeleteAsync(int id)
        {
            if (!await _repository.ExistsAsync(id))
                throw new KeyNotFoundException("Workshop not found.");

            await _repository.DeleteAsync(id);
        }
    }
}