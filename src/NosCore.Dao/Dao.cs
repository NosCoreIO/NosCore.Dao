//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 

using System;
using System.Collections;
using System.Collections.Generic;
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

        public Dao(ILogger logger, IDbContextBuilder dbContextBuilder)
        {
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
                await using var context = _dbContextBuilder.CreateContext();
                var entity = dto!.Adapt<TEntity>();
                var dbset = context.Set<TEntity>();
                var value = _primaryKey.Select(primaryKey => primaryKey.GetValue(dto, null)).ToArray();
                var entityfound = await (value.Length > 1 ? dbset.FindAsync(value) : dbset.FindAsync(value.First())).ConfigureAwait(false);
                if (entityfound != null)
                {
                    context.Entry(entityfound).CurrentValues.SetValues(entity);
                }

                if ((value.Any(val => val == null)) || (entityfound == null))
                {
                    dbset.Add(entity);
                }

                await context.SaveChangesAsync().ConfigureAwait(false);
                return entity.Adapt<TDto>();
            }
            catch (Exception e)
            {
                _logger.Error(e.Message, e);
                return default!;
            }
        }

        public async Task<bool> TryInsertOrUpdateAsync(IEnumerable<TDto> dtos)
        {
            try
            {
                await using var context = _dbContextBuilder.CreateContext();

                var dbset = context.Set<TEntity>();
                var entitytoadd = new List<TEntity>();
                var enumerable = dtos.ToList();
                var ids2 = _primaryKey.Length == 1 ? enumerable.Select(dto => new Tuple<TEntity, TPk>(dto!.Adapt<TEntity>(), (TPk)_primaryKey.First().GetValue(dto, null)!)).Select(s => s.Item2).ToArray() : null;
                var dbkey2 = _primaryKey.Select(key => typeof(TEntity).GetProperty(key.Name)).ToArray();
                var list = enumerable.Select(dto => new Tuple<TEntity, IEnumerable>(dto!.Adapt<TEntity>(), _primaryKey.Select(part => part.GetValue(dto, null)))).ToList();
                var ids = list.Select(s => s.Item2).ToArray();
                var entityKey = typeof(TEntity).GetProperties()
                    .Where(p => _primaryKey.Select(s => s.Name).Contains(p.Name)).ToArray();
                var entityfounds = (_primaryKey.Length > 1 ? dbset.FindAll(_primaryKey, ids) : dbset.FindAll(dbkey2, ids2!))
                    .ToDictionary(s => string.Join(",", entityKey.Select(part => part.GetValue(s, null))), x => x);

                foreach (var entity in list.Select(s => s.Item1))
                {
                    var dbKeys = _primaryKey.Select(s => s.Name).ToArray<object>();
                    var key = string.Join(",", entityKey.Select(part => part.GetValue(entity, null)));
                    var entityfound = entityfounds.ContainsKey(key) ? entityfounds[key]  : null;
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
                _logger.Error(e.Message, e);
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
                var deletedDto = toDelete.Adapt<IEnumerable<TDto>>().ToList();
                dbset.RemoveRange(toDelete);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return deletedDto;
            }
            catch (Exception e)
            {
                _logger.Error(e.Message, e);
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
                    deletedDto = entityfound.Adapt<TDto>();
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
            return ent.Adapt<TDto>();
        }

        public IEnumerable<TDto> LoadAll()
        {
            using var context = _dbContextBuilder.CreateContext();
            return context.Set<TEntity>().ToList().Adapt<IEnumerable<TDto>>();
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
            return entities.Adapt<IEnumerable<TDto>>().ToList();
        }
    }
}
