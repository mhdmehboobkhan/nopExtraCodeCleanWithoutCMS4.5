using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Customers;
using Nop.Services.Media;

namespace Nop.Web.Controllers
{
    public partial class PictureController : BasePublicController
    {
        #region Fields

        private readonly IPictureService _pictureService;
        private readonly IWorkContext _workContext;
        private readonly ICustomerService _customerService;

        #endregion

        #region Ctor

        public PictureController(IPictureService pictureService,
            IWorkContext workContext,
            ICustomerService customerService)
        {
            _pictureService = pictureService;
            _workContext = workContext;
            _customerService = customerService;
        }

        #endregion

        #region Methods

        [HttpPost]
        //do not validate request token (XSRF)
        [IgnoreAntiforgeryToken]
        public virtual async Task<IActionResult> AsyncUpload()
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (!await _customerService.IsRegisteredAsync(customer))
                return Json(new { success = false, message = "Customer cannot be loaded" });

            var httpPostedFile = Request.Form.Files.FirstOrDefault();
            if (httpPostedFile == null)
                return Json(new { success = false, message = "No file uploaded" });

            const string qqFileNameParameter = "qqfilename";

            var qqFileName = Request.Form.ContainsKey(qqFileNameParameter)
                ? Request.Form[qqFileNameParameter].ToString()
                : string.Empty;

            var picture = await _pictureService.InsertPictureAsync(httpPostedFile, qqFileName);

            //when returning JSON the mime-type must be set to text/plain
            //otherwise some browsers will pop-up a "Save As" dialog.

            if (picture == null)
                return Json(new { success = false, message = "Wrong file format" });

            return Json(new
            {
                success = true,
                pictureId = picture.Id,
                imageUrl = (await _pictureService.GetPictureUrlAsync(picture, 100)).Url
            });
        }

        #endregion
    }
}