using System;

namespace Nop.Core.Domain.Messages
{
    /// <summary>
    /// MessageTemplate Extensions
    /// </summary>
    public static class MessageTemplateExtensions
    {
        /// <summary>
        /// Returns full template with basic styles
        /// </summary>
        /// <param name="period">Message template</param>
        /// <param name="value">Value of delay send</param>
        /// <returns>Value of message delay in hours</returns>
        public static string ToBasicUIStyle(this MessageTemplate template)
        {
            if (template.Body.Contains("background-color: #eaeaea"))
                return template.Body;

            return "<p>&nbsp;</p> <!-- copy from here --> <div style=\"background-color: #eaeaea;\"> <div class=\"adM\">&nbsp;</div> " +
                "<table width=\"100%\" bgcolor=\"#EAEAEA\"> <tbody> <tr> <td style=\"padding: 20px;\"> " +
                "<table class=\"m_-235875566056356318mainText\" style=\"background-color: #fff;\" width=\"600\" align=\"center\"> <tbody> " +
                "<tr> <td style=\"background-color: #ffffff; padding: 10px;\" align=\"center\"><a href=\"%Store.URL%\"> <img style=\"margin: auto; display: table; max-width: 300px;\" title=\"%Store.Name%\" src=\"%Store.Logo%\" alt=\"%Store.Name%\" /> </a></td> </tr> " +
                "<tr> <td style=\"padding: 10px;\" align=\"left\">  " + template.Body +
                "</td> </tr> </tbody> </table> </td> </tr> </tbody> </table> </div>";
        }
    }
}