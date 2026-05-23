namespace DRB_HMI_3D.Models
{
    public class Workshop
    {
        public int Id { get; set; }
        public string Name { get; set; }     
        public string Channel { get; set; }    
        public string Description { get; set; }
        public string Icon { get; set; }      
        public virtual ICollection<PressGroup> PressGroups { get; set; }
    }
}
