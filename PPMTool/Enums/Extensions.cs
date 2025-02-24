using System;
using System.Linq;
using System.Reflection;
using DotNetExtensions;
using Radzen;

namespace PPMTool.Enums
{
    public static class Extensions
    {
        public static string ToNiceString(this Enum me)
        {
            return me.GetDescription() ?? me.ToString();
        }

        /// <summary>
        ///     A generic extension method that aids in reflecting 
        ///     and retrieving any attribute that is applied to an `Enum`.
        ///     https://stackoverflow.com/questions/13099834/how-to-get-the-display-name-attribute-of-an-enum-member-via-mvc-razor-code
        /// </summary>
        public static TAttribute GetAttribute<TAttribute>(this Enum enumValue)
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
        /// Custom attribute to assign a badge style to an enum
        /// </summary>
        public class BadgeStyleAttribute : Attribute
        {
            public BadgeStyleAttribute(BadgeStyle style)
            {
                Style = style;
            }

            public BadgeStyle Style { get; }
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
    }
}
