// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.API.DTOs
{
    /// <summary>
    /// One Note (project comment), for API read access via
    /// GET /api/projects/notes/getAll.
    /// </summary>
    /// <param name="NoteId">Identifies this Note for PUT /api/projects/notes/update</param>
    /// <param name="RTP">RTP of the Project this Note belongs to</param>
    /// <param name="AuthorUsername">Access Control username of the Note's author</param>
    /// <param name="AuthorDisplayName">The author's display name (Person.Name if linked, else the bare User.Name)</param>
    /// <param name="HtmlContent"></param>
    /// <param name="CreatedDate"></param>
    /// <param name="EditedDate"></param>
    public sealed record NoteDTO(
        int NoteId,
        int RTP,
        string AuthorUsername,
        string AuthorDisplayName,
        string HtmlContent,
        DateTime CreatedDate,
        DateTime EditedDate
    );
}
