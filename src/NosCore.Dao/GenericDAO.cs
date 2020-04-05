//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using NosCore.Dao.Extensions;
using NosCore.Dao.Interfaces;
using Serilog;

namespace NosCore.Dao
{
    public class GenericDao<TEntity, TDto, TPk> : IGenericDao<TDto, TPk>
    where TEntity : class
    {
        private readonly ILogger _logger;
        private readonly PropertyInfo[] _primaryKey;
        private readonly IDbContextBuilder _dbContextBuilder;

        public GenericDao(ILogger logger, IDbContextBuilder dbContextBuilder)
        {
            _logger = logger;
            _dbContextBuilder = dbContextBuilder;
            var key = typeof(TDto).FindKey();
            _primaryKey = key ?? throw new KeyNotFoundException();
        }

        public async Task<TDto> TryInsertOrUpdateAsync(TDto dto)
        {
            try
            {
                await using var context = _dbContextBuilder.CreateContext();
                var entity = dto!.Adapt<TEntity>();
                var dbset = context.Set<TEntity>();
                var value = _primaryKey.Select(primaryKey => primaryKey.GetValue(dto, null)).ToArray<object>();
                var entityfound = value.Length > 1 ? dbset.Find(value) : dbset.Find(value.First());
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
            IEnumerable<Tuple<TEntity, TPk>> ConvertToTypedList(List<Tuple<TEntity, dynamic>> list)
            {
                foreach (var entity in list)
                {
                    if (!(entity.Item2 is object[] key))
                    {
                        throw new InvalidCastException();
                    }
                    if (key.Length > 1)
                    {
                        yield return new Tuple<TEntity, TPk>(entity.Item1, (TPk)entity.Item2);
                    }
                    else
                    {
                        yield return new Tuple<TEntity, TPk>(entity.Item1, (TPk)key.First());
                    }
                }
            }

            try
            {
                await using var context = _dbContextBuilder.CreateContext();

                var dbset = context.Set<TEntity>();
                var entitytoadd = new List<TEntity>();
                var list = dtos.Select(dto => new Tuple<TEntity, dynamic>(dto!.Adapt<TEntity>(), _primaryKey.Select(composite => composite.GetValue(dto, null)!).ToArray())).ToList();
                var typedList = ConvertToTypedList(list).ToList();
                var ids = typedList.Select(s => s.Item2).ToArray();
                var dbkey = _primaryKey.Select(primaryKey => typeof(TEntity).GetProperty(primaryKey.Name)).ToArray();
                var entityfounds = dbset.FindAll(dbkey!, ids).ToList();
                foreach (var (entity, item2) in typedList)
                {
                    var entityfound = entityfounds.Find(s => (dynamic?)dbkey.First()?.GetValue(s, null) == item2);
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
                var toDelete = dbset.FindAll(dbkey!, dtokeys.ToArray());
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
            try
            {
                TDto deletedDto = default!;
                await using var context = _dbContextBuilder.CreateContext();
                var dbset = context.Set<TEntity>();
                var entityfound = dbset.Find(dtokey);

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
