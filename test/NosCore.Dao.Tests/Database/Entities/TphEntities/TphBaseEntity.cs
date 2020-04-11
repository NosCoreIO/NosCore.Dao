//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 

using System.ComponentModel.DataAnnotations;

namespace NosCore.Dao.Tests.Database.Entities
{
    public class TphBaseEntity : ITphEntity
    {
        [Key]
        public int Key { get; set; }

        public string? Value { get; set; }
    }
}
