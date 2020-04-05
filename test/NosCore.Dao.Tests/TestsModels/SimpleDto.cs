using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace NosCore.Dao.Tests.TestsModels
{
    public class SimpleDto
    {
        [Key]
        public int Key { get; set; }

        public string? Value { get; set; }
    }
}
