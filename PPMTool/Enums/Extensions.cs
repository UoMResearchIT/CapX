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
        /// Method to return a badge style based on status
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public static BadgeStyle GetBadgeStyle(this AssessmentStatus status)
        {
            if (status == AssessmentStatus.FullyMet)
            {
                return BadgeStyle.Success;
            }
            else if (status == AssessmentStatus.PartiallyMet)
            {
                return BadgeStyle.Warning;
            }
            return BadgeStyle.Danger;
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
        /// Gets the badge style of a timesheet status based on the attribute
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public static BadgeStyle GetBadgeStyle(this TimesheetStatus status)
        {
            return status.GetAttribute<BadgeStyleAttribute>()?.Style ?? BadgeStyle.Light;
        }
    }
}
