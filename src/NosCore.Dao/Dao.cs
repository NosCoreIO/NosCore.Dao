//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// -----------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NosCore.Dao.Extensions;
using NosCore.Dao.Interfaces;

namespace NosCore.Dao
{
    /// <summary>
    /// Generic data access object implementation for CRUD operations.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <typeparam name="TPk">The primary key type</typeparam>
    public class Dao<TEntity, TDto, TPk> : IDao<TDto, TPk>
    where TEntity : class
    where TPk : struct
    {
        private readonly ILogger<Dao<TEntity, TDto, TPk>> _logger;
        private readonly PropertyInfo[] _primaryKey;
        private readonly Func<DbContext> _dbContextBuilder;
        private readonly ReadOnlyDictionary<Type, Type> _tphEntityToDtoDictionary;
        private readonly ReadOnlyDictionary<Type, Type> _tphDtoToEntityDictionary;

        /// <summary>
        /// Initializes a new instance of the Dao class.
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="dbContextBuilder">The database context factory</param>
        public Dao(ILogger<Dao<TEntity, TDto, TPk>> logger, Func<DbContext> dbContextBuilder)
        {
            // The DTO list is filtered to actual DTO classes (suffix "Dto") — otherwise
            // downstream subclasses of a DTO (e.g. a runtime wrapper `WearableInstance :
            // WearableInstanceDto`) also satisfy TrimEnd("Dto") == entity name and can be
            // picked first, breaking later lookups keyed by the real DTO type.
            var dtos = InterfaceHelper.GetAllTypesOf<TDto>()
                .Where(s => s.Name.EndsWith("Dto"))
                .ToList();
            _tphEntityToDtoDictionary = new ReadOnlyDictionary<Type, Type>(typeof(TDto).IsInterface ?
                InterfaceHelper.GetAllTypesOf<TEntity>().ToDictionary(
                    entity => entity,
                    entity => dtos.First(s => s.Name.TrimEnd("Dto") == entity.Name.TrimEnd("Entity"))
                )
                : new Dictionary<Type, Type> { { typeof(TEntity), typeof(TDto) } });

            _tphDtoToEntityDictionary = new ReadOnlyDictionary<Type, Type>(_tphEntityToDtoDictionary.ToDictionary(s => s.Value, s => s.Key));
            _logger = logger;
            _dbContextBuilder = dbContextBuilder;
            using (var lease = AmbientDbContext.LeaseSync(_dbContextBuilder))
            {
                var keyNames = lease.Context.Model.FindEntityType(typeof(TEntity))?
                    .FindPrimaryKey()?.Properties.Select(x => x.Name).ToHashSet();
                var key = typeof(TDto).GetProperties()
                    .Where(s => keyNames?.Contains(s.Name) ?? false)
                    .ToArray();
                _primaryKey = key.Any() ? key : throw new KeyNotFoundException();
            }
        }

        // Runs a write inside a lease. A failure is logged with the keys involved and,
        // inside a DaoTransactionScope, dooms the scope so it cannot commit the
        // operations that did succeed around the failed one.
        private async Task<T> TryRunAsync<T>(Func<DbContext, Task<T>> operation, T failureResult, string operationName, Func<object?> keys)
        {
            try
            {
                using var lease = await AmbientDbContext.LeaseAsync(_dbContextBuilder).ConfigureAwait(false);
                try
                {
                    return await operation(lease.Context).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    lease.Fail(e);
                    throw;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "{Operation}<{Entity}> failed for key(s) {Keys}", operationName, typeof(TEntity).Name, keys());
                return failureResult;
            }
        }

        private object?[] KeyOf(TDto dto)
        {
            return _primaryKey.Select(primaryKey => primaryKey.GetValue(dto, null)).ToArray();
        }

        private static string FormatKeys(IEnumerable<object?[]> keys)
        {
            return string.Join(", ", keys.Select(key => key.Length == 1 ? $"{key[0]}" : $"({string.Join(", ", key)})"));
        }

        // A key made only of CLR defaults (0, Guid.Empty, null) is a "generate one for me"
        // key: several such rows in one batch are distinct inserts, not duplicates.
        private static bool IsDefaultKey(IEnumerable<object?> keyValues)
        {
            return keyValues.All(value => value == null
                || (value.GetType().IsValueType && value.Equals(Activator.CreateInstance(value.GetType()))));
        }

        /// <inheritdoc />
        public Task<TDto> TryInsertOrUpdateAsync(TDto dto)
        {
            return TryRunAsync(async context =>
            {
                var entity = ToEntity(dto);
                var dbset = context.Set<TEntity>();
                var value = KeyOf(dto);
                var entityfound = await (value.Length > 1 ? dbset.FindAsync(value) : dbset.FindAsync(value.First())).ConfigureAwait(false);
                if (entityfound != null)
                {
                    context.Entry(entityfound).CurrentValues.SetValues(entity);
                }

                if ((value.Any(val => val == null)) || (entityfound == null))
                {
                    dbset.Add(entity ?? throw new InvalidOperationException());
                }

                await context.SaveChangesAsync().ConfigureAwait(false);
                return ToDto(entity);
            }, default(TDto)!, nameof(TryInsertOrUpdateAsync), () => dto == null ? null : FormatKeys(new[] { KeyOf(dto) }));
        }

        /// <inheritdoc />
        public async Task<bool> TryInsertOrUpdateAsync(IEnumerable<TDto> dtos)
        {
            List<TDto> enumerable;
            try
            {
                enumerable = dtos.ToList();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "{Operation}<{Entity}> batch failed", nameof(TryInsertOrUpdateAsync), typeof(TEntity).Name);
                return false;
            }

            return await TryRunAsync(async context =>
            {
                var dbset = context.Set<TEntity>();
                var dbkey2 = _primaryKey.Select(key => typeof(TEntity).GetProperty(key.Name)).ToArray();
                var entities = enumerable.Select(ToEntity).ToList();
                var ids = _primaryKey.Length > 1 ? enumerable.Select(dto => _primaryKey.Select(part => part.GetValue(dto, null))).ToArray() : null;
                var ids2 = _primaryKey.Length == 1 ? enumerable.Select(dto => (TPk)_primaryKey.First().GetValue(dto, null)!).ToArray() : null;
                var entityfounds = (await (_primaryKey.Length > 1 ? dbset.FindAll(dbkey2!, ids!) : dbset.FindAll(dbkey2!, ids2!))
                        .ToListAsync().ConfigureAwait(false))
                    .ToDictionary(s => dbkey2.Select(part => part!.GetValue(s, null)).GetTuple(), x => x);

                var entitytoadd = new List<TEntity>();
                var pendingByKey = new Dictionary<ITuple, int>();
                foreach (var entity in entities)
                {
                    var keyValues = dbkey2.Select(part => part!.GetValue(entity, null)).ToArray();
                    var key = keyValues.GetTuple();
                    if (entityfounds.TryGetValue(key, out var entityfound))
                    {
                        context.Entry(entityfound).CurrentValues.SetValues(entity);
                        continue;
                    }

                    // The same key twice in one batch: last one wins, as it would with two
                    // successive single upserts, instead of failing the whole batch.
                    if (!IsDefaultKey(keyValues) && pendingByKey.TryGetValue(key, out var index))
                    {
                        entitytoadd[index] = entity;
                        continue;
                    }

                    pendingByKey[key] = entitytoadd.Count;
                    entitytoadd.Add(entity);
                }

                dbset.AddRange(entitytoadd);

                await context.SaveChangesAsync().ConfigureAwait(false);

                return true;
            }, false, nameof(TryInsertOrUpdateAsync), () => FormatKeys(enumerable.Select(KeyOf))).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<TDto>?> TryDeleteAsync(IEnumerable<TPk> dtokeys)
        {
            TPk[] keys;
            try
            {
                keys = dtokeys.ToArray();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "{Operation}<{Entity}> batch failed", nameof(TryDeleteAsync), typeof(TEntity).Name);
                return null;
            }

            return await TryRunAsync<IEnumerable<TDto>?>(async context =>
            {
                var dbset = context.Set<TEntity>();
                var dbkey = _primaryKey.Select(primaryKey => typeof(TEntity).GetProperty(primaryKey.Name)).ToArray();
                var toDelete = await dbset.FindAll(dbkey!, keys).ToListAsync().ConfigureAwait(false);
                var deletedDto = toDelete.Select(ToDto).ToList();
                dbset.RemoveRange(toDelete);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return deletedDto;
            }, null, nameof(TryDeleteAsync), () => string.Join(", ", keys)).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task<TDto> TryDeleteAsync(TPk dtokey)
        {
            return TryRunAsync(async context =>
            {
                TDto deletedDto = default!;
                var dbset = context.Set<TEntity>();
                var key = dtokey is ITuple keyArray ? keyArray
                    .GetType()
                    .GetFields()
                    .Select(property => property.GetValue(keyArray))
                    .ToArray() : new object[] { dtokey };
                var entityfound = await dbset.FindAsync(key).ConfigureAwait(false);

                if (entityfound != null)
                {
                    deletedDto = ToDto(entityfound);
                    dbset.Remove(entityfound);
                }

                await context.SaveChangesAsync().ConfigureAwait(false);
                return deletedDto;
            }, default(TDto)!, nameof(TryDeleteAsync), () => dtokey);
        }

        /// <inheritdoc />
        public async Task<TDto> FirstOrDefaultAsync(Expression<Func<TDto, bool>> predicate)
        {
            using var lease = await AmbientDbContext.LeaseAsync(_dbContextBuilder).ConfigureAwait(false);
            var context = lease.Context;
            var ent = await context.Set<TEntity>().FirstOrDefaultAsync(predicate.ReplaceParameter<TDto, TEntity>()).ConfigureAwait(false);
            return ent == null ? default! : ToDto(ent);
        }

        /// <inheritdoc />
        public IEnumerable<TDto> LoadAll()
        {
            using var lease = AmbientDbContext.LeaseSync(_dbContextBuilder);
            return lease.Context.Set<TEntity>().ToList().Select(ToDto).ToList();
        }

        /// <inheritdoc />
        public IEnumerable<TDto> Where(Expression<Func<TDto, bool>> predicate)
        {
            using var lease = AmbientDbContext.LeaseSync(_dbContextBuilder);
            var entities = lease.Context.Set<TEntity>().Where(predicate.ReplaceParameter<TDto, TEntity>());
            return entities.ToList().Select(ToDto).ToList();
        }

        private TDto ToDto(TEntity ent)
        {
            var entityType = ent.GetType();
            var dtoType = _tphEntityToDtoDictionary[entityType];
            return (TDto)ent.Adapt(entityType, dtoType)!;
        }

        private TEntity ToEntity(TDto dto)
        {
            var dtoType = dto!.GetType();
            while (!dtoType.Name.EndsWith("Dto"))
            {
                dtoType = dtoType.BaseType ?? throw new InvalidOperationException();
            }
            var entityType = _tphDtoToEntityDictionary[dtoType];
            return (TEntity)dto!.Adapt(dtoType, entityType)!;
        }
    }
}
