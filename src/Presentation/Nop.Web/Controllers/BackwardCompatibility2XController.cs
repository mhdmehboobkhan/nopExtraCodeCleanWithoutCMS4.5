using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Services.Seo;
using Nop.Services.Topics;

namespace Nop.Web.Controllers
{
    //do not inherit it from BasePublicController. otherwise a lot of extra action filters will be called
    //they can create guest account(s), etc
    public partial class BackwardCompatibility2XController : Controller
    {
        #region Fields

        private readonly ITopicService _topicService;
        private readonly IUrlRecordService _urlRecordService;

        #endregion

        #region Ctor

        public BackwardCompatibility2XController(ITopicService topicService,
            IUrlRecordService urlRecordService)
        {
            _topicService = topicService;
            _urlRecordService = urlRecordService;
        }

        #endregion

        #region Methods

        //in versions 2.00-3.20 we had SystemName in topic URLs
        public virtual async Task<IActionResult> RedirectTopicBySystemName(string systemName)
        {
            var topic = await _topicService.GetTopicBySystemNameAsync(systemName);
            if (topic == null)
                return RedirectToRoutePermanent("Homepage");

            return RedirectToRoutePermanent("Topic", new { SeName = await _urlRecordService.GetSeNameAsync(topic) });
        }

        #endregion
    }
}