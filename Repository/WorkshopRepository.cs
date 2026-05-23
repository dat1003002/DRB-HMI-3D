using DRB_HMI_3D.Data;
using DRB_HMI_3D.Models;
using Microsoft.EntityFrameworkCore;

namespace DRB_HMI_3D.Repository
{
    public class WorkshopRepository : IWorkshopRepository
    {
        private readonly AppDbContext _context;

        public WorkshopRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Workshop>> GetAllAsync()
        {
            return await _context.Workshops
                                 .OrderBy(w => w.Id)
                                 .ToListAsync();
        }

        public async Task<Workshop?> GetByIdAsync(int id)
        {
            return await _context.Workshops.FindAsync(id);
        }

        public async Task<Workshop> AddAsync(Workshop workshop)
        {
            _context.Workshops.Add(workshop);
            await _context.SaveChangesAsync();
            return workshop;
        }

        public async Task UpdateAsync(Workshop workshop)
        {
            _context.Workshops.Update(workshop);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var workshop = await _context.Workshops.FindAsync(id);
            if (workshop != null)
            {
                _context.Workshops.Remove(workshop);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Workshops.AnyAsync(w => w.Id == id);
        }
    }
}