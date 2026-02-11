using System;
using System.ComponentModel.DataAnnotations;

namespace ServiceCore.Models
{
    public class Milestone
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string? Name { get; set; }

        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        public int ProjectId { get; set; }
        public Project? Project { get; set; }
    }
}
