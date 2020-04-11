//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// -----------------------------------

namespace NosCore.Dao.Tests.TestsModels.CompositeTphModels
{
    public class CompositeTphBaseDto : ICompositeTphDto
    {
        public int Key1 { get; set; }

        public int Key2 { get; set; }

        public string? Value { get; set; }
    }
}
