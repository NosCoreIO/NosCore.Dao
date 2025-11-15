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
using NosCore.Dao.Extensions;
using NosCore.Dao.Interfaces;
using Serilog;

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
        private readonly ILogger _logger;
        private readonly PropertyInfo[] _primaryKey;
        private readonly Func<DbContext> _dbContextBuilder;
        private readonly ReadOnlyDictionary<Type, Type> _tphEntityToDtoDictionary;
        private readonly ReadOnlyDictionary<Type, Type> _tphDtoToEntityDictionary;

        /// <summary>
        /// Initializes a new instance of the Dao class.
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="dbContextBuilder">The database context factory</param>
        public Dao(ILogger logger, Func<DbContext> dbContextBuilder)
        {
            var dtos = InterfaceHelper.GetAllTypesOf<TDto>().ToList();
            _tphEntityToDtoDictionary = new ReadOnlyDictionary<Type, Type>(typeof(TDto).IsInterface ?
                InterfaceHelper.GetAllTypesOf<TEntity>().ToDictionary(
                    entity => entity,
                    entity => dtos.First(s => s.Name.TrimEnd("Dto") == entity.Name.TrimEnd("Entity"))
                )
                : new Dictionary<Type, Type> { { typeof(TEntity), typeof(TDto) } });

            _tphDtoToEntityDictionary = new ReadOnlyDictionary<Type, Type>(_tphEntityToDtoDictionary.ToDictionary(s => s.Value, s => s.Key));
            _logger = logger;
            _dbContextBuilder = dbContextBuilder;
            var context = _dbContextBuilder();
            var key = typeof(TDto).GetProperties()
                .Where(s => context.Model.FindEntityType(typeof(TEntity))?
                    .FindPrimaryKey()?.Properties.Select(x => x.Name)
                    .Contains(s.Name) ?? false
                ).ToArray();
            _primaryKey = key.Any() ? key : throw new KeyNotFoundException();
        }

        /// <inheritdoc />
        public async Task<TDto> TryInsertOrUpdateAsync(TDto dto)
        {
            try
            {
                var entity = ToEntity(dto);
                var context = _dbContextBuilder();
                var dbset = context.Set<TEntity>();
                var value = _primaryKey.Select(primaryKey => primaryKey.GetValue(dto, null)).ToArray();
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
            }
            catch (Exception e)
            {
                _logger.Error("", e);
                return default!;
            }
        }

        /// <inheritdoc />
        public async Task<bool> TryInsertOrUpdateAsync(IEnumerable<TDto> dtos)
        {
            try
            {
                var enumerable = dtos.ToList();
                var context = _dbContextBuilder();

                var dbset = context.Set<TEntity>();
                var entitytoadd = new List<TEntity>();

                var dbkey2 = _primaryKey.Select(key => typeof(TEntity).GetProperty(key.Name)).ToArray();
                var list = enumerable.Select(ToEntity);
                var ids = _primaryKey.Length > 1 ? enumerable.Select(dto => _primaryKey.Select(part => part.GetValue(dto, null))).ToArray() : null;
                var ids2 = _primaryKey.Length == 1 ? enumerable.Select(dto => (TPk)_primaryKey.First().GetValue(dto, null)!).ToArray() : null;
                var entityfounds = (_primaryKey.Length > 1 ? dbset.FindAll(dbkey2!, ids!) : dbset.FindAll(dbkey2!, ids2!))
                    .ToDictionary(s => dbkey2.Select(part => part!.GetValue(s, null)).GetTuple(), x => x);

                foreach (var entity in list)
                {
                    var key = dbkey2.Select(part => part!.GetValue(entity, null)).GetTuple();
                    var entityfound = entityfounds.ContainsKey(key) ? entityfounds[key] : null;
                    if (entityfound != null)
                    {
                        context.Entry(entityfound).CurrentValues.SetValues(entity);
                        continue;
                    }

                    entitytoadd.Add(entity);
                }

                dbset.AddRange(entitytoadd);

                await context.SaveChangesAsync().ConfigureAwait(false);

                return true;
            }
            catch (Exception e)
            {
                _logger.Error("", e);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<TDto>?> TryDeleteAsync(IEnumerable<TPk> dtokeys)
        {
            try
            {
                var context = _dbContextBuilder();
                var dbset = context.Set<TEntity>();
                var dbkey = _primaryKey.Select(primaryKey => typeof(TEntity).GetProperty(primaryKey.Name)).ToArray();
                var toDelete = dbset.FindAll(dbkey!, dtokeys.ToArray());
                var deletedDto = toDelete.ToList().Select(ToDto);
                dbset.RemoveRange(toDelete);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return deletedDto;
            }
            catch (Exception e)
            {
                _logger.Error("", e);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<TDto> TryDeleteAsync(TPk dtokey)
        {
            try
            {
                TDto deletedDto = default!;
                var context = _dbContextBuilder();
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
            }
            catch (Exception e)
            {
                _logger.Error(e.Message, e);
                return default!;
            }
        }

        /// <inheritdoc />
        public async Task<TDto> FirstOrDefaultAsync(Expression<Func<TDto, bool>> predicate)
        {
            var context = _dbContextBuilder();
            var ent = await context.Set<TEntity>().FirstOrDefaultAsync(predicate.ReplaceParameter<TDto, TEntity>()).ConfigureAwait(false);
            return ent == null ? default! : ToDto(ent);
        }

        /// <inheritdoc />
        public IEnumerable<TDto> LoadAll()
        {
            var context = _dbContextBuilder();
            return context.Set<TEntity>().ToList().Select(ToDto);
        }

        /// <inheritdoc />
        public IEnumerable<TDto> Where(Expression<Func<TDto, bool>> predicate)
        {
            var context = _dbContextBuilder();
            var entities = context.Set<TEntity>().Where(predicate.ReplaceParameter<TDto, TEntity>());
            return entities.ToList().Select(ToDto);
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
