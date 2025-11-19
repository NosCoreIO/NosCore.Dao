//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// -----------------------------------

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace NosCore.Dao.Interfaces
{
    /// <summary>
    /// Base data access object interface for loading entities.
    /// </summary>
    /// <typeparam name="TDto">The DTO type</typeparam>
    public interface IDao<out TDto>
    {
        /// <summary>
        /// Loads all entities from the database.
        /// </summary>
        /// <returns>An enumerable collection of DTOs</returns>
        IEnumerable<TDto> LoadAll();
    }

    /// <summary>
    /// Extended data access object interface with CRUD operations.
    /// </summary>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <typeparam name="TPk">The primary key type</typeparam>
    public interface IDao<TDto, in TPk> : IDao<TDto>
    {
        /// <summary>
        /// Attempts to delete an entity by its primary key.
        /// </summary>
        /// <param name="dtokey">The primary key of the entity to delete</param>
        /// <returns>The deleted DTO, or default if not found</returns>
        Task<TDto> TryDeleteAsync(TPk dtokey);

        /// <summary>
        /// Attempts to delete multiple entities by their primary keys.
        /// </summary>
        /// <param name="dtokeys">The collection of primary keys to delete</param>
        /// <returns>An enumerable of deleted DTOs, or null if operation failed</returns>
        public Task<IEnumerable<TDto>?> TryDeleteAsync(IEnumerable<TPk> dtokeys);

        /// <summary>
        /// Gets the first entity matching the predicate or default value.
        /// </summary>
        /// <param name="predicate">The filter expression</param>
        /// <returns>The first matching DTO, or default if not found</returns>
        Task<TDto> FirstOrDefaultAsync(Expression<Func<TDto, bool>> predicate);

        /// <summary>
        /// Attempts to insert or update an entity.
        /// </summary>
        /// <param name="dto">The DTO to insert or update</param>
        /// <returns>The inserted or updated DTO</returns>
        Task<TDto> TryInsertOrUpdateAsync(TDto dto);

        /// <summary>
        /// Attempts to insert or update multiple entities.
        /// </summary>
        /// <param name="dtos">The collection of DTOs to insert or update</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> TryInsertOrUpdateAsync(IEnumerable<TDto> dtos);

        /// <summary>
        /// Filters entities based on a predicate.
        /// </summary>
        /// <param name="predicate">The filter expression</param>
        /// <returns>An enumerable of filtered DTOs, or null if operation failed</returns>
        IEnumerable<TDto>? Where(Expression<Func<TDto, bool>> predicate);
    }
}