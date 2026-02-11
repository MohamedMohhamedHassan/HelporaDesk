using System.ComponentModel.DataAnnotations;

namespace ServiceCore.Models
{
    public class SolutionTopic
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int? ParentId { get; set; }
        public SolutionTopic? Parent { get; set; }

        public string? Icon { get; set; }

        public ICollection<SolutionTopic> Children { get; set; } = new List<SolutionTopic>();
        public ICollection<Solution> Solutions { get; set; } = new List<Solution>();
    }
}
