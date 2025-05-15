using IMSystem.Protocol.Common;
using IMSystem.Protocol.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace IMSystem.Server.Web;

/// <summary>
/// Defines common API conventions for the IMSystem application.
/// These conventions help reduce repetitive [ProducesResponseType] attributes on controller actions.
/// </summary>
public static class DefaultApiConventions
{
    // === Conventions for GET operations ===

    /// <summary>
    /// Convention for GET operations that retrieve a single resource by ID.
    /// Matches methods like: GetById(Guid id), GetUserById(Guid id).
    /// Expected success: 200 OK with the resource.
    /// Expected errors: 400, 401, 403, 404, 500.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
    public static void GetById(
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Suffix)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object id)
    { }

    /// <summary>
    /// Convention for GET operations that retrieve a list or collection of resources.
    /// Matches methods like: Get(), List(), GetUsers(), GetGroups().
    /// Expected success: 200 OK with the list/collection.
    /// Expected errors: 400, 401, 500. (404 is typically handled by returning an empty list with 200 OK)
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
    public static void GetList() // Covers Get(), List(), Search() variants
    { }
    
    /// <summary>
    /// Convention for GET operations that retrieve a paged list of resources.
    /// Matches methods like: GetPaged(int pageNumber, int pageSize), GetUserMessages(Guid id, int pageNumber, int pageSize).
    /// Expected success: 200 OK with the paged result.
    /// Expected errors: 400, 401, 403 (if applicable for resource access), 404 (if primary resource for paging not found), 500.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK)] // Typically PagedResult<T>
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)] // More flexible matching for paged results
    public static void GetPaged(
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object idOrQuery, // Can be an ID for a parent resource or a query object
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object pageNumber,
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object pageSize)
    { }


    // === Conventions for POST operations ===

    /// <summary>
    /// Convention for POST operations that create a new resource.
    /// Matches methods like: Create(Model model), Post(Model model), Add(Model model).
    /// Expected success: 201 Created with location header and/or created resource/ID.
    /// Expected errors: 400, 401, 403 (if applicable), 409, 500.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
    public static void Post(
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object model)
    { }
    
    /// <summary>
    /// Convention for POST operations that perform an action and may return data or just success/failure.
    /// Matches methods like: Send(), Action(), Process().
    /// Expected success: 200 OK (if returning data/Result) or 204 No Content (if no data returned on success).
    /// Expected errors: 400, 401, 403, 404, 409, 500.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK)] // For actions returning Result or data
    [ProducesResponseType(StatusCodes.Status204NoContent)] // For actions returning no content on success
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)] // More flexible matching
    public static void DoAction( // Generic name for actions like "Send", "Invite", "Accept", "Reject", "Block", "Recall"
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object payload) // Can be an ID, a request DTO, etc.
    { }


    // === Conventions for PUT operations ===

    /// <summary>
    /// Convention for PUT operations that update an existing resource.
    /// Matches methods like: Update(Guid id, Model model), Put(Guid id, Model model).
    /// Expected success: 204 No Content or 200 OK (if returning updated resource).
    /// Expected errors: 400, 401, 403, 404, 409, 500.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status200OK)] // If the update returns the updated entity
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
    public static void Put(
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Suffix)] // Matches 'id' as a common suffix for the ID parameter
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object id,
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object model)
    { }

    // === Conventions for DELETE operations ===

    /// <summary>
    /// Convention for DELETE operations.
    /// Matches methods like: Delete(Guid id), Remove(Guid id).
    /// Expected success: 204 No Content or 200 OK (if returning some confirmation).
    /// Expected errors: 400, 401, 403, 404, 500.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status200OK)] // If delete returns a confirmation object
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
    public static void Delete(
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Suffix)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object id)
    { }
}