using Microsoft.EntityFrameworkCore;
using DRB_HMI_3D.Data;
using DRB_HMI_3D.Models;

namespace DRB_HMI_3D.Repository
{
    public class PressGroupRepository : IPressGroupRepository
    {
        private readonly AppDbContext _context;

        public PressGroupRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PressGroup>> GetAllAsync(int workshopId)
        {
            var query = _context.PressGroups
                .AsNoTracking()
                .AsQueryable();

            if (workshopId > 0)
            {
                query = query.Where(g => g.WorkshopId == workshopId);
            }

            return await query
                .OrderBy(g => g.WorkshopId)
                .ThenBy(g => g.StartIndex)
                .ToListAsync();
        }

        public async Task<PressGroup?> GetByIdAsync(int id)
        {
            return await _context.PressGroups
                .FirstOrDefaultAsync(g => g.Id == id);
        }

        public async Task<PressGroup> AddAsync(PressGroup pressGroup)
        {
            _context.PressGroups.Add(pressGroup);
            await _context.SaveChangesAsync();
            return pressGroup;
        }

        public async Task<PressGroup> UpdateAsync(PressGroup pressGroup)
        {
            _context.PressGroups.Update(pressGroup);
            await _context.SaveChangesAsync();
            return pressGroup;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var group = await _context.PressGroups.FindAsync(id);

            if (group == null)
            {
                return false;
            }

            _context.PressGroups.Remove(group);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.PressGroups.AnyAsync(g => g.Id == id);
        }
    }
}