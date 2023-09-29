using Nop.Web.Framework.Models;

namespace Nop.Web.Areas.Admin.Models.Templates
{
    /// <summary>
    /// Represents a templates model
    /// </summary>
    public partial record TemplatesModel : BaseNopModel
    {
        #region Ctor

        public TemplatesModel()
        {
            TemplatesTopic = new TopicTemplateSearchModel();
            AddTopicTemplate = new TopicTemplateModel();
        }

        #endregion

        #region Properties

        public TopicTemplateSearchModel TemplatesTopic { get; set; }

        public TopicTemplateModel AddTopicTemplate { get; set; }

        #endregion
    }
}
