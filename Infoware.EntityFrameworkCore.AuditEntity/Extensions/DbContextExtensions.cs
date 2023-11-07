﻿using Infoware.SensitiveDataLogger.Attributes;
using Infoware.SensitiveDataLogger.JsonSerializer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Reflection;

namespace Infoware.EntityFrameworkCore.AuditEntity.Extensions
{
    public static class DbContextExtensions
    {
        internal static Task<IBaseAudit?> FactoryBaseAsync(Type source)
        {
            return Task.FromResult(Activator.CreateInstance(source) as IBaseAudit);
        }

        public static Task<int> SaveWithAuditsAsync(this DbContext context, ILogJsonSerializer logJsonSerializer, 
            Func<CancellationToken, Task<int>> baseSaveChangesAsync, CancellationToken cancellationToken = default)
        {
            return SaveWithAuditsAsync(context, logJsonSerializer, FactoryBaseAsync, baseSaveChangesAsync, cancellationToken);
        }

        public static async Task<int> SaveWithAuditsAsync<T>(this DbContext context, ILogJsonSerializer logJsonSerializer,
            Func<Type, Task<T?>> factoryAudit, Func<CancellationToken, Task<int>> baseSaveChangesAsync, CancellationToken cancellationToken = default) where T : IBaseAudit
        {
            var operationsUpdatesDeletes = await context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Modified || e.State == EntityState.Deleted)
                .AddEntriesAsync(logJsonSerializer, factoryAudit);

            var entriesAdded = context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added).ToList();

            var result = await baseSaveChangesAsync(cancellationToken);

            var operationsAdds = await entriesAdded.AddEntriesAsync(logJsonSerializer, factoryAudit, EntityState.Added);
            context.AddRange(operationsUpdatesDeletes);
            context.AddRange(operationsAdds);

            await baseSaveChangesAsync(cancellationToken);
            return result;
        }

        internal static IBaseAudit? FactoryBase(Type source)
        {
            return Activator.CreateInstance(source) as IBaseAudit;
        }

        public static int SaveWithAudits(this DbContext context, ILogJsonSerializer logJsonSerializer, Func<int> baseSaveChanges)
        {
            return SaveWithAudits(context, logJsonSerializer, FactoryBase, baseSaveChanges);
        }

        public static int SaveWithAudits<T>(this DbContext context, ILogJsonSerializer logJsonSerializer, 
            Func<Type, T?> factoryAudit, Func<int> baseSaveChanges) where T : IBaseAudit
        {
            var operationsUpdatesDeletes = context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Modified || e.State == EntityState.Deleted)
                .AddEntries(logJsonSerializer, factoryAudit);

            var entriesAdded = context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added).ToList();

            var result = baseSaveChanges();

            var operationsAdds = entriesAdded.AddEntries(logJsonSerializer, factoryAudit, EntityState.Added);
            context.AddRange(operationsUpdatesDeletes);
            context.AddRange(operationsAdds);

            baseSaveChanges();
            return result;
        }

        internal static async Task<List<IBaseAudit>> AddEntriesAsync<T>(this IEnumerable<EntityEntry> entries, ILogJsonSerializer logJsonSerializer,
            Func<Type, Task<T?>> factoryAudit, EntityState? overrideState = null) where T : IBaseAudit
        {
            var result = new List<IBaseAudit>();
            if (entries == null) return result;

            foreach (var entry in entries)
            {
                var auditableEntityType = entry.Entity.GetType().GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAuditable<>));
                if (auditableEntityType != null)
                {
                    var keyName = entry.Metadata.FindPrimaryKey()!.Properties[0]!;
                    var keyValue = entry.Property(keyName.Name).CurrentValue;

                    Type entityAuditType = auditableEntityType.GenericTypeArguments.First();

                    IBaseAudit? objResult = await factoryAudit(entityAuditType);
                    objResult!.Operation = entry.State.ToString();

                    PropertyInfo? prop = entityAuditType.GetProperty("TableId", BindingFlags.Public | BindingFlags.Instance);
                    if (null != prop && prop.CanWrite)
                    {
                        prop.SetValue(objResult, keyValue, null);
                    }

                    switch (overrideState ?? entry.State)
                    {
                        case EntityState.Added:
                            WriteHistoryAddedState(objResult, entry, logJsonSerializer);
                            break;
                        case EntityState.Modified:
                            WriteHistoryModifiedState(objResult, entry, logJsonSerializer);
                            break;
                        case EntityState.Deleted:
                            WriteHistoryDeletedState(objResult, entry, logJsonSerializer);
                            break;
                    }

                    result.Add(objResult);
                }
            }
            return result;
        }

        internal static List<IBaseAudit> AddEntries<T>(this IEnumerable<EntityEntry> entries, ILogJsonSerializer logJsonSerializer,
            Func<Type, T?> factoryAudit, EntityState? overrideState = null) where T : IBaseAudit
        {
            var result = new List<IBaseAudit>();
            if (entries == null) return result;

            foreach (var entry in entries)
            {
                var auditableEntityType = entry.Entity.GetType().GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAuditable<>));
                if (auditableEntityType != null)
                {
                    var keyName = entry.Metadata.FindPrimaryKey()!.Properties[0]!;
                    var keyValue = entry.Property(keyName.Name).CurrentValue;

                    Type entityAuditType = auditableEntityType.GenericTypeArguments.First();

                    IBaseAudit? objResult = factoryAudit(entityAuditType);
                    objResult!.Operation = entry.State.ToString();

                    PropertyInfo? prop = entityAuditType.GetProperty("TableId", BindingFlags.Public | BindingFlags.Instance);
                    if (null != prop && prop.CanWrite)
                    {
                        prop.SetValue(objResult, keyValue, null);
                    }

                    switch (overrideState ?? entry.State)
                    {
                        case EntityState.Added:
                            WriteHistoryAddedState(objResult, entry, logJsonSerializer);
                            break;
                        case EntityState.Modified:
                            WriteHistoryModifiedState(objResult, entry, logJsonSerializer);
                            break;
                        case EntityState.Deleted:
                            WriteHistoryDeletedState(objResult, entry, logJsonSerializer);
                            break;
                    }

                    result.Add(objResult);
                }
            }
            return result;
        }

        private static IEnumerable<string> GetSensitiveProperties(this EntityEntry entry)
        {
            return entry.Metadata.ClrType.GetProperties()
                .Where(p => p.GetCustomAttributes<SensitiveDataAttribute>(true).Any())
                .Select(p => p.Name);
        }

        private static bool IfEntryPropertySensitive(this PropertyEntry prop, IEnumerable<string> sensitives)
        {
            return sensitives.Contains(prop.Metadata.Name);
        }

        private static void WriteHistoryAddedState(IBaseAudit audit, EntityEntry entry, ILogJsonSerializer logJsonSerializer)
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
                    ((IDictionary<string, object?>)json)[prop.Metadata.Name] = prop.IfEntryPropertySensitive(sensitives) ? "**SensitiveData**" : prop.CurrentValue;
                }
            }

            audit.Operation = EntityState.Added.ToString();
            audit.Details = logJsonSerializer.SerializeObject(json);
        }

        private static void WriteHistoryModifiedState(IBaseAudit audit, EntityEntry entry, ILogJsonSerializer logJsonSerializer)
        {
            var sensitives = GetSensitiveProperties(entry);
            dynamic json = new System.Dynamic.ExpandoObject();
            dynamic bef = new System.Dynamic.ExpandoObject();
            dynamic aft = new System.Dynamic.ExpandoObject();

            PropertyValues? databaseValues = null;
            foreach (var prop in entry.Properties)
            {
                if (prop.IsModified && !(prop.OriginalValue ?? "").Equals((prop.CurrentValue ?? "")))
                {
                    if (prop.OriginalValue != null)
                    {
                        if (!prop.OriginalValue.Equals(prop.CurrentValue))
                        {
                            ((IDictionary<string, object?>)bef)[prop.Metadata.Name] = prop.IfEntryPropertySensitive(sensitives) ? "**SensitiveData**" : prop.OriginalValue;
                        }
                        else
                        {
                            databaseValues ??= entry.GetDatabaseValues();
                            var originalValue = databaseValues!.GetValue<object>(prop.Metadata.Name);
                            ((IDictionary<string, object?>)bef)[prop.Metadata.Name] = prop.IfEntryPropertySensitive(sensitives) ? "**SensitiveData**" : originalValue;
                        }
                    }
                    else
                    {
                        ((IDictionary<string, object?>)bef)[prop.Metadata.Name] = prop.IfEntryPropertySensitive(sensitives) ? "**SensitiveData**" : default;
                    }

                    ((IDictionary<string, object?>)aft)[prop.Metadata.Name] = prop.IfEntryPropertySensitive(sensitives) ? "**SensitiveData**" : prop.CurrentValue!;
                }
            }

            ((IDictionary<string, object?>)json)["before"] = bef;
            ((IDictionary<string, object?>)json)["after"] = aft;

            audit.Operation = EntityState.Modified.ToString();
            audit.Details = logJsonSerializer.SerializeObject(json);
        }

        private static void WriteHistoryDeletedState(IBaseAudit audit, EntityEntry entry, ILogJsonSerializer logJsonSerializer)
        {
            var sensitives = GetSensitiveProperties(entry);
            dynamic json = new System.Dynamic.ExpandoObject();

            foreach (var prop in entry.Properties)
            {
                ((IDictionary<string, object?>)json)[prop.Metadata.Name] = prop.IfEntryPropertySensitive(sensitives) ? "**SensitiveData**" : prop.OriginalValue;
            }
            audit.Operation = EntityState.Deleted.ToString();
            audit.Details = logJsonSerializer.SerializeObject(json);
        }
    }
}
