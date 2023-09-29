using Nop.Core.Caching;

namespace Nop.Services.Common
{
    /// <summary>
    /// Represents default values related to common services
    /// </summary>
    public static partial class NopCommonDefaults
    {
        /// <summary>
        /// Gets a request path to the keep alive URL
        /// </summary>
        public static string KeepAlivePath => "keepalive/index";

        #region Maintenance

        /// <summary>
        /// Gets a default timeout (in milliseconds) before restarting the application
        /// </summary>
        public static int RestartTimeout => 3000;

        /// <summary>
        /// Gets a path to the database backup files
        /// </summary>
        public static string DbBackupsPath => "db_backups\\";

        /// <summary>
        /// Gets a database backup file extension
        /// </summary>
        public static string DbBackupFileExtension => "bak";

        #endregion

        #region Favicon and app icons

        /// <summary>
        /// Gets a name of the file with code for the head element
        /// </summary>
        public static string HeadCodeFileName => "html_code.html";

        /// <summary>
        ///  Gets a head link for the favicon
        /// </summary>
        public static string SingleFaviconHeadLink => "<link rel=\"shortcut icon\" href=\"/icons/icons_{0}/{1}\">";

        /// <summary>
        /// Gets a path to the favicon and app icons
        /// </summary>
        public static string FaviconAndAppIconsPath => "icons/icons_{0}";

        /// <summary>
        /// Gets a name of the old favicon icon for current store
        /// </summary>
        public static string OldFaviconIconName => "favicon-{0}.ico";

        #endregion

        #region Localization client-side validation

        /// <summary>
        /// Gets a path to the localization client-side validation 
        /// </summary>
        public static string LocalePatternPath => "lib_npm/cldr-data/main/{0}";

        /// <summary>
        /// Gets a name of the archive with localization of templates
        /// </summary>
        public static string LocalePatternArchiveName => "main.zip";

        /// <summary>
        /// Gets a name of the default pattern locale
        /// </summary>
        public static string DefaultLocalePattern => "en";

        /// <summary>
        /// Gets default CultureInfo 
        /// </summary>
        public static string DefaultLanguageCulture => "en-US";

        /// <summary>
        /// Gets minimal progress of language pack translation to download and install
        /// </summary>
        public static int LanguagePackMinTranslationProgressToInstall => 80;

        /// <summary>
        /// Gets a name of generic attribute to store the value of 'LanguagePackProgress'
        /// </summary>
        public static string LanguagePackProgressAttribute => "LanguagePackProgress";

        #endregion

        #region Caching defaults

        #region Generic attributes

        /// <summary>
        /// Gets a key for caching
        /// </summary>
        /// <remarks>
        /// {0} : entity ID
        /// {1} : key group
        /// </remarks>
        public static CacheKey GenericAttributeCacheKey => new("Nop.genericattribute.{0}-{1}");

        #endregion

        #endregion
    }
}
