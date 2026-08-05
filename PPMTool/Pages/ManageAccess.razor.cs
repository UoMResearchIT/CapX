// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Linq.Dynamic.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Superuser")]
    public partial class ManageAccess : DataGridPage<User>
    {
        [Inject]
        public PersonService PersonService { get; set; }

        private List<Person> people;
        private List<RoleType> roles;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            CallingPage = "AccessControl";
            dataGridEntityService = UserService;
            dataGridEntities = UserService.GetAll(Context).OrderBy(x => x.GetName()).ToList();

            // Populate the people and role types for the dropdowns
            roles = Enum.GetValues<RoleType>().ToList();
            people = PersonService.GetAll(Context).OrderBy(x => x.Name).ToList();

            LogInformation($"Viewing access grid");
        }

        protected override async Task DeleteRow(User entity)
        {
            if (await DialogService.Confirm($"You are about to delete access record {entity.GetSensibleObjectName()}.", "Delete Access") ?? false)
            {
                await base.DeleteRow(entity);
                UserService.Delete(Context, entity);
                LogInformation($"Deleted access record for {entity.GetSensibleObjectName()}");
            }
        }

        /// <summary>
        /// Override the save row method to allow interruption to do custom validatoin and display an error.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected override async Task SaveRow(User entity)
        {
            // Validate
            if (string.IsNullOrWhiteSpace(entity.CASUserName))
            {
                SetError("You must supply a user name");
                return;
            }
            if (string.IsNullOrWhiteSpace(entity.Name))
            {
                SetError("You must give the user a name");
                return;
            }
            if (string.IsNullOrWhiteSpace(entity.EmailAddress))
            {
                SetError("Users must have a valid email address");
                return;
            }

            await base.SaveRow(entity);
        }

        /// <summary>
        /// Wrapper to set an error message at the top of the page and fire a notification
        /// </summary>
        /// <param name="message"></param>
        private void SetError(string message)
        {
            ShowNotification(new CapXNotificationMessage
            {
                Summary = "Oops!",
                Detail = message,
                Severity = NotificationSeverity.Error
            });
            SetErrorMessage(new StatusMessage(message, StatusMessage.MessageType.Error));
        }
    }
}