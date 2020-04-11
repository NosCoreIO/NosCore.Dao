//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 

namespace NosCore.Dao.Tests.TestsModels.TphModels
{
    public class TphBaseDto : ITphDto
    {
        public int Key { get; set; }

        public string? Value { get; set; }
    }
}
