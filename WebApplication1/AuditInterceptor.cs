using Infoware.EntityFrameworkCore.AuditEntity;
using Infoware.SensitiveDataLogger.Attributes;
using Infoware.SensitiveDataLogger.JsonSerializer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WebApplication1.Models;

namespace WebApplication1
{
    public class AuditInterceptor : BaseAuditInterceptor
    {
        public AuditInterceptor(ILogJsonSerializer logJsonSerializer): base(logJsonSerializer) { }

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
