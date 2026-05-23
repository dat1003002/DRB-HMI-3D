using DRB_HMI_3D.Data;
using DRB_HMI_3D.Models;
using Microsoft.EntityFrameworkCore;

namespace DRB_HMI_3D.Repository
{
    public class PressItemTagRepository : IPressItemTagRepository
    {
        private readonly AppDbContext _context;

        public PressItemTagRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<PressItem>> GetAllAsync()
        {
            return await _context.PressItems
                .Include(x => x.PressGroup)
                .Include(x => x.Tags)
                .OrderBy(x => x.PressGroupId)
                .ThenBy(x => x.Id)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<PressItem?> GetByIdAsync(int id)
        {
            return await _context.PressItems
                .Include(x => x.PressGroup)
                .Include(x => x.Tags)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<PressGroup>> GetPressGroupsAsync()
        {
            return await _context.PressGroups
                .OrderBy(x => x.Id)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<PressTag?> GetTagByIdAsync(int id)
        {
            return await _context.PressTags
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> PressGroupExistsAsync(int id)
        {
            return await _context.PressGroups
                .AnyAsync(x => x.Id == id);
        }

        public async Task AddAsync(PressItem item)
        {
            await _context.PressItems.AddAsync(item);
        }

        public void Remove(PressItem item)
        {
            _context.PressItems.Remove(item);
        }

        public void RemoveTag(PressTag tag)
        {
            _context.PressTags.Remove(tag);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}