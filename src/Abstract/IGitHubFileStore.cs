using System.Threading;
using System.Threading.Tasks;
using Soenneker.GitHub.OpenApiClient.Models;

namespace Soenneker.GitHub.FileStore.Abstract;

/// <summary>
/// A utility interface that provides file system operations for GitHub repositories using the OpenAPI client.
/// </summary>
public interface IGitHubFileStore
{
    /// <summary>
    /// Gets the metadata of a file or directory.
    /// </summary>
    /// <param name="owner">The repository owner.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="path">The path to the file or directory.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The content file metadata.</returns>
    ValueTask<ContentFile> Get(string owner, string repo, string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the contents of a file.
    /// </summary>
    /// <param name="owner">The repository owner.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="path">The path to the file.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The file contents as a string.</returns>
    ValueTask<string> Read(string owner, string repo, string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the contents of a file.
    /// </summary>
    /// <param name="owner">The repository owner.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="path">The path to the file.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The file contents as a string.</returns>
    ValueTask<byte[]> ReadAsBytes(string owner, string repo, string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new file.
    /// </summary>
    /// <param name="owner">The repository owner.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="path">The path where to create the file.</param>
    /// <param name="content">The file content.</param>
    /// <param name="message">Optional commit message. If not provided, an automated message will be used.</param>
    /// <param name="branch">Optional branch name. Defaults to "main".</param>
    /// <param name="authorEmail"></param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <param name="authorName"></param>
    /// <returns>The file commit information.</returns>
    ValueTask<FileCommit?> Write(string owner, string repo, string path, string content, string? message = null, string branch = "main",
        string? authorName = null, string? authorEmail = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new file.
    /// </summary>
    /// <param name="owner">The repository owner.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="path">The path where to create the file.</param>
    /// <param name="content">The file content.</param>
    /// <param name="message">Optional commit message. If not provided, an automated message will be used.</param>
    /// <param name="branch">Optional branch name. Defaults to "main".</param>
    /// <param name="authorEmail"></param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <param name="authorName"></param>
    /// <returns>The file commit information.</returns>
    ValueTask<FileCommit?> WriteBytes(string owner, string repo, string path, byte[] content, string? message = null, string branch = "main",
        string? authorName = null, string? authorEmail = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file.
    /// </summary>
    /// <param name="owner">The repository owner.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="path">The path to the file to delete.</param>
    /// <param name="message">Optional commit message. If not provided, an automated message will be used.</param>
    /// <param name="branch">Optional branch name. Defaults to "main".</param>
    /// <param name="authorEmail"></param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <param name="authorName"></param>
    /// <returns>The file commit information.</returns>
    ValueTask<FileCommit?> Delete(string owner, string repo, string path, string? message = null, string branch = "main", string? authorName = null,
        string? authorEmail = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the contents of a directory.
    /// </summary>
    /// <param name="owner">The repository owner.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="path">The path to the directory.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>An array of content files representing the directory contents.</returns>
    ValueTask<ContentFile[]> List(string owner, string repo, string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file or directory exists.
    /// </summary>
    /// <param name="owner">The repository owner.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="path">The path to check.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>True if the file or directory exists, false otherwise.</returns>
    ValueTask<bool> Exists(string owner, string repo, string path, CancellationToken cancellationToken = default);
}