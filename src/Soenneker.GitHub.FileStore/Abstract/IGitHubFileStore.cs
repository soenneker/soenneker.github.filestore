using Soenneker.GitHub.OpenApiClient.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
    ValueTask<byte[]> ReadToBytes(string owner, string repo, string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the contents of a GitHub repository file and writes it to a local file.
    /// </summary>
    ValueTask ReadToFile(string owner, string repo, string path, string filePath, CancellationToken cancellationToken = default);

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
    /// Writes the contents of a local file to a GitHub repository path.
    /// </summary>
    ValueTask<FileCommit?> WriteFromFile(string owner, string repo, string path, string filePath, string? message = null, string branch = "main",
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

    /// <summary>
    /// Writes the contents of a file to a GitHub repository path, overwriting it if it already exists (SHA-safe).
    /// </summary>
    /// <param name="owner">The repository owner.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="path">The destination path in the repository.</param>
    /// <param name="content">The file content in bytes.</param>
    /// <param name="message">Optional commit message. If not provided, an automated message will be used.</param>
    /// <param name="branch">Optional branch name. Defaults to "main".</param>
    /// <param name="authorName">Optional author name for the commit.</param>
    /// <param name="authorEmail">Optional author email for the commit.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The file commit information, or null if the write was skipped or failed.</returns>
    ValueTask<FileCommit?> WriteBytes(string owner, string repo, string path, byte[] content, string? message = null, string branch = "main",
        string? authorName = null, string? authorEmail = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recursively uploads a local directory to a GitHub repository path, preserving relative file structure. Overwrites existing files.
    /// </summary>
    /// <param name="owner">The repository owner.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="rootPath">The target base path in the repository where files will be written.</param>
    /// <param name="localDirPath">The local directory to upload.</param>
    /// <param name="message">Optional commit message. If not provided, an automated message will be used.</param>
    /// <param name="branch">Optional branch name. Defaults to "main".</param>
    /// <param name="authorName">Optional commit author name.</param>
    /// <param name="authorEmail">Optional commit author email.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A list of file commit results for each file written.</returns>
    ValueTask<IReadOnlyList<FileCommit>> WriteDirectory(string owner, string repo, string rootPath, string localDirPath, string? message = null,
        string branch = "main", string? authorName = null, string? authorEmail = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies a file within the repository by reading from <paramref name="sourcePath"/> and writing to <paramref name="destPath"/>.
    /// </summary>
    /// <param name="owner">The repository owner.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="sourcePath">The path of the file to copy.</param>
    /// <param name="destPath">The destination path for the copied file.</param>
    /// <param name="message">Optional commit message. If not provided, an automated message will be used.</param>
    /// <param name="branch">Optional branch name. Defaults to "main".</param>
    /// <param name="authorName">Optional author name for the commit.</param>
    /// <param name="authorEmail">Optional author email for the commit.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The file commit information for the new file, or null if skipped or failed.</returns>
    ValueTask<FileCommit?> Copy(string owner, string repo, string sourcePath, string destPath, string? message = null, string branch = "main",
        string? authorName = null, string? authorEmail = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a file within the repository by copying from <paramref name="sourcePath"/> to <paramref name="destPath"/> and then deleting the original.
    /// </summary>
    /// <param name="owner">The repository owner.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="sourcePath">The path of the file to move.</param>
    /// <param name="destPath">The destination path for the moved file.</param>
    /// <param name="message">Optional commit message. If not provided, an automated message will be used.</param>
    /// <param name="branch">Optional branch name. Defaults to "main".</param>
    /// <param name="authorName">Optional author name for the commit.</param>
    /// <param name="authorEmail">Optional author email for the commit.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The file commit information for the new file, or null if skipped or failed.</returns>
    ValueTask<FileCommit?> Move(string owner, string repo, string sourcePath, string destPath, string? message = null, string branch = "main",
        string? authorName = null, string? authorEmail = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves only the metadata (without content) of a file or directory.
    /// </summary>
    /// <param name="owner">The repository owner.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="path">The path to the file or directory.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The content file metadata.</returns>
    ValueTask<ContentFile> GetMetadata(string owner, string repo, string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates the raw.githubusercontent.com download URL for a given file path.
    /// </summary>
    /// <param name="owner">The repository owner.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="path">The path to the file within the repository.</param>
    /// <param name="branch">The branch name (defaults to "main").</param>
    /// <returns>The raw download URL.</returns>
    string GetRawDownloadUrl(string owner, string repo, string path, string branch = "main");

    /// <summary>
    /// Deletes a directory and all its contents from the repository.
    /// </summary>
    /// <param name="owner">The repository owner.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="path">The path to the directory to delete.</param>
    /// <param name="message">Optional commit message. If not provided, an automated message will be used.</param>
    /// <param name="branch">Optional branch name. Defaults to "main".</param>
    /// <param name="authorName">Optional author name for the commit.</param>
    /// <param name="authorEmail">Optional author email for the commit.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A list of file commit results for each file deleted.</returns>
    ValueTask<IReadOnlyList<FileCommit>> DeleteDirectory(string owner, string repo, string path, string? message = null, string branch = "main",
        string? authorName = null, string? authorEmail = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all contents from the repository.
    /// </summary>
    /// <param name="owner">The repository owner.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="message">Optional commit message. If not provided, an automated message will be used.</param>
    /// <param name="branch">Optional branch name. Defaults to "main".</param>
    /// <param name="authorName">Optional author name for the commit.</param>
    /// <param name="authorEmail">Optional author email for the commit.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A list of file commit results for each file deleted.</returns>
    ValueTask<IReadOnlyList<FileCommit>> DeleteRepositoryContents(string owner, string repo, string? message = null, string branch = "main",
        string? authorName = null, string? authorEmail = null, CancellationToken cancellationToken = default);
}