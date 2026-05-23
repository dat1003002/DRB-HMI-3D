using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Text.Json.Serialization;

namespace DRB_HMI_3D.Models
{
    public class PressGroup
    {
        public int Id { get; set; }

        public int WorkshopId { get; set; }

        public string Label { get; set; } = string.Empty;

        public int StartIndex { get; set; }

        public int EndIndex { get; set; }

        public string Icon { get; set; } = string.Empty;

        [JsonIgnore]
        [ValidateNever]
        public virtual Workshop? Workshop { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public virtual ICollection<PressItem> PressItems { get; set; } = new List<PressItem>();
    }
}