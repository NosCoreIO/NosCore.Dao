using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.Dao.Extensions
{
    /// <summary>
    /// Extension methods for ModelBuilder.
    /// </summary>
    public static class ModelBuilderExtensions
    {
        /// <summary>
        /// Removes the pluralizing table name convention, using entity display names instead.
        /// </summary>
        /// <param name="modelBuilder">The model builder instance</param>
        public static void RemovePluralizingTableNameConvention(this ModelBuilder modelBuilder)
        {
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                if (entity.BaseType == null)
                {
                    entity.SetTableName(entity.DisplayName());
                }
            }
        }
    }
}
