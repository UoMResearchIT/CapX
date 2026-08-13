// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Data;
using System.Diagnostics;
using System.Net.Mail;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;

namespace PPMTool.Services
{
    /// <summary>
    /// Service offering email sending capabilities
    /// </summary>
    public class EmailService
    {
        public EmailService(
            IConfiguration configuration,
            ProjectService projectService,
            UserService userService,
            PersonService personService,
            IDbContextFactory<PPMToolContext> dbContextFactory,
            ILogger logger
        )
        {
            Configuration = configuration;
            ProjectService = projectService;
            UserService = userService;
            PersonService = personService;
            DbContextFactory = dbContextFactory;
            Logger = logger;
        }

        public IConfiguration Configuration { get; }
        public ProjectService ProjectService { get; }
        public UserService UserService { get; }
        public PersonService PersonService { get; }
        public IDbContextFactory<PPMToolContext> DbContextFactory { get; }
        public ILogger Logger { get; }

        /// <summary>
        /// Send an email to the recipient provided.
        /// </summary>
        /// <param name="to"></param>
        /// <param name="subject"></param>
        /// <param name="message"></param>
        public void SendEmail(string to, string subject, string message)
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(Configuration["Email:From"]),
                Subject = subject,
                Body = message,
                IsBodyHtml = true,
            };
            mailMessage.To.Add(to);

            Logger.LogInformation($"Sending email to {AnonymiseEmail(to)}, subject {mailMessage.Subject}");

#if RELEASE
            // Launch a background task to do the sending
            Task.Run(() =>
            {
                try
                {
                    // Send
                    using var client = new SmtpClient(Configuration["Email:SmtpServer"]);
                    client.Send(mailMessage);
                }
                catch (Exception e)
                {
                    Logger.LogInformation($"Failed to send email to {AnonymiseEmail(to)}, subject {mailMessage.Subject}");
                }
            });
#endif
        }

        /// <summary>
        /// Send a timesheet submission email notification to the staff member's line manager
        /// </summary>
        /// <param name="staff"></param>
        /// <param name="timesheet"></param>
        public async Task SendTimesheetSubmissionEmailNotificationAsync(Person staff, Timesheet timesheet)
        {
            List<string> recipients = new List<string>();

            // Run a background thread to do the sending and updating
            await Task.Run(async () =>
            {
                try
                {
                    // Create context and get relevant details for the email
                    using (var context = DbContextFactory.CreateDbContext())
                    {
                        Person lineManager = staff.LineManager;

                        if (lineManager != staff) // No point emailing someone about their own timesheet if they are their own line manager. :)
                        {
                            User lineManagerUser = UserService.GetAll(context).First(p => p.Person.PersonId == lineManager.PersonId);
                            var lineManagerEmailAddresses = lineManagerUser.GetNormalisedEmailAddresses();
                            if (lineManagerEmailAddresses.Any())
                            {
                                foreach (var lineManagerEmailAddress in lineManagerEmailAddresses)
                                {
                                    recipients.Add(lineManagerEmailAddress);
                                }
                            }

                            // Only build the email and send it if there are any email addresses
                            if (recipients.Any())
                            {
                                // Create email
                                var subject = $"{Configuration["Email:TimesheetSubmissionEmailSubject"]}. {staff.ShortName} [{timesheet.StartDate.ToString("dd/MM/yy")}]";

                                StringBuilder body = new StringBuilder();
                                body.Append($"<p>Dear {lineManager.Name},</p>");
                                body.Append($"<p>{Configuration["Email:TimesheetSubmissionEmailBody"]} by {staff.Name} for the week commencing {timesheet.StartDate.ToString("dd/MM/yy")}.</p>");
                                body.Append($"<p>{Configuration["Email:TimesheetSubmissionEmailEndBody"]}</p>");
                                body.Append($"<p><a href=\"{Configuration["Authentication:HostUrl"]}/timesheets/addtimesheet/{timesheet.TimesheetId.ToString()}\">Review this timesheet on CapX</a></p>");
                                body.Append("<p><em>Sent from CapX</em></p>");

                                // Send email
                                Debug.WriteLine($"** Sending Timesheet Submission email to {string.Join("|", recipients.Select(AnonymiseEmail))}");
                                foreach (var recipient in recipients)
                                {
                                    SendEmail(recipient, subject, body.ToString());
                                    await Task.Delay(1000);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError($"Timesheet email failure: {e}");
                }
            });
        }

        /// <summary>
        /// Send absence email notifications to relevant project managers
        /// </summary>
        /// <param name="personId">Manually supply the person ID as deletion may not have this info any more</param>
        /// <param name="newAbsences"></param>
        /// <param name="modifiedAbsences"></param>
        /// <param name="deletedAbsences"></param>
        public async Task SendAbsenceEmailNotificationsAsync(
            int personId,
            IEnumerable<Absence> newAbsences,
            IEnumerable<IGrouping<Absence, EntityDiff<Absence>>> modifiedAbsences,
            IEnumerable<Absence> deletedAbsences)
        {
            // Run this task on a background thread
            await Task.Run(async () =>
            {
                try
                {
                    // Create context and get people for lookup
                    using (var context = DbContextFactory.CreateDbContext())
                    {
                        // Get all people who are in the user list
                        var people = UserService
                            .GetAll(context)
                            .Select(x => x.Person)
                            .Where(p => p != null)
                            .DistinctBy(x => x.Name);

                        // Get name of the affected person
                        var absentPerson = people.FirstOrDefault(x => x.PersonId == personId);
                        var name = absentPerson?.Name;

                        // Get various lists of relevant info
                        var allUpdatedAbsences = newAbsences.Concat(modifiedAbsences.Select(x => x.Key)).Concat(deletedAbsences);

                        // Find projects where they have subtasks affected by the absence
                        var affectedProjects = ProjectService.GetAll(context).Where(x => x.SubTasks.Any(x =>
                        {
                            foreach (var absence in allUpdatedAbsences)
                            {
                                if (x.IsAffectedByAbsence(absence, personId))
                                {
                                    return true;
                                }
                            }
                            return false;
                        }));

                        // Get affected PMs based on projects they currently own
                        var affectedPMs = affectedProjects.Select(x => x.ProjectManager).Distinct().ToList();

                        // Get all PMs based on their current WLM
                        var managersToNotify = UserService
                            .GetAll(context)
                            .Where(x => x.RoleType == RoleType.Manager || x.RoleType == RoleType.Superuser)
                            .Select(x => x.Person)
                            .DistinctBy(x => x.Name)
                            .Where(x => x.GetWorkloadModelOnDate(DateTime.Today)?.ProjectManagementFTE > 0);

                        // Check to see whether any PMs are currently absent
                        var currentPMAbsences = PersonService
                            .GetAbsencesForPeople(context, affectedPMs)
                            .Where(x => x.IsCurrentAbsence());

                        // Just need to notify the affected if there are no affected PMs who are absent at the moment
                        // Otherwise leave the managers to notfy as all PMs
                        if (currentPMAbsences.Count() == 0)
                        {
                            managersToNotify = affectedPMs;
                        }
                        // Log that we are choosing to notify all managers due to the current absence of at least one affected PM
                        else
                        {
                            Logger.LogInformation($"Following affected PMs are currently absent. Notifying all managers... {string.Join(", ", currentPMAbsences.Select(x => $"{x.Person.Name} ({x.StartDate.ToShortDateString()} to {x.EndDate?.ToShortDateString() ?? "present"})"))}");
                        }

                        // Do not notify a manager if it is their own absence
                        if (managersToNotify.Any(x => x.PersonId == personId))
                        {
                            managersToNotify = managersToNotify.Where(x => x.PersonId != personId).ToList();
                        }

                        // For each manager to notify
                        foreach (var pm in managersToNotify)
                        {
                            // Create email body
                            StringBuilder body = new StringBuilder();
                            body.Append($"<p>Dear {pm.Name},</p>");
                            body.Append($"<p>{(affectedPMs.Any(x => x.PersonId == pm.PersonId) ? Configuration["Email:AbsenceEmailBody"] : Configuration["Email:AbsenceEmailBodyNotAffected"])}</p>");

                            // Initialise a list of absences have been previously mentioned
                            var mentionedAbsences = new List<Absence>();

                            // Get affected projects owned by this person
                            var myProjects = affectedProjects.Where(x => x.ProjectManager?.PersonId == pm?.PersonId);

                            // Loop over the projects
                            foreach (var project in myProjects)
                            {
                                // Find absences related to this project
                                var relevantAbsences = new List<Absence>();
                                foreach (var absence in allUpdatedAbsences)
                                {
                                    if (project.SubTasks.Any(x => x.IsAffectedByAbsence(absence, personId)))
                                    {
                                        relevantAbsences.Add(absence);
                                    }
                                }

                                // Add to email for this project
                                if (relevantAbsences.Count > 0)
                                {
                                    body.Append($"<h4>{ProjectService.GetFullName(project)}</h4>");
                                    foreach (var ab in relevantAbsences)
                                    {
                                        // Add to the mentioned absences if not already there
                                        if (!mentionedAbsences.Contains(ab))
                                        {
                                            mentionedAbsences.Add(ab);
                                        }

                                        // Decide on the state of the absence
                                        var state = GetAbsenceState(ab, newAbsences, modifiedAbsences, deletedAbsences);

                                        // Write absence info
                                        body.Append(GetFormattedAbsenceLine(ab, state, name));

                                    }
                                }
                            }

                            // Any absences that remain in the list are therefore not related to any projects
                            var notProjectRelatedAbsences = allUpdatedAbsences.Except(mentionedAbsences);
                            if (notProjectRelatedAbsences.Count() > 0)
                            {
                                // Only add this text if there were projects mentioned higher up
                                if (mentionedAbsences.Count > 0)
                                {
                                    body.Append($"<p>{Configuration["Email:AbsenceEmailSomeAffectedEndBody"]}</p>");
                                }

                                foreach (var ab in notProjectRelatedAbsences)
                                {
                                    // Decide on the state of the absence
                                    var state = GetAbsenceState(ab, newAbsences, modifiedAbsences, deletedAbsences);

                                    // Write absence info
                                    body.Append(GetFormattedAbsenceLine(ab, state, name));
                                }
                            }

                            // Add closing statement
                            if (affectedPMs.Any(x => x.PersonId == pm.PersonId))
                            {
                                body.Append($"<p>{Configuration["Email:AbsenceEmailEndBody"]}</p>");
                            }
                            body.Append("<p><i>Sent from CapX</i></p>");

                            // Send email
                            var subject = Configuration["Email:AbsenceEmailSubject"];
                            var users = UserService.GetAll(context).Where(x => x.Person == pm);
                            var recipients = new List<string>();
                            foreach (var user in users)
                            {
                                var lineManagerEmailAddresses = user.GetNormalisedEmailAddresses();
                                if (!lineManagerEmailAddresses.Any())
                                {
                                    lineManagerEmailAddresses.Add($"{user.CASUserName}@manchester.ac.uk");
                                }
                                recipients.AddRange(lineManagerEmailAddresses);
                            }

                            Debug.WriteLine($"** Sending email to {string.Join(',', recipients.Select(AnonymiseEmail))}");
                            foreach (var recipient in recipients)
                            {
                                SendEmail(recipient, subject, body.ToString());
                                await Task.Delay(1000);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError($"Absence email failure: {e}");
                }
            });
        }

        /// <summary>
        /// Decide on the state of the absence
        /// </summary>
        /// <param name="absence"></param>
        /// <param name="newAbsences"></param>
        /// <param name="modifiedAbsences"></param>
        /// <param name="deletedAbsences"></param>
        /// <returns></returns>
        private string GetAbsenceState(Absence absence, IEnumerable<Absence> newAbsences, IEnumerable<IGrouping<Absence, EntityDiff<Absence>>> modifiedAbsences, IEnumerable<Absence> deletedAbsences)
        {
            if (deletedAbsences.Contains(absence))
            {
                return "Deleted";
            }
            else if (modifiedAbsences.Any(x => x.Key == absence))
            {
                var changes = modifiedAbsences.FirstOrDefault(x => x.Key == absence);

                // Determine if end date updated
                var change = changes.FirstOrDefault(x => x.PropertyName == "EndDate");
                if (change != null)
                {
                    return "End Date Updated";
                }
                else
                {
                    return "Modified";
                }
            }
            return "New";
        }

        /// <summary>
        /// Format the absence information suitable for the email body
        /// </summary>
        /// <param name="absence"></param>
        /// <param name="state"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private string GetFormattedAbsenceLine(Absence absence, string state, string name = null)
        {
            return $"<p>{name ?? absence.Person.Name} is absent from {absence.StartDate.ToShortDateString()} to {absence.EndDate?.ToShortDateString() ?? "present"} (<b>{state}</b>).</p>";
        }

        /// <summary>
        /// Send an email to the people mentioned in a note and the project owner
        /// </summary>
        /// <param name="note"></param>
        /// <param name="mentions"></param>
        /// <param name="listOfChanges"></param>
        internal async Task SendMentionAndOwnerEmailNotificationsAsync(Note note, IList<Person> mentions, IList<EntityDiff<Note>> listOfChanges = null)
        {
            await Task.Run(async () =>
            {
                try
                {
                    // Create context and get roles (ignoring externals)
                    using (var context = DbContextFactory.CreateDbContext())
                    {
                        var users = UserService.GetAll(context).Where(x => x.Person != null).DistinctBy(x => x.Person.PersonId);

                        // Start with those mentioned in the note
                        var peopleToBeNotfied = mentions;

                        // Add the PM
                        if (!peopleToBeNotfied.Contains(note.Project.ProjectManager))
                        {
                            peopleToBeNotfied.Add(note.Project.ProjectManager);
                        }

                        // Add those who are following
                        foreach (var p in note.Project.Followers)
                        {
                            if (!peopleToBeNotfied.Contains(p))
                            {
                                peopleToBeNotfied.Add(p);
                            }
                        }

                        // Remove the author or the editor
                        if (note.Editor != null)
                        {
                            peopleToBeNotfied.Remove(note.Editor.Person);
                        }
                        else
                        {
                            peopleToBeNotfied.Remove(note.Author.Person);
                        }

                        // Create the emails and send
                        foreach (var m in peopleToBeNotfied)
                        {
                            // Create email body
                            StringBuilder body = new StringBuilder();

                            // Inject the CSS for styling
                            body.Append($"{Configuration["Email:EmailBadgeStyling"]}");

                            // Write intro
                            body.Append($"<p>Dear {m.Name},</p>");
                            var content = listOfChanges != null ? Configuration["Email:MentionEmailBodyUpdate"] : Configuration["Email:MentionEmailBodyNew"];
                            body.Append($"<p>{content}</p>");
                            body.Append("<hr />");

                            // Include author info as bold
                            body.Append($"<b>{note.GetNoteAuthorText()}</b>{(note.IsFinanceInfo ? " [Finance Info]" : "")} {(note.DueDate != null ? $"Due Date: {note.DueDate?.ToShortDateString()}" : "")} {(note.CompletedDate != null ? $"Completed: {note.CompletedDate?.ToShortDateString()}" : "")}");

                            // Include the full message from the note
                            body.Append($"<p>{note.HtmlContent}</p>");

                            // Include editor info as italics
                            body.Append($"<br /><i>{note.GetNoteEditorText()}</i>");
                            body.Append("<hr />");

                            // State changes
                            if (listOfChanges != null)
                            {
                                body.Append("<p><b>Changes</b></p>");

                                // Write each change one and a time
                                foreach (var diff in listOfChanges
                                    .Where(x => x.PropertyName != "EditorPersonId" && x.PropertyName != nameof(Note.EditedDate))
                                )
                                {
                                    body.Append($"<p><b><i>{diff.PropertyName}:</i></b></p> <p>{diff.OriginalValue ?? "None"}<br/><b>&hArr;</b> {diff.CurrentValue ?? "None"}</p>");
                                }
                                body.Append("<hr />");
                            }

                            // Add footer
                            body.Append($"<p>{Configuration["Email:MentionEmailEndBody"]}</p><p><i>Sent from CapX</i></p>");
                            body.Append($"<br /><a href=\"{Configuration["Authentication:HostUrl"]}/projects/projectdetails?rtp={note.Project.RTP}&filteredNote={note.NoteId}\">View this note on CapX</a>");

                            // Send email
                            var subject = $"{Configuration["Email:MentionEmailSubject"]} - {ProjectService.GetFullName(note.Project)}";
                            var pmUsers = users.Where(x => x.Person.PersonId == m.PersonId);
                            var recipients = new List<string>();
                            foreach (var u in pmUsers)
                            {
                                var lineManagerEmailAddresses = u.GetNormalisedEmailAddresses();
                                if (!lineManagerEmailAddresses.Any())
                                {
                                    lineManagerEmailAddresses.Add($"{u.CASUserName}@manchester.ac.uk");
                                }
                                recipients.AddRange(lineManagerEmailAddresses);
                            }
                            Debug.WriteLine($"** Sending email to {string.Join(',', recipients.Select(AnonymiseEmail))}");
                            foreach (var recipient in recipients)
                            {
                                SendEmail(recipient, subject, body.ToString());
                                await Task.Delay(1000);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError($"Mention email failure: {e}");
                }
            });
        }

        /// <summary>
        /// Anonymise an email address for logging purposes
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        private static string AnonymiseEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return email;
            }

            // Find the at
            var atIndex = email.IndexOf('@');
            if (atIndex <= 0 || atIndex == email.Length - 1)
            {
                return new string('*', email.Length);
            }

            // Extract the parts
            var localPart = email[..atIndex];
            var domainPart = email[(atIndex + 1)..];

            // Mask the local part, leaving only the first 3 characters visible
            var visibleCount = Math.Min(3, localPart.Length);
            var maskedLocalPart = localPart[..visibleCount] + new string('*', Math.Max(0, localPart.Length - visibleCount));

            return $"{maskedLocalPart}@{domainPart}";
        }
    }
}
