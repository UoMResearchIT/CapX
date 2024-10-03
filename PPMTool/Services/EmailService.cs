using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
#if !LOCAL
using System.Net.Mail;
#endif
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Enums;

namespace PPMTool.Services
{
    public class EmailService
    {
        public EmailService(
            IConfiguration configuration,
            ProjectService projectService,
            RolesService rolesService,
            PersonService personService,
            IDbContextFactory<PPMToolContext> dbContextFactory,
            ILogger logger
        )
        {
            Configuration = configuration;
            ProjectService = projectService;
            RolesService = rolesService;
            PersonService = personService;
            DbContextFactory = dbContextFactory;
            Logger = logger;
        }

        public IConfiguration Configuration { get; }
        public ProjectService ProjectService { get; }
        public RolesService RolesService { get; }
        public PersonService PersonService { get; }

        public IDbContextFactory<PPMToolContext> DbContextFactory { get; }
        public ILogger Logger { get; }

        public void SendEmail(IEnumerable<string> to, string subject, string message)
        {
#if !LOCAL
            var client = new SmtpClient(Configuration["Email:SmtpServer"]);

            var mailMessage = new MailMessage
            {
                From = new MailAddress(Configuration["Email:From"]),
                Subject = subject,
                Body = message,
                IsBodyHtml = true,
            };

            foreach (var recipient in to)
            {
                mailMessage.To.Add(recipient);
            }

            try
            {
                client.Send(mailMessage);
                Logger.LogInformation($"Sent email to {string.Join(',', mailMessage.To)}, subject {mailMessage.Subject}");
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to send email to {string.Join(',', mailMessage.To)}, subject {mailMessage.Subject}:\n{e}");
            }
#endif
        }

        public void SendAbsenceEmailNotifications(IEnumerable<Absence> newAbsences, IEnumerable<IGrouping<Absence, EntityDiff<Absence>>> modifiedAbsences, Dictionary<int, Absence> deletedAbsences)
        {
            Task.Run(() =>
            {
                // Create context and get people for lookup
                var context = DbContextFactory.CreateDbContext();
                var people = RolesService.GetAll(context).Select(x => x.Person).DistinctBy(x => x.Name);

                // Get various lists of relevant info
                var allUpdatedAbsences = newAbsences.Concat(modifiedAbsences.Select(x => x.Key)).Concat(deletedAbsences.Values);
                var updatedAbsentPeople = allUpdatedAbsences.Select(x => x.Person).Distinct();

                // Find projects where they have subtasks affected by the absence
                var affectedProjects = ProjectService.GetAll(context).Where(x => x.SubTasks.Any(x =>
                {
                    foreach (var absence in allUpdatedAbsences)
                    {
                        // If a deletion, need to provide a person ID
                        var kvp = deletedAbsences.FirstOrDefault(x => x.Value == absence);
                        int? id = kvp.Key == 0 ? null : kvp.Key;
                        if (x.IsAffectedByAbsence(absence, id))
                        {
                            return true;
                        }
                    }
                    return false;
                }));

                // Get affected PMs
                var affectedPMs = affectedProjects.Select(x => x.ProjectManager).Distinct().ToList();

                // If any affected PM is currently absent then notify all PMs
                var managersToNotify = RolesService.GetAll(context).Where(x => x.RoleType == RoleType.Manager || x.RoleType == RoleType.Superuser).Select(x => x.Person).DistinctBy(x => x.Name);
                var currentPMAbsences = PersonService.GetAbsencesForPeople(context, affectedPMs).Where(x => x.IsCurrentAbsence());

                // Just need to notify the affected if there are no affected PMs who are absent at the moment
                if (currentPMAbsences.Count() == 0)
                {
                    managersToNotify = affectedPMs;
                }

                // Ensure superusers are in the list in any case
                var superusers = RolesService.GetAll(context).Where(x => x.RoleType == RoleType.Superuser).Select(x => x.Person).DistinctBy(x => x.Name);
                foreach (var su in superusers)
                {
                    if (!affectedPMs.Contains(su))
                    {
                        affectedPMs.Add(su);
                    }
                }

                // For each manager to notify
                foreach (var pm in managersToNotify)
                {
                    // Create email body
                    StringBuilder body = new StringBuilder();
                    body.Append($"<p>Dear {pm.Name},</p>");
                    body.Append($"<p>{(affectedPMs.Contains(pm) ? Configuration["Email:AbsenceEmailBody"] : Configuration["Email:AbsenceEmailBodyNotAffected"])}</p>");

                    // Initialise a list of absences have been previously mentioned
                    var mentionedAbsences = new List<Absence>();

                    // Get affected projects owned by this person
                    var myProjects = affectedProjects.Where(x => x.ProjectManager == pm);

                    // Loop over the projects
                    foreach (var project in myProjects)
                    {
                        // Find absences related to this project
                        var relevantAbsences = new List<Absence>();
                        foreach (var absence in allUpdatedAbsences)
                        {
                            var kvp = deletedAbsences.FirstOrDefault(x => x.Value == absence);
                            int? id = kvp.Key == 0 ? null : kvp.Key;
                            if (project.SubTasks.Any(x => x.IsAffectedByAbsence(absence, id)))
                            {
                                relevantAbsences.Add(absence);
                            }
                        }

                        // Add to email for this project
                        if (relevantAbsences.Count > 0)
                        {
                            body.Append($"<h4>{project.GetFullName()}</h4>");
                            foreach (var ab in relevantAbsences)
                            {
                                // Add to the mentioned absences if not already there
                                if (!mentionedAbsences.Contains(ab))
                                {
                                    mentionedAbsences.Add(ab);
                                }

                                // Decide on the state of the absence
                                var state = GetAbsenceState(ab, newAbsences, modifiedAbsences, deletedAbsences.Select(x => x.Value));

                                // If absence is deletion need to pass name
                                string name = null;
                                if (deletedAbsences.ContainsValue(ab))
                                {
                                    var id = deletedAbsences.FirstOrDefault(x => x.Value == ab).Key;
                                    name = people.FirstOrDefault(x => x.PersonId == id)?.Name;
                                }

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
                            var state = GetAbsenceState(ab, newAbsences, modifiedAbsences, deletedAbsences.Select(x => x.Value));

                            // If absence is deletion need to pass name
                            string name = null;
                            if (deletedAbsences.ContainsValue(ab))
                            {
                                var id = deletedAbsences.FirstOrDefault(x => x.Value == ab).Key;
                                name = people.FirstOrDefault(x => x.PersonId == id)?.Name;
                            }

                            // Write absence info
                            body.Append(GetFormattedAbsenceLine(ab, state, name));
                        }
                    }

                    // Add closing statement
                    if (affectedPMs.Contains(pm))
                    {
                        body.Append($"<p>{Configuration["Email:AbsenceEmailEndBody"]}</p>");
                    }
                    body.Append("<p><i>Sent from CapX</i></p>");

                    // Send email
                    var subject = Configuration["Email:AbsenceEmailSubject"];
                    var role = RolesService.GetAll(context).Where(x => x.Person == pm);
                    IEnumerable<string> recipients = role
                        .Select(x => string.IsNullOrWhiteSpace(x.EmailAddress) ?
                            $"{x.CASUserName}@manchester.ac.uk" : x.EmailAddress);
                    Debug.WriteLine($"** Sending email to {string.Join(',', recipients)}");
                    SendEmail(recipients, subject, body.ToString());
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

        private string GetFormattedAbsenceLine(Absence absence, string state, string name = null)
        {
            return $"<p>{name ?? absence.Person.Name} is absent from {absence.StartDate.ToShortDateString()} to {absence.EndDate?.ToShortDateString() ?? "present"} (<b>{state}</b>).</p>";
        }

        internal void SendMentionAndOwnerEmailNotifications(Note note, IList<Person> mentions, IList<EntityDiff<Note>> listOfChanges = null)
        {
            Task.Run(() =>
            {
                // Create context and get roles
                var context = DbContextFactory.CreateDbContext();
                var roles = RolesService.GetAll(context).DistinctBy(x => x.Person.PersonId);

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
                    peopleToBeNotfied.Remove(note.Editor);
                }
                else
                {
                    peopleToBeNotfied.Remove(note.Author);
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
                    body.Append($"<br /><a href=\"{Configuration["Authentication:HostUrl"]}/projectdetails?rtp={note.Project.RTP}&filteredNote={note.NoteId}\">View this note on CapX</a>");

                    // Send email
                    var subject = $"{Configuration["Email:MentionEmailSubject"]} - {note.Project.GetFullName()}";
                    var role = roles.Where(x => x.Person.PersonId == m.PersonId);
                    IEnumerable<string> recipients = role
                        .Select(x => string.IsNullOrWhiteSpace(x.EmailAddress) ?
                            $"{x.CASUserName}@manchester.ac.uk" : x.EmailAddress);
                    SendEmail(recipients, subject, body.ToString());
                }
            });
        }
    }
}
