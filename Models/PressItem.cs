using System.Collections.Generic;

namespace DRB_HMI_3D.Models
{
    public class PressItem
    {
        public int Id { get; set; }
        public int PressGroupId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string KepwareTag { get; set; } = string.Empty;
        public bool Active { get; set; }
        public virtual ICollection<PressTag> Tags { get; set; } = new List<PressTag>();
        public virtual PressGroup? PressGroup { get; set; }
    }
}