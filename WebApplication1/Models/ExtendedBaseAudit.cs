using Infoware.EntityFrameworkCore.AuditEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApplication1.Models
{
    public class ExtendedBaseAudit<TEntity, TUniqueId> : BaseAudit<TEntity, TUniqueId>, IExtendedBaseAudit where TEntity : IAuditable
    {
        [Required]
        public string UserId { get; set; } = null!;

        [Required]
        public string UserName { get; set; } = null!;
    }

    public interface IExtendedBaseAudit : IBaseAudit
    {
        public string UserId { get; set; }

        public string UserName { get; set; }
    }
}
