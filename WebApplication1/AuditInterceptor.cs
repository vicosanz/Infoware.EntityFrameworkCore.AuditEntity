using Infoware.EntityFrameworkCore.AuditEntity;
using Infoware.SensitiveDataLogger.JsonSerializer;
using WebApplication1.Models;

namespace WebApplication1
{
    public class AuditInterceptor : BaseAuditInterceptor
    {
        public AuditInterceptor(ILogJsonSerializer logJsonSerializer) : base(logJsonSerializer) { }

        public override async Task<IBaseAudit?> InitAuditObject(Type entityAuditType)
        {
            var result = (IExtendedBaseAudit?)Activator.CreateInstance(entityAuditType);
            result!.UserId = "User1";
            result!.UserName = "Username";
            await Task.Delay(1);
            return result;
        }
    }
}
