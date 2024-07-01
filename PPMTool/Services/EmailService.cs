using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

namespace PPMTool.Services
{
    public class EmailService
    {
        public EmailService(IConfiguration configuration, ProjectService projectService, RolesService rolesService, ILogger logger)
        {
            Configuration = configuration;
            ProjectService = projectService;
            RolesService = rolesService;
            Logger = logger;
        }

        public IConfiguration Configuration { get; }
        public ProjectService ProjectService { get; }
        public RolesService RolesService { get; }
        public ILogger Logger { get; }

        public void SendEmail(IEnumerable<string> to, string subject, string message)
        {
            var client = new SmtpClient(Configuration["Email:SmtpServer"]);

            var mailMessage = new MailMessage
            {
                From = new MailAddress(Configuration["Email:From"]),
                Subject = subject,
                Body = message,
                IsBodyHtml = true,
            };

#if !LOCAL
            foreach (var recipient in to)
            {
                mailMessage.To.Add(recipient);
            }
#else
            mailMessage.To.Add("mbgm6ah3@manchester.ac.uk");
#endif

            try
            {
                client.Send(mailMessage);
                Logger.LogInformation($"Sent email to {string.Join(',', mailMessage.To)}, subject {mailMessage.Subject}");
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to send email to {string.Join(',', mailMessage.To)}, subject {mailMessage.Subject}:\n{e}");
            }
        }

        public void SendAbsenceEmailNotifications(PPMToolContext context, IEnumerable<Absence> newAbsences, Dictionary<Absence, IList<EntityDiff>> modifiedAbsences, IList<Absence> deletedAbsences)
        {
            Task.Run(() =>
            {
                // Get various lists of relevant info
                var allAbsences = newAbsences.Concat(modifiedAbsences.Select(x => x.Key)).Concat(deletedAbsences);
                var absentPeople = allAbsences.Select(x => x.Person).Distinct();

                // Find projects where they have subtasks affected by the absence
                var affectedProjects = ProjectService.GetAll(context).Where(x => x.SubTasks.Any(x =>
                {
                    foreach (var absence in allAbsences)
                    {
                        if (x.IsAffectedByAbsence(absence))
                        {
                            return true;
                        }
                    }
                    return false;
                }));

                // Get affected PMs
                var affectedPMs = affectedProjects.Select(x => x.ProjectManager).Distinct();
                var allPMs = RolesService.GetAll(context).Where(x => x.RoleType == RoleType.Manager || x.RoleType == RoleType.Superuser).Select(x => x.Person).Distinct();

                // For each manager
                foreach (var pm in allPMs)
                {
                    // Create email body
                    StringBuilder body = new StringBuilder();
                    body.Append($"<p>Dear {pm.Name},</p>");
                    body.Append($"<p>{(affectedPMs.Contains(pm) ? Configuration["Email:AbsenceEmailBody"] : Configuration["Email:AbsenceEmailBodyNotAffected"])}</p>");

                    // Get people absent from projects owned by this PM
                    var myProjects = affectedProjects.Where(x => x.ProjectManager == pm);
                    foreach (var project in myProjects)
                    {
                        var relevantAbsences = new List<Absence>();
                        foreach (var absence in allAbsences)
                        {
                            if (project.SubTasks.Any(x => x.IsAffectedByAbsence(absence)))
                            {
                                relevantAbsences.Add(absence);
                            }
                        }

                        // Add to email
                        if (relevantAbsences.Count > 0)
                        {
                            body.Append($"<h4>{project.GetFullName()}</h4>");
                            foreach (var ab in relevantAbsences)
                            {
                                // Decide on the state of the absence
                                var state = "New";
                                if (deletedAbsences.Contains(ab))
                                {
                                    state = "Deleted";
                                }
                                else if (modifiedAbsences.ContainsKey(ab))
                                {
                                    var changes = modifiedAbsences[ab];

                                    // Determine if return to work or some other modification
                                    var change = changes.FirstOrDefault(x => x.PropertyName == "EndDate");
                                    if (change != null && change.OriginalValue == null)
                                    {
                                        state = "Returned to Work";
                                    }
                                    else
                                    {
                                        state = "Modified";
                                    }
                                }

                                // Write absence info
                                body.Append($"<p>{ab.Person.Name} is absent from {ab.StartDate.ToShortDateString()} to {ab.EndDate?.ToShortDateString() ?? "present"} (<b>{state}</b>).</p>");
                            }
                        }
                    }

                    // Send email
                    body.Append($"<p>{(affectedPMs.Contains(pm) ? Configuration["Email:AbsenceEmailEndBody"] : Configuration["Email:AbsenceEmailEndBodyNotAffected"])}</p><p><i>Sent from CapX</i></p>");
                    var subject = Configuration["Email:AbsenceEmailSubject"];
                    var role = RolesService.GetAll(context).Where(x => x.Person == pm);
                    IEnumerable<string> recipients = role
                        .Select(x => string.IsNullOrWhiteSpace(x.EmailAddress) ?
                            $"{x.CASUserName}@manchester.ac.uk" : x.EmailAddress);
                    SendEmail(recipients, subject, body.ToString());
                }
            });
        }

        internal void SendMentionAndOwnerEmailNotifications(PPMToolContext context, Note note, IList<Person> mentions, bool isUpdate)
        {
            Task.Run(() =>
            {
                var roles = RolesService.GetAll(context);

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
                    var content = isUpdate ? Configuration["Email:MentionEmailBodyUpdate"] : Configuration["Email:MentionEmailBodyNew"];
                    body.Append($"<p>{content}</p>");
                    body.Append("<hr />");

                    // Include author info as bold
                    body.Append($"<b>{note.GetNoteAuthorText()}</b>{(note.IsFinanceInfo ? " [Finance Info]" : "")} {(note.DueDate != null ? $"Due Date: {note.DueDate?.ToShortDateString()}" : "")} {(note.CompletedDate != null ? $"Completed: {note.CompletedDate?.ToShortDateString()}" : "")}");

                    // Include the full message from the note
                    body.Append($"<p>{note.HtmlContent}</p>");

                    // Include editor info as italics
                    body.Append($"<br /><i>{note.GetNoteEditorText()}</i>");
                    body.Append("<hr />");

                    // Add footer
                    body.Append($"<p>{Configuration["Email:MentionEmailEndBody"]}</p><p><i>Sent from CapX</i></p>");
                    body.Append($"<br /><a href=\"{Configuration["Authentication:HostUrl"]}/projectdetails?rtp={note.Project.RTP}&filteredNote={note.NoteId}\">View {note.Project.GetFullName()} on CapX</a>");

                    // Send email
                    var subject = $"{Configuration["Email:MentionEmailSubject"]} - {note.Project.GetFullName()}";
                    var role = RolesService.GetAll(context).Where(x => x.Person == m);
                    IEnumerable<string> recipients = role
                        .Select(x => string.IsNullOrWhiteSpace(x.EmailAddress) ?
                            $"{x.CASUserName}@manchester.ac.uk" : x.EmailAddress);
                    SendEmail(recipients, subject, body.ToString());
                }
            });
        }
    }
}
