using DRB_HMI_3D.Models;
using DRB_HMI_3D.Repository;

namespace DRB_HMI_3D.Services
{
    public class PressItemTagService : IPressItemTagService
    {
        private readonly IPressItemTagRepository _repository;

        public PressItemTagService(IPressItemTagRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<PressItem>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<List<PressGroup>> GetPressGroupsAsync()
        {
            return await _repository.GetPressGroupsAsync();
        }

        public async Task SaveAsync(PressItem model)
        {
            if (model.PressGroupId <= 0)
                throw new Exception("Vui lòng chọn Press Group.");

            if (string.IsNullOrWhiteSpace(model.Name))
                throw new Exception("Vui lòng nhập Press Item Name.");

            if (string.IsNullOrWhiteSpace(model.KepwareTag))
                throw new Exception("Vui lòng nhập Kepware Tag.");

            var groupExists = await _repository.PressGroupExistsAsync(model.PressGroupId);

            if (!groupExists)
                throw new Exception("Press Group không tồn tại.");

            var cleanTags = model.Tags == null
                ? new List<PressTag>()
                : model.Tags
                    .Where(x => !string.IsNullOrWhiteSpace(x.Name) || !string.IsNullOrWhiteSpace(x.KepwareAddress))
                    .ToList();

            if (model.Id == 0)
            {
                var item = new PressItem
                {
                    PressGroupId = model.PressGroupId,
                    Name = model.Name.Trim(),
                    KepwareTag = model.KepwareTag.Trim(),
                    Active = model.Active,
                    Tags = cleanTags.Select(x => new PressTag
                    {
                        Name = x.Name.Trim(),
                        Value = x.Value,
                        KepwareAddress = x.KepwareAddress.Trim()
                    }).ToList()
                };

                await _repository.AddAsync(item);
                await _repository.SaveChangesAsync();
                return;
            }

            var existingItem = await _repository.GetByIdAsync(model.Id);

            if (existingItem == null)
                throw new Exception("Press Item không tồn tại.");

            existingItem.PressGroupId = model.PressGroupId;
            existingItem.Name = model.Name.Trim();
            existingItem.KepwareTag = model.KepwareTag.Trim();
            existingItem.Active = model.Active;

            if (existingItem.Tags == null)
                existingItem.Tags = new List<PressTag>();

            var incomingIds = cleanTags
                .Where(x => x.Id > 0)
                .Select(x => x.Id)
                .ToList();

            var removedTags = existingItem.Tags
                .Where(x => !incomingIds.Contains(x.Id))
                .ToList();

            foreach (var tag in removedTags)
            {
                _repository.RemoveTag(tag);
            }

            foreach (var tagModel in cleanTags)
            {
                if (tagModel.Id > 0)
                {
                    var existingTag = existingItem.Tags.FirstOrDefault(x => x.Id == tagModel.Id);

                    if (existingTag != null)
                    {
                        existingTag.Name = tagModel.Name.Trim();
                        existingTag.Value = tagModel.Value;
                        existingTag.KepwareAddress = tagModel.KepwareAddress.Trim();
                    }
                }
                else
                {
                    existingItem.Tags.Add(new PressTag
                    {
                        PressItemId = existingItem.Id,
                        Name = tagModel.Name.Trim(),
                        Value = tagModel.Value,
                        KepwareAddress = tagModel.KepwareAddress.Trim()
                    });
                }
            }

            await _repository.SaveChangesAsync();
        }

        public async Task DeleteItemAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);

            if (item == null)
                throw new Exception("Press Item không tồn tại.");

            if (item.Tags != null)
            {
                foreach (var tag in item.Tags.ToList())
                {
                    _repository.RemoveTag(tag);
                }
            }

            _repository.Remove(item);
            await _repository.SaveChangesAsync();
        }

        public async Task DeleteTagAsync(int id)
        {
            var tag = await _repository.GetTagByIdAsync(id);

            if (tag == null)
                throw new Exception("Press Tag không tồn tại.");

            _repository.RemoveTag(tag);
            await _repository.SaveChangesAsync();
        }
    }
}