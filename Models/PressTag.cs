using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Text.Json.Serialization;

namespace DRB_HMI_3D.Models
{
    public class PressTag
    {
        public int Id { get; set; }
        public int PressItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string KepwareAddress { get; set; } = string.Empty;

        [JsonIgnore]
        [ValidateNever]
        public virtual PressItem? PressItem { get; set; }
    }
}