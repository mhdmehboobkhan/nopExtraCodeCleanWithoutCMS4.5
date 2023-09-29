using Nop.Core.Configuration;

namespace Nop.Plugin.Misc.ZsWebApi
{
    public class ZsWebApiSettings : ISettings
    {
        public bool Enable { get; set; }
        public string NSTKey { get; set; }
        public string NSTSecret { get; set; }
        public string NSTToken { get; set; }
    }
}