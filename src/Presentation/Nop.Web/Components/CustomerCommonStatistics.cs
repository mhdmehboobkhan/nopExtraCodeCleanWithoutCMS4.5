using Microsoft.AspNetCore.Mvc;
using Nop.Web.Factories;
using Nop.Web.Framework.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nop.Web.Components
{
    public class CustomerCommonStatisticsViewComponent : NopViewComponent
    {
        private readonly ICommonModelFactory _commonModelFactory;

        public CustomerCommonStatisticsViewComponent(ICommonModelFactory commonModelFactory)
        {
            this._commonModelFactory = commonModelFactory;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var model = await _commonModelFactory.PrepareCommonStatisticsModel();
            return View(model);
        }
    }
}
