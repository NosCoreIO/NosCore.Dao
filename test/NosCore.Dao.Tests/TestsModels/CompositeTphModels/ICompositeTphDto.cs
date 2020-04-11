//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|

namespace NosCore.Dao.Tests.TestsModels.CompositeTphModels
{
    public interface ICompositeTphDto
    {
        public int Key1 { get; set; }

        public int Key2 { get; set; }

        string? Value { get; set; }
    }
}