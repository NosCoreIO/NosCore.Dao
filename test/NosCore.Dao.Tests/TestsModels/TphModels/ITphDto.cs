//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|

namespace NosCore.Dao.Tests.TestsModels.TphModels
{
    public interface ITphDto
    {
        int Key { get; set; }

        string? Value { get; set; }
    }
}