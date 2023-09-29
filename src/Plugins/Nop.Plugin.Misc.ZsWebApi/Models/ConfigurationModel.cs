using System.ComponentModel.DataAnnotations;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Misc.ZsWebApi.Models
{
    public record ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }
        
        [NopResourceDisplayName("Plugins.Misc.ZsWebApi.Enable")]
        public bool Enable { get; set; }
        public bool Enable_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Misc.ZsWebApi.NSTToken")]
        public string NSTToken { get; set; }

        [NopResourceDisplayName("Plugins.Misc.ZsWebApi.NSTKey")]
        public string NSTKey { get; set; }
        public bool NSTKey_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Misc.ZsWebApi.NSTSecret")]
        public string NSTSecret { get; set; }
        public bool NSTSecret_OverrideForStore { get; set; }
    }
}