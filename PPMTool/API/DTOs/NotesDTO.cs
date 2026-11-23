// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

#nullable enable

namespace PPMTool.API.DTOs
{
    /// <summary>
    /// One Note (project comment), for API read access via
    /// GET /api/projects/notes/getAll.
    /// </summary>
    /// <param name="NoteId">Identifies this Note for PUT /api/projects/notes/update</param>
    /// <param name="CapXProjectId">CapX project id of the Project this Note belongs to</param>
    /// <param name="AuthorUsername">Access Control username of the Note's author</param>
    /// <param name="AuthorDisplayName">The author's display name (<see cref="PPMTool.Data.Entities.Person.Name"/> if linked, else the bare <see cref="PPMTool.Data.Entities.User.Name"/>)</param>
    /// <param name="HtmlContent"></param>
    /// <param name="CreatedDate"></param>
    /// <param name="EditedDate"></param>
    public sealed record NoteDTO(
        int NoteId,
        int CapXProjectId,
        string AuthorUsername,
        string AuthorDisplayName,
        string HtmlContent,
        DateTime CreatedDate,
        DateTime EditedDate
    );

    /// <summary>
    ///One comment to add as a Note on a Project.
    /// Kept separate from <see cref="NoteDTO"/>: a request carries only what the caller
    /// supplies, and the read record adds the ids and fields CapX derives or links.
    /// </summary>
    /// <param name="AuthorUsername">Username of the comment's author, if known. When it's missing or matches no User, the Note is attributed to the "migration-import" User, which must exist, and AuthorDisplayName is kept in its text.</param>
    /// <param name="AuthorDisplayName">The author's name as the source recorded it</param>
    /// <param name="ContentHtml">The comment, as HTML; sanitised on save</param>
    /// <param name="CreatedDate">When the comment was made</param>
    public sealed record WriteNoteDTO(
        string? AuthorUsername,
        string AuthorDisplayName,
        string ContentHtml,
        DateTime CreatedDate
    );

    /// <summary>
    /// Request body for POST /api/projects/notes/add. Adds comments as Notes to
    /// an existing Project. Notes aren't deduplicated, so sending the same
    /// comments twice adds them twice: check GET /api/projects/notes/getAll first.
    /// </summary>
    /// <param name="CapXProjectId">CapX project id of the Project</param>
    /// <param name="Comments">At least one</param>
    public sealed record WriteNotesDTO(
        int CapXProjectId,
        IReadOnlyList<WriteNoteDTO> Comments
    );

    /// <summary>Response for added Notes.</summary>
    /// <param name="ProjectId"></param>
    /// <param name="NotesCreated"></param>
    public sealed record WriteNotesResponseDTO(
        int ProjectId,
        int NotesCreated
    );

    /// <summary>
    /// Request body for PUT /api/projects/notes/update. Replaces a Note's
    /// content, identified by NoteId. The author is unchanged.
    /// </summary>
    /// <param name="NoteId">From GET /api/projects/notes/getAll</param>
    /// <param name="HtmlContent">The whole new content, as HTML; sanitised on save</param>
    public sealed record UpdateNoteRequestDTO(
        int NoteId,
        string HtmlContent
    );

    /// <summary>Response for an updated Note.</summary>
    /// <param name="NoteId"></param>
    public sealed record UpdateNoteResponseDTO(
        int NoteId
    );
}
