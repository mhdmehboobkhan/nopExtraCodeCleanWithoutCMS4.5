using System.Collections.Generic;
using System.Collections.Specialized;
using Nop.Core.Domain.Common;
using Nop.Plugin.Misc.ZsWebApi.Models.Common;

namespace Nop.Plugin.Misc.ZsWebApi.Extensions
{
    public static class MappingExtensions
    {
        public static NameValueCollection ToNameValueCollection(this List<KeyValueApi> formValues)
        {
            var form = new NameValueCollection();
            foreach (var values in formValues)
            {
                form.Add(values.Key, values.Value);
            }
            return form;
        }
    }
}
