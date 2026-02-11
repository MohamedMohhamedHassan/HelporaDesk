using System.ComponentModel.DataAnnotations;

namespace ServiceCore.Models
{
    public class RolePermission
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string RoleName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FeatureKey { get; set; } = string.Empty;

        public bool IsAllowed { get; set; } = true;
    }
}
