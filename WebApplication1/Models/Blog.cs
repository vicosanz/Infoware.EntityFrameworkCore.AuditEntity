using Infoware.EntityFrameworkCore.AuditEntity;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Blog : IAuditable<BlogAudit>
    {
        [Key, Required]
        public long Id { get; set; }
        [Required]
        public string Name { get; set; } = null!;
    }
}
