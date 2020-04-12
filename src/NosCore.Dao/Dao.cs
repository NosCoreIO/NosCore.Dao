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
    public class Dao<TEntity, TDto, TPk> : IDao<TDto, TPk>
    where TEntity : class
    where TPk : struct
    {
        private readonly ILogger _logger;
        private readonly PropertyInfo[] _primaryKey;
        private readonly IDbContextBuilder _dbContextBuilder;
        private readonly ReadOnlyDictionary<Type, Type> _tphEntityToDtoDictionary;
        private readonly ReadOnlyDictionary<Type, Type> _tphDtoToEntityDictionary;
        public Dao(ILogger logger, IDbContextBuilder dbContextBuilder)
        {
            if (typeof(TDto).IsInterface)
            {
                var entities = InterfaceHelper.GetAllTypesOf<TEntity>().ToList();
                var dtos = InterfaceHelper.GetAllTypesOf<TDto>().ToList();
                var tphEntityToDtoDictionary = new Dictionary<Type, Type>();
                foreach (var entity in entities)
                {
                    var dto = dtos.First(s => s.Name.TrimEnd("Dto") == entity.Name.TrimEnd("Entity"));
                    tphEntityToDtoDictionary.Add(entity, dto);
                }
                _tphEntityToDtoDictionary = new ReadOnlyDictionary<Type, Type>(tphEntityToDtoDictionary);
            }
            else
            {
                _tphEntityToDtoDictionary = new ReadOnlyDictionary<Type, Type>(new Dictionary<Type, Type> { { typeof(TEntity), typeof(TDto) } });
            }

            _tphDtoToEntityDictionary = new ReadOnlyDictionary<Type, Type>(_tphEntityToDtoDictionary.ToDictionary(s => s.Value, s => s.Key));
            _logger = logger;
            _dbContextBuilder = dbContextBuilder;
            using var context = _dbContextBuilder.CreateContext();
            var key = typeof(TDto).GetProperties()
                .Where(s => context.Model.FindEntityType(typeof(TEntity))
                    .FindPrimaryKey().Properties.Select(x => x.Name)
                    .Contains(s.Name)
                ).ToArray();
            _primaryKey = key.Any() ? key : throw new KeyNotFoundException();
        }

        public async Task<TDto> TryInsertOrUpdateAsync(TDto dto)
        {
            try
            {
                var dtoType = dto!.GetType();
                while (!dtoType.Name.EndsWith("Dto"))
                {
                    dtoType = dtoType.BaseType ?? throw new InvalidOperationException();
                }
                var entityType = _tphDtoToEntityDictionary[dtoType];

                await using var context = _dbContextBuilder.CreateContext();
                var entity = dto!.Adapt(dtoType, entityType);
                var dbset = context.Set<TEntity>();
                var value = _primaryKey.Select(primaryKey => primaryKey.GetValue(dto, null)).ToArray();
                var entityfound = await (value.Length > 1 ? dbset.FindAsync(value) : dbset.FindAsync(value.First())).ConfigureAwait(false);
                if (entityfound != null)
                {
                    context.Entry(entityfound).CurrentValues.SetValues(entity);
                }

                if ((value.Any(val => val == null)) || (entityfound == null))
                {
                    dbset.Add(entity as TEntity ?? throw new InvalidOperationException());
                }

                await context.SaveChangesAsync().ConfigureAwait(false);
                return (TDto)entity!.Adapt(entityType, dtoType)!;
            }
            catch (Exception e)
            {
                _logger.Error("{Level:u4} {Timestamp:HH:mm:ss} -- {Message:lj}{NewLine}{Exception}", e);
                return default!;
            }
        }

        public async Task<bool> TryInsertOrUpdateAsync(IEnumerable<TDto> dtos)
        {
            try
            {

                var enumerable = dtos.ToList();
                await using var context = _dbContextBuilder.CreateContext();

                var dbset = context.Set<TEntity>();
                var entitytoadd = new List<TEntity>();

                var dbkey2 = _primaryKey.Select(key => typeof(TEntity).GetProperty(key.Name)).ToArray();
                var list = enumerable.Select(dto =>
                {
                    var dtoType = dto!.GetType();
                    while (!dtoType.Name.EndsWith("Dto"))
                    {
                        dtoType = dtoType.BaseType ?? throw new InvalidOperationException();
                    }
                    var entityType = _tphDtoToEntityDictionary[dtoType];
                    return (TEntity) dto!.Adapt(dtoType, entityType)!;
                });

                var ids = _primaryKey.Length > 1 ? enumerable.Select(dto => _primaryKey.Select(part => part.GetValue(dto, null))).ToArray() : null;
                var ids2 = _primaryKey.Length == 1 ? enumerable.Select(dto => (TPk)_primaryKey.First().GetValue(dto, null)!).ToArray() : null;
                var entityfounds = (_primaryKey.Length > 1 ? dbset.FindAll(dbkey2, ids!) : dbset.FindAll(dbkey2, ids2!))
                    .ToDictionary(s => dbkey2.Select(part => part.GetValue(s, null)).GetTuple(), x => x);

                foreach (var entity in list)
                {
                    var key = dbkey2.Select(part => part.GetValue(entity, null)).GetTuple();
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
                _logger.Error("{Level:u4} {Timestamp:HH:mm:ss} -- {Message:lj}{NewLine}{Exception}", e);
                return false;
            }
        }

        public async Task<IEnumerable<TDto>?> TryDeleteAsync(IEnumerable<TPk> dtokeys)
        {
            try
            {
                await using var context = _dbContextBuilder.CreateContext();
                var dbset = context.Set<TEntity>();
                var dbkey = _primaryKey.Select(primaryKey => typeof(TEntity).GetProperty(primaryKey.Name)).ToArray();
                var toDelete = dbset.FindAll(dbkey, dtokeys.ToArray());
                var deletedDto = toDelete.ToList().Select(entity =>
                {
                    var entityType = entity.GetType();
                    var dtoType = _tphEntityToDtoDictionary[entityType];
                    return (TDto)entity.Adapt(entityType, dtoType)!;
                });
                dbset.RemoveRange(toDelete);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return deletedDto;
            }
            catch (Exception e)
            {
                _logger.Error("{Level:u4} {Timestamp:HH:mm:ss} -- {Message:lj}{NewLine}{Exception}", e);
                return null;
            }
        }

        public async Task<TDto> TryDeleteAsync(TPk dtokey)
        {
            if (dtokey.ToString() == default)
            {
                return default!;
            }

            try
            {
                TDto deletedDto = default!;
                await using var context = _dbContextBuilder.CreateContext();
                var dbset = context.Set<TEntity>();
                var key = dtokey is ITuple keyArray ? keyArray
                    .GetType()
                    .GetFields()
                    .Select(property => property.GetValue(keyArray))
                    .ToArray() : new object[] { dtokey };
                var entityfound = await dbset.FindAsync(key).ConfigureAwait(false);

                if (entityfound != null)
                {
                    var entityType = entityfound.GetType();
                    var dtoType = _tphEntityToDtoDictionary[entityType];
                    deletedDto = (TDto)entityfound.Adapt(entityType, dtoType)!;
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

        public async Task<TDto> FirstOrDefaultAsync(Expression<Func<TDto, bool>> predicate)
        {
            if (predicate == null)
            {
                return default!;
            }

            await using var context = _dbContextBuilder.CreateContext();
            var dbset = context.Set<TEntity>();
            var ent = await dbset.FirstOrDefaultAsync(predicate.ReplaceParameter<TDto, TEntity>()).ConfigureAwait(false);
            if (ent == null)
            {
                return default!;
            }
            var entityType = ent.GetType();
            var dtoType = _tphEntityToDtoDictionary[entityType];

            return (TDto)ent.Adapt(entityType, dtoType)!;
        }

        public IEnumerable<TDto> LoadAll()
        {
            using var context = _dbContextBuilder.CreateContext();
            return context.Set<TEntity>().ToList().Select(entity =>
            {
                var entityType = entity.GetType();
                var dtoType = _tphEntityToDtoDictionary[entityType];
                return (TDto)entity.Adapt(entityType, dtoType)!;
            });
        }

        public IEnumerable<TDto> Where(Expression<Func<TDto, bool>> predicate)
        {
            if (predicate == null)
            {
                return default!;
            }

            using var context = _dbContextBuilder.CreateContext();
            var dbset = context.Set<TEntity>();
            var entities = dbset.Where(predicate.ReplaceParameter<TDto, TEntity>());
            return entities.ToList().Select(entity =>
            {
                var entityType = entity.GetType();
                var dtoType = _tphEntityToDtoDictionary[entityType];
                return (TDto)entity.Adapt(entityType, dtoType)!;
            });
        }
    }
}
