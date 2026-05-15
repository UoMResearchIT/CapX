using System.ComponentModel;
using System.Reflection;
using PPMTool.Data.Enums.Attributes;
using Radzen;

namespace PPMTool.Data.Enums
{
    public static class Extensions
    {
        /// <summary>
        /// Extension method to get the description attribute of an enum value
        /// </summary>
        /// <param name="genericEnum"></param>
        /// <returns></returns>
        public static string GetDescription(this Enum genericEnum)
        {
            Type genericEnumType = genericEnum.GetType();
            MemberInfo[] memberInfo = genericEnumType.GetMember(genericEnum.ToString());
            if ((memberInfo != null && memberInfo.Length > 0))
            {
                var _Attribs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                if ((_Attribs != null && _Attribs.Count() > 0))
                {
                    return ((DescriptionAttribute)_Attribs.ElementAt(0)).Description;
                }
            }
            return genericEnum.ToString();
        }

        /// <summary>
        /// Extension method to get the default setting value attribute of a setting type enum value.
        /// If it doesn't have the attribute, returns the name of the enum value.
        /// </summary>
        /// <param name="settingType"></param>
        /// <returns></returns>
        public static string GetDefaultSettingValue(this SettingType settingType)
        {
            MemberInfo[] memberInfo = settingType.GetType().GetMember(settingType.ToString());
            if ((memberInfo != null && memberInfo.Length > 0))
            {
                var _Attribs = memberInfo[0].GetCustomAttributes(typeof(DefaultSettingValueAttribute), false);
                if ((_Attribs != null && _Attribs.Count() > 0))
                {
                    return ((DefaultSettingValueAttribute)_Attribs.ElementAt(0)).Value;
                }
            }
            return settingType.ToString();
        }

        /// <summary>
        /// Whether a cost model is one which carries indirects
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static bool HasIndirects(this CostModel model)
        {
            return model == CostModel.TechOnlyWithIndirects || model == CostModel.TechAndLeadershipWithIndirects;
        }

        /// <summary>
        /// Whether a cost model is one with leadership charges
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static bool HasLeadership(this CostModel model)
        {
            return model == CostModel.TechAndLeadership || model == CostModel.TechAndLeadershipWithIndirects;
        }

        /// <summary>
        /// Makes use of the decription attribute to convert to a string if it is avaialble
        /// </summary>
        /// <param name="me"></param>
        /// <returns></returns>
        public static string ToNiceString(this Enum me)
        {
            return me.GetDescription() ?? me.ToString();
        }

        /// <summary>
        ///     A generic extension method that aids in reflecting 
        ///     and retrieving any attribute that is applied to an `Enum`.
        ///     https://stackoverflow.com/questions/13099834/how-to-get-the-display-name-attribute-of-an-enum-member-via-mvc-razor-code
        /// </summary>
        public static TAttribute? GetAttribute<TAttribute>(this Enum enumValue)
                where TAttribute : Attribute
        {
            return enumValue.GetType()
                            .GetMember(enumValue.ToString())
                            .First()
                            .GetCustomAttribute<TAttribute>();
        }

        /// <summary>
        /// Gets the background colour code from the enum if it has the attribute. Otherwise returns UoM purple.
        /// </summary>
        /// <param name="enumValue"></param>
        /// <returns></returns>
        public static string GetBackgroundColourCode(this Enum enumValue)
        {
            MemberInfo[] member = enumValue.GetType().GetMember(enumValue.ToString());
            if (member != null && member.Length != 0)
            {
                object[] customAttributes = member[0].GetCustomAttributes(typeof(ColourAttribute), inherit: false);
                if (customAttributes != null && customAttributes.Count() > 0)
                {
                    return ((ColourAttribute)customAttributes.ElementAt(0)).BackgroundColourCode;
                }
            }

            return "#609";
        }

        /// <summary>
        /// Gets the text colour code from the enum if it has the attribute. Otherwise returns white.
        /// </summary>
        /// <param name="enumValue"></param>
        /// <returns></returns>
        public static string GetTextColourCode(this Enum enumValue)
        {
            MemberInfo[] member = enumValue.GetType().GetMember(enumValue.ToString());
            if (member != null && member.Length != 0)
            {
                object[] customAttributes = member[0].GetCustomAttributes(typeof(ColourAttribute), inherit: false);
                if (customAttributes != null && customAttributes.Count() > 0)
                {
                    return ((ColourAttribute)customAttributes.ElementAt(0)).TextColourCode;
                }
            }

            return "#FFF";
        }

        /// <summary>
        /// Project status is one of the cancelled states or finished
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public static bool IsFinishedOrCancelled(this ProjectStatus status)
        {
            return
                status == ProjectStatus.Finished ||
                status == ProjectStatus.CancelledByCustomer ||
                status == ProjectStatus.CancelledBidFailed ||
                status == ProjectStatus.CancelledNoResource;
        }

        /// <summary>
        /// Project status is one of the cancelled states
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public static bool IsCancelled(this ProjectStatus status)
        {
            return
                status == ProjectStatus.CancelledByCustomer ||
                status == ProjectStatus.CancelledBidFailed ||
                status == ProjectStatus.CancelledNoResource;
        }

        /// <summary>
        /// Project status is unfunded or cancelled
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public static bool IsUnconfirmed(this ProjectStatus status)
        {
            return
                status.IsCancelled() ||
                status.IsUnfunded();
        }

        /// <summary>
        /// Project status is one of the pre-funded statuses
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public static bool IsUnfunded(this ProjectStatus status)
        {
            return
                status == ProjectStatus.NewRequest ||
                status == ProjectStatus.AwaitingSubmission ||
                status == ProjectStatus.AwaitingOutcome;
        }

        /// <summary>
        /// The project ran i.e. it is in the active, paused, maintenance or finished state
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public static bool DidRun(this ProjectStatus status)
        {
            return
                status == ProjectStatus.Active ||
                status == ProjectStatus.Paused ||
                status == ProjectStatus.Maintenance ||
                status == ProjectStatus.Finished;
        }

        /// <summary>
        /// Check if it is in the active, paused or maintenance state
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public static bool IsActive(this ProjectStatus status)
        {
            return
                status == ProjectStatus.Active ||
                status == ProjectStatus.Paused ||
                status == ProjectStatus.Maintenance;
        }

        /// <summary>
        /// Gets the badge style of an enum based on the attribute
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public static BadgeStyle GetBadgeStyle(this Enum status)
        {
            return status.GetAttribute<BadgeStyleAttribute>()?.Style ?? BadgeStyle.Light;
        }

        /// <summary>
        /// Returns a CSS background-color tag
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public static string GetBackgroundCss(this AssessmentStatus status)
        {
            if (status == AssessmentStatus.FullyMet)
            {
                return "background-color: var(--rz-success-lighter);";
            }
            else if (status == AssessmentStatus.PartiallyMet)
            {
                return "background-color: var(--rz-warning-lighter);";
            }
            return "background-color: var(--rz-danger-lighter);";
        }

        /// <summary>
        /// Test whether a link is valid URL
        /// </summary>
        /// <param name="test"></param>
        /// <returns></returns>
        public static bool IsValidURL(this string test)
        {
            return !string.IsNullOrWhiteSpace(test) && test.StartsWith("http") && test.Length >= 12 && Uri.TryCreate(test, UriKind.Absolute, out _);
        }
    }
}
