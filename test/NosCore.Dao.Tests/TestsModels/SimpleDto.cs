using System.ComponentModel.DataAnnotations;

namespace NosCore.Dao.Tests.TestsModels
{
    public class SimpleDto
    {
        [Key]
        public int Key { get; set; }

        public string? Value { get; set; }
    }
}
