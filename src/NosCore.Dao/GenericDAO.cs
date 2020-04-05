//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Mapster;
using Microsoft.EntityFrameworkCore;
using NosCore.Dao.Extensions;
using NosCore.Dao.Interfaces;
using Serilog;

namespace NosCore.Dao
{
    public class GenericDao<TEntity, TDto, TPk> : IGenericDao<TDto, TPk>
    where TEntity : class
    {
        private readonly ILogger _logger;
        private readonly PropertyInfo _primaryKey;
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
                var value = _primaryKey.GetValue(dto, null);
                var entityfound = value is object[] objects ? dbset.Find(objects) : dbset.Find(value);
                if (entityfound != null)
                {
                    context.Entry(entityfound).CurrentValues.SetValues(entity);
                }

                if ((value == null) || (entityfound == null))
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
                var list = dtos.Select(dto => new Tuple<TEntity, TPk>(dto!.Adapt<TEntity>(), (TPk)_primaryKey!.GetValue(dto, null)!)).ToList();

                var ids = list.Select(s => s.Item2).ToArray();
                var dbkey = typeof(TEntity).GetProperty(_primaryKey!.Name);
                var entityfounds = dbset.FindAll(dbkey!, ids).ToList();
                foreach (var dto in list)
                {
                    var entity = dto.Item1;
                    var entityfound =
                        entityfounds.FirstOrDefault(s => (dynamic?)dbkey?.GetValue(s, null) == dto.Item2);
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
                var dbkey = typeof(TEntity).GetProperty(_primaryKey!.Name);
                var toDelete = dbset.FindAll(dbkey!, dtokeys.ToArray());
                var deletedDto = toDelete.Adapt<IEnumerable<TDto>>();
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
