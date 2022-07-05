using System.ComponentModel.DataAnnotations;

namespace Infoware.EntityFrameworkCore.AuditEntity
{
    public class BaseAudit<TEntity, TPrimaryKeyType> : BaseAudit, IBaseAudit<TEntity, TPrimaryKeyType> where TEntity : IAuditable
    {
        [Required]
        public TPrimaryKeyType TableId { get; set; } = default!;
    }

    public interface IBaseAudit<TEntity, TPrimaryKeyType> : IBaseAudit where TEntity : IAuditable
    {
        [Required]
        public TPrimaryKeyType TableId { get; set; }
    }

    public class BaseAudit : IBaseAudit
    {
        [Key, Required]
        public long Id { get; set; }

        [Required]
        public string Operation { get; set; } = null!;

        [Required]
        public string Details { get; set; } = null!;

        [Required]
        public DateTime EventDate { get; set; } = DateTime.Now;
    }
}