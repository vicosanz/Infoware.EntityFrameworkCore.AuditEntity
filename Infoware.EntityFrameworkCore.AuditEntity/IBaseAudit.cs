namespace Infoware.EntityFrameworkCore.AuditEntity
{
    public interface IBaseAudit
    {
        string Details { get; set; }
        DateTime EventDate { get; set; }
        long Id { get; set; }
        string Operation { get; set; }
    }
}