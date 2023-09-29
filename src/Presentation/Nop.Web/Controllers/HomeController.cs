using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Customers;
using Nop.Web.Factories;
using System.Threading.Tasks;

namespace Nop.Web.Controllers
{
    public partial class HomeController : BasePublicController
    {
        private readonly ICustomerService _customerService;
        private readonly IWorkContext _workContext;
        private readonly ICommonModelFactory _commonModelFactory;

        public HomeController(ICommonModelFactory commonModelFactory,
            ICustomerService customerService,
            IWorkContext workContext)
        {
            _commonModelFactory = commonModelFactory;
            _customerService = customerService;
            _workContext = workContext;
        }

        public virtual IActionResult Index()
        {
            return View();
        }
        public virtual async Task<IActionResult> DashBoard()
        {
            if (!await _customerService.IsRegisteredAsync(await _workContext.GetCurrentCustomerAsync()))
                return Challenge();

            var model = await _commonModelFactory.PrepareDashboardModelAsync();
            return View(model);
        }
    }
}