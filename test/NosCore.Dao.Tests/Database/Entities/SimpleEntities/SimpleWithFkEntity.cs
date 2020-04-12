//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// -----------------------------------

using System;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Dao.Tests.Database.Entities.SimpleEntities
{
    public class SimpleWithFkEntity
    {
        [Key]
        public int Key { get; set; }

        public string? Value { get; set; }

        public int Fk { get; set; }

        public virtual SimpleEntity FkEntity { get; set; } = null!;
    }
}
