using Infoware.SensitiveDataLogger.Attributes;
using Infoware.SensitiveDataLogger.JsonSerializer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Infoware.EntityFrameworkCore.AuditEntity
{
    public abstract class BaseAuditInterceptor : SaveChangesInterceptor
    {
        private readonly ILogJsonSerializer _logJsonSerializer;
        private List<EntityEntry<IAuditable>> addeds = new();

        public BaseAuditInterceptor(ILogJsonSerializer logJsonSerializer)
        {
            _logJsonSerializer = logJsonSerializer;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
                                                                              InterceptionResult<int> result,
                                                                              CancellationToken cancellationToken = default)
        {
            if (eventData.Context is null)
            {
                return base.SavingChangesAsync(eventData, result, cancellationToken);
            }

            addeds = eventData.Context.ChangeTracker.Entries<IAuditable>()
                    .Where(e => e.State == EntityState.Added).ToList();

            eventData.Context.AddRange(
                eventData.Context.ChangeTracker.Entries<IAuditable>()
                    .Where(e => e.State == EntityState.Modified || e.State == EntityState.Deleted)
                    .Select(e => GetAuditFromRecord(e))
            );

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context is null)
            {
                return base.SavedChangesAsync(eventData, result, cancellationToken);
            }

            eventData.Context.AddRange(
                addeds.Select(e => GetAuditFromRecord(e, EntityState.Added))
            );
            eventData.Context.SaveChanges();

            return base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        private void SetAuditRecords(DbContext context)
        {
            context.AddRange(
                context.ChangeTracker.Entries<IAuditable>()
                    .Select(e => GetAuditFromRecord(e))
            );
        }

        private IBaseAudit GetAuditFromRecord(EntityEntry entry, EntityState? overrideState = null)
        {
            var auditableEntityType = entry.Entity.GetType().GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAuditable<>))!;
            var keyName = entry.Metadata.FindPrimaryKey()!.Properties[0]!;
            var keyValue = entry.Property(keyName.Name).CurrentValue;

            Type entityAuditType = auditableEntityType.GenericTypeArguments.First();

            IBaseAudit? objResult = InitAuditObject(entityAuditType).Result;
            objResult!.Operation = (overrideState ?? entry.State).ToString();

            PropertyInfo? prop = entityAuditType.GetProperty("TableId", BindingFlags.Public | BindingFlags.Instance);
            if (null != prop && prop.CanWrite)
            {
                prop.SetValue(objResult, keyValue, null);
            }

            switch (overrideState ?? entry.State)
            {
                case EntityState.Added:
                    WriteHistoryAddedState(objResult, entry);
                    break;
                case EntityState.Modified:
                    WriteHistoryModifiedState(objResult, entry);
                    break;
                case EntityState.Deleted:
                    WriteHistoryDeletedState(objResult, entry);
                    break;
            }

            return objResult;
        }

        private void WriteHistoryAddedState(IBaseAudit audit, EntityEntry entry)
        {
            var sensitives = GetSensitiveProperties(entry);
            dynamic json = new System.Dynamic.ExpandoObject();
            foreach (var prop in entry.Properties)
            {
                if (prop.CurrentValue != null)
                {
                    if (prop.Metadata.IsKey() || prop.Metadata.IsForeignKey())
                    {
                        continue;
                    }
                    ((IDictionary<string, object?>)json)[prop.Metadata.Name] = IfEntryPropertySensitive(prop, sensitives) ? "**SensitiveData**" : prop.CurrentValue;
                }
            }

            audit.Operation = EntityState.Added.ToString();
            audit.Details = _logJsonSerializer.SerializeObject(json);
        }

        private void WriteHistoryModifiedState(IBaseAudit audit, EntityEntry entry)
        {
            var sensitives = GetSensitiveProperties(entry);
            dynamic json = new System.Dynamic.ExpandoObject();
            dynamic bef = new System.Dynamic.ExpandoObject();
            dynamic aft = new System.Dynamic.ExpandoObject();

            PropertyValues? databaseValues = null;
            foreach (var prop in entry.Properties)
            {
                if (prop.IsModified && !(prop.OriginalValue ?? "").Equals(prop.CurrentValue ?? ""))
                {
                    if (prop.OriginalValue != null)
                    {
                        if (!prop.OriginalValue.Equals(prop.CurrentValue))
                        {
                            ((IDictionary<string, object?>)bef)[prop.Metadata.Name] = IfEntryPropertySensitive(prop, sensitives) ? "**SensitiveData**" : prop.OriginalValue;
                        }
                        else
                        {
                            databaseValues ??= entry.GetDatabaseValues();
                            var originalValue = databaseValues!.GetValue<object>(prop.Metadata.Name);
                            ((IDictionary<string, object?>)bef)[prop.Metadata.Name] = IfEntryPropertySensitive(prop, sensitives) ? "**SensitiveData**" : originalValue;
                        }
                    }
                    else
                    {
                        ((IDictionary<string, object?>)bef)[prop.Metadata.Name] = IfEntryPropertySensitive(prop, sensitives) ? "**SensitiveData**" : default;
                    }

                    ((IDictionary<string, object?>)aft)[prop.Metadata.Name] = IfEntryPropertySensitive(prop, sensitives) ? "**SensitiveData**" : prop.CurrentValue!;
                }
            }

            ((IDictionary<string, object?>)json)["before"] = bef;
            ((IDictionary<string, object?>)json)["after"] = aft;

            audit.Operation = EntityState.Modified.ToString();
            audit.Details = _logJsonSerializer.SerializeObject(json);
        }

        private void WriteHistoryDeletedState(IBaseAudit audit, EntityEntry entry)
        {
            var sensitives = GetSensitiveProperties(entry);
            dynamic json = new System.Dynamic.ExpandoObject();

            foreach (var prop in entry.Properties)
            {
                ((IDictionary<string, object?>)json)[prop.Metadata.Name] = IfEntryPropertySensitive(prop, sensitives) ? "**SensitiveData**" : prop.OriginalValue;
            }
            audit.Operation = EntityState.Deleted.ToString();
            audit.Details = _logJsonSerializer.SerializeObject(json);
        }

        private static IEnumerable<string> GetSensitiveProperties(EntityEntry entry)
        {
            return entry.Metadata.ClrType.GetProperties()
                .Where(p => p.GetCustomAttributes<SensitiveDataAttribute>(true).Any())
                .Select(p => p.Name);
        }

        private static bool IfEntryPropertySensitive(PropertyEntry prop, IEnumerable<string> sensitives)
        {
            return sensitives.Contains(prop.Metadata.Name);
        }

        public virtual Task<IBaseAudit?> InitAuditObject(Type entityAuditType)
        {
            var result = (IBaseAudit?)Activator.CreateInstance(entityAuditType);
            return Task.FromResult(result);
        }
    }
}
