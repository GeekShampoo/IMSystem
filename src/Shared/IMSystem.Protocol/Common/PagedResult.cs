using System;
using System.Collections.Generic;

namespace IMSystem.Protocol.Common;

/// <summary>
/// Represents a paged list of items.
/// </summary>
/// <typeparam name="T">The type of the items in the list.</typeparam>
public class PagedResult<T> : Result // Inherit from Result to get IsSuccess and Error properties
{
    /// <summary>
    /// Gets the items for the current page.
    /// </summary>
    public List<T> Items { get; }

    /// <summary>
    /// Gets the current page number (1-based).
    /// </summary>
    public int PageNumber { get; }

    /// <summary>
    /// Gets the number of items per page.
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Gets the total number of items across all pages.
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PagedResult{T}"/> class for a successful result.
    /// </summary>
    protected PagedResult(List<T> items, int pageNumber, int pageSize, int totalCount, bool isSuccess, Error? error)
        : base(isSuccess, error)
    {
        Items = items ?? new List<T>();
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        if (TotalPages == 0 && totalCount > 0) TotalPages = 1;
        if (TotalPages == 0 && totalCount == 0 && isSuccess) TotalPages = 0; // Allow 0 pages for empty successful result
        else if (TotalPages == 0 && totalCount == 0 && !isSuccess) TotalPages = 0; // Or handle error case for pages
    }
    
    /// <summary>
    /// Creates a successful paged result.
    /// </summary>
    public static PagedResult<T> Success(List<T> items, int totalCount, int pageNumber, int pageSize)
    {
        return new PagedResult<T>(items, pageNumber, pageSize, totalCount, true, null);
    }

    /// <summary>
    /// Creates a failure paged result.
    /// </summary>
    public static new PagedResult<T> Failure(Error error) // 'new' keyword to hide base class method if necessary
    {
        return new PagedResult<T>(new List<T>(), 0, 0, 0, false, error);
    }

    /// <summary>
    /// Creates a failure paged result with error code and message.
    /// </summary>
    public static new PagedResult<T> Failure(string errorCode, string errorMessage) // 'new' keyword
    {
        return new PagedResult<T>(new List<T>(), 0, 0, 0, false, new Error(errorCode, errorMessage));
    }


    /// <summary>
    /// Creates an empty paged result, typically representing a successful empty state.
    /// </summary>
    /// <param name="pageNumber">The current page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>An empty PagedResult.</returns>
    public static PagedResult<T> Empty(int pageNumber, int pageSize)
    {
        return new PagedResult<T>(new List<T>(), pageNumber, pageSize, 0, true, null);
    }
}