using Microsoft.Extensions.Logging;
using Soenneker.Extensions.Arrays.Bytes;
using Soenneker.Extensions.String;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Soenneker.GitHub.ClientUtil.Abstract;
using Soenneker.GitHub.FileStore.Abstract;
using Soenneker.GitHub.OpenApiClient;
using Soenneker.GitHub.OpenApiClient.Models;
using Soenneker.GitHub.OpenApiClient.Repos.Item.Item.Contents.Item;
using Soenneker.Utils.File.Abstract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.GitHub.FileStore;

///<inheritdoc cref="IGitHubFileStore"/>
public sealed class GitHubFileStore : IGitHubFileStore
{
    private readonly IGitHubOpenApiClientUtil _clientUtil;
    private readonly IFileUtil _fileUtil;
    private readonly ILogger<GitHubFileStore> _logger;

    public GitHubFileStore(IGitHubOpenApiClientUtil clientUtil, IFileUtil fileUtil, ILogger<GitHubFileStore> logger)
    {
        _clientUtil = clientUtil;
        _fileUtil = fileUtil;
        _logger = logger;
    }

    public async ValueTask<ContentFile> Get(string owner, string repo, string path, CancellationToken cancellationToken = default)
    {
        path = path.TrimStart('/');
        _logger.LogInformation("Getting file '{Path}' from '{Owner}/{Repo}'.", path, owner, repo);

        GitHubOpenApiClient client;
        try
        {
            client = await _clientUtil.Get(cancellationToken).NoSync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire GitHub client for Get('{Path}').", path);
            throw;
        }

        WithPathItemRequestBuilder.WithPathGetResponse? response;
        try
        {
            response = await client.Repos[owner][repo].Contents[path].GetAsWithPathGetResponseAsync(cancellationToken: cancellationToken).NoSync();
        }
        catch (Exception ex) when (!(ex is FileNotFoundException))
        {
            _logger.LogError(ex, "Error retrieving content for '{Path}'.", path);
            throw;
        }

        if (response?.ContentFile == null)
        {
            var message = $"File not found: {path}";
            _logger.LogWarning(message);
            throw new FileNotFoundException(message);
        }

        _logger.LogDebug("Successfully retrieved '{Path}'. SHA: {Sha}", path, response.ContentFile.Sha);
        return response.ContentFile;
    }

    public async ValueTask<string> Read(string owner, string repo, string path, CancellationToken cancellationToken = default)
    {
        path = path.TrimStart('/');
        _logger.LogInformation("Reading file '{Path}' as string from '{Owner}/{Repo}'.", path, owner, repo);

        byte[] bytes;
        try
        {
            bytes = await ReadToBytes(owner, repo, path, cancellationToken).NoSync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read file '{Path}' to bytes.", path);
            throw;
        }

        string result = bytes.ToStr();
        _logger.LogDebug("Read {Length} bytes from '{Path}'.", bytes.Length, path);
        return result;
    }

    public async ValueTask<byte[]> ReadToBytes(string owner, string repo, string path, CancellationToken cancellationToken = default)
    {
        path = path.TrimStart('/');
        _logger.LogInformation("Reading file '{Path}' as bytes from '{Owner}/{Repo}'.", path, owner, repo);

        ContentFile contentFile;
        try
        {
            contentFile = await Get(owner, repo, path, cancellationToken).NoSync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get content for '{Path}'.", path);
            throw;
        }

        if (contentFile.Content == null)
        {
            var message = $"File not found: {path}";
            _logger.LogWarning(message);
            throw new FileNotFoundException(message);
        }

        byte[] data;
        if (contentFile.Encoding?.ToLowerInvariantFast() == "base64")
        {
            data = contentFile.Content.ToBytesFromBase64();
            _logger.LogDebug("Decoded base64 content for '{Path}', resulting in {Length} bytes.", path, data.Length);
        }
        else
        {
            data = contentFile.Content.ToBytes();
            _logger.LogDebug("Converted content for '{Path}' to bytes, length {Length}.", path, data.Length);
        }

        return data;
    }

    public async ValueTask ReadToFile(string owner, string repo, string path, string filePath, CancellationToken cancellationToken = default)
    {
        path = path.TrimStart('/');
        _logger.LogInformation("Reading '{Path}' from '{Owner}/{Repo}' and writing to local path '{FilePath}'.", path, owner, repo, filePath);

        byte[] fileBytes;
        try
        {
            fileBytes = await ReadToBytes(owner, repo, path, cancellationToken).NoSync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read '{Path}' to bytes for writing to file.", path);
            throw;
        }

        try
        {
            await _fileUtil.Write(filePath, fileBytes, cancellationToken).NoSync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write bytes to local file '{FilePath}'.", filePath);
            throw;
        }

        _logger.LogDebug("Successfully wrote '{Path}' to '{FilePath}'.", path, filePath);
    }

    public async ValueTask<FileCommit?> Write(string owner, string repo, string path, string content, string? message = null, string branch = "main",
        string? authorName = null, string? authorEmail = null, CancellationToken cancellationToken = default)
    {
        path = path.TrimStart('/');
        _logger.LogInformation("Writing string content to '{Path}' in '{Owner}/{Repo}' on branch '{Branch}'.", path, owner, repo, branch);

        byte[] bytes = content.ToBytes();
        return await WriteBytes(owner, repo, path, bytes, message, branch, authorName, authorEmail, cancellationToken).NoSync();
    }

    public async ValueTask<FileCommit?> WriteFromFile(string owner, string repo, string path, string filePath, string? message = null, string branch = "main",
        string? authorName = null, string? authorEmail = null, CancellationToken cancellationToken = default)
    {
        path = path.TrimStart('/');
        _logger.LogInformation("Reading local file '{LocalFilePath}' and writing to '{Path}' in '{Owner}/{Repo}' on branch '{Branch}'.", filePath, path, owner,
            repo, branch);

        byte[] fileBytes;
        try
        {
            fileBytes = await _fileUtil.ReadToBytes(filePath, cancellationToken).NoSync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read local file '{LocalFilePath}'.", filePath);
            throw;
        }

        return await WriteBytes(owner, repo, path, fileBytes, message, branch, authorName, authorEmail, cancellationToken).NoSync();
    }

    public async ValueTask<FileCommit?> WriteBytes(string owner, string repo, string path, byte[] content, string? message = null, string branch = "main",
        string? authorName = null, string? authorEmail = null, CancellationToken cancellationToken = default)
    {
        path = path.TrimStart('/');
        _logger.LogInformation("Writing binary content to '{Path}' in '{Owner}/{Repo}' on branch '{Branch}'.", path, owner, repo, branch);

        GitHubOpenApiClient client;
        try
        {
            client = await _clientUtil.Get(cancellationToken).NoSync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire GitHub client for WriteBytes('{Path}').", path);
            throw;
        }

        var requestBody = new WithPathPutRequestBody
        {
            Message = message ?? GetDefaultMessage("Write", path),
            Content = Convert.ToBase64String(content),
            Branch = branch,
            Author = authorName != null && authorEmail != null
                ? new WithPathPutRequestBody_author
                {
                    Name = authorName,
                    Email = authorEmail
                }
                : null
        };

        try
        {
            ContentFile existing = await Get(owner, repo, path, cancellationToken).NoSync();
            requestBody.Sha = existing.Sha;
            _logger.LogDebug("Existing file '{Path}' found. Using SHA '{Sha}' for update.", path, existing.Sha);
        }
        catch (FileNotFoundException)
        {
            _logger.LogDebug("No existing file at '{Path}'; creating new.", path);
        }
        catch (BasicError ex) when (ex.Message?.Contains("This repository is empty") == true)
        {
            _logger.LogDebug("Repository is empty; creating first commit without SHA.");
        }

        FileCommit? commit;
        try
        {
            commit = await client.Repos[owner][repo].Contents[path].PutAsync(requestBody, cancellationToken: cancellationToken).NoSync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write bytes to '{Path}'.", path);
            throw;
        }

        if (commit != null)
        {
            _logger.LogDebug("Write to '{Path}' succeeded. Commit SHA: {Sha}", path, commit.Commit?.Sha);
        }
        else
        {
            _logger.LogWarning("Write to '{Path}' returned no commit.", path);
        }

        return commit;
    }

    public async ValueTask<IReadOnlyList<FileCommit>> WriteDirectory(string owner, string repo, string rootPath, string localDirPath, string? message = null,
        string branch = "main", string? authorName = null, string? authorEmail = null, CancellationToken cancellationToken = default)
    {
        rootPath = rootPath.TrimStart('/');
        _logger.LogInformation("Writing directory '{LocalDirectory}' to '{RootPath}' in '{Owner}/{Repo}' on branch '{Branch}'.", localDirPath, rootPath, owner,
            repo, branch);

        IEnumerable<string> filePaths;
        try
        {
            filePaths = Directory.EnumerateFiles(localDirPath, "*", SearchOption.AllDirectories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enumerate files in local directory '{LocalDirectory}'.", localDirPath);
            throw;
        }

        var commits = new List<FileCommit>();

        foreach (string filePath in filePaths)
        {
            string relativePath = Path.GetRelativePath(localDirPath, filePath).Replace('\\', '/');
            var gitHubPath = $"{rootPath.TrimEnd('/')}/{relativePath}";

            _logger.LogDebug("Processing file '{LocalFilePath}', mapping to '{GitHubPath}'.", filePath, gitHubPath);

            byte[] content;
            try
            {
                content = await _fileUtil.ReadToBytes(filePath, cancellationToken).NoSync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read local file '{LocalFilePath}'. Skipping.", filePath);
                continue;
            }

            FileCommit? commit;
            try
            {
                commit = await WriteBytes(owner, repo, gitHubPath, content, message, branch, authorName, authorEmail, cancellationToken).NoSync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write file '{GitHubPath}'. Skipping.", gitHubPath);
                continue;
            }

            if (commit != null)
            {
                commits.Add(commit);
                _logger.LogDebug("Successfully wrote '{GitHubPath}'. Commit SHA: {Sha}", gitHubPath, commit.Commit?.Sha);
            }
        }

        _logger.LogInformation("Completed writing directory. Total commits: {Count}", commits.Count);
        return commits;
    }

    public async ValueTask<FileCommit?> Delete(string owner, string repo, string path, string? message = null, string branch = "main",
        string? authorName = null, string? authorEmail = null, CancellationToken cancellationToken = default)
    {
        path = path.TrimStart('/');
        _logger.LogInformation("Deleting file '{Path}' from '{Owner}/{Repo}' on branch '{Branch}'.", path, owner, repo, branch);

        ContentFile existingFile;
        try
        {
            existingFile = await Get(owner, repo, path, cancellationToken).NoSync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve existing file '{Path}' for deletion.", path);
            throw;
        }

        var requestBody = new WithPathDeleteRequestBody
        {
            Message = message ?? GetDefaultMessage("Delete", path),
            Sha = existingFile.Sha,
            Branch = branch,
            Author = authorName != null && authorEmail != null
                ? new WithPathDeleteRequestBody_author
                {
                    Name = authorName,
                    Email = authorEmail
                }
                : null
        };

        GitHubOpenApiClient client;
        try
        {
            client = await _clientUtil.Get(cancellationToken).NoSync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire GitHub client for Delete('{Path}').", path);
            throw;
        }

        FileCommit? commit;
        try
        {
            commit = await client.Repos[owner][repo].Contents[path].DeleteAsync(requestBody, cancellationToken: cancellationToken).NoSync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete '{Path}'.", path);
            throw;
        }

        if (commit != null)
        {
            _logger.LogDebug("Deleted '{Path}'. Commit SHA: {Sha}", path, commit.Commit?.Sha);
        }
        else
        {
            _logger.LogWarning("Delete operation for '{Path}' returned no commit.", path);
        }

        return commit;
    }

    public async ValueTask<ContentFile[]> List(string owner, string repo, string path, CancellationToken cancellationToken = default)
    {
        path = path.TrimStart('/');
        _logger.LogInformation("Listing contents of directory '{Path}' in '{Owner}/{Repo}'.", path, owner, repo);

        GitHubOpenApiClient client;
        try
        {
            client = await _clientUtil.Get(cancellationToken).NoSync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire GitHub client for List('{Path}').", path);
            throw;
        }

        WithPathItemRequestBuilder.WithPathGetResponse? response;
        try
        {
            response = await client.Repos[owner][repo].Contents[path].GetAsWithPathGetResponseAsync(cancellationToken: cancellationToken).NoSync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving directory contents for '{Path}'.", path);
            throw;
        }

        if (response?.WithPathGetResponseMember1 == null)
        {
            var message = $"Path is not a directory: {path}";
            _logger.LogWarning(message);
            throw new InvalidOperationException(message);
        }

        ContentFile[] items = response.WithPathGetResponseMember1.Select(item => new ContentFile
                                      {
                                          Name = item.AdditionalData["name"] as string,
                                          Path = item.AdditionalData["path"] as string,
                                          Sha = item.AdditionalData["sha"] as string,
                                          Size = item.AdditionalData["size"] as int?,
                                          Type = (item.AdditionalData["type"] as string) == "dir" ? ContentFile_type.File : ContentFile_type.File,
                                          Url = item.AdditionalData["url"] as string
                                      })
                                      .ToArray();

        _logger.LogDebug("Found {Count} items in '{Path}'.", items.Length, path);
        return items;
    }

    public async ValueTask<bool> Exists(string owner, string repo, string path, CancellationToken cancellationToken = default)
    {
        path = path.TrimStart('/');
        _logger.LogInformation("Checking existence of '{Path}' in '{Owner}/{Repo}'.", path, owner, repo);

        try
        {
            await Get(owner, repo, path, cancellationToken).NoSync();
            _logger.LogDebug("'{Path}' exists in '{Owner}/{Repo}'.", path, owner, repo);
            return true;
        }
        catch (FileNotFoundException)
        {
            _logger.LogDebug("'{Path}' does not exist in '{Owner}/{Repo}'.", path, owner, repo);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of '{Path}'.", path);
            return false;
        }
    }

    public async ValueTask<FileCommit?> Copy(string owner, string repo, string sourcePath, string destPath, string? message = null, string branch = "main",
        string? authorName = null, string? authorEmail = null, CancellationToken cancellationToken = default)
    {
        sourcePath = sourcePath.TrimStart('/');
        destPath = destPath.TrimStart('/');
        _logger.LogInformation("Copying file from '{SourcePath}' to '{DestPath}' in '{Owner}/{Repo}'.", sourcePath, destPath, owner, repo);

        byte[] content;
        try
        {
            content = await ReadToBytes(owner, repo, sourcePath, cancellationToken).NoSync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read source file '{SourcePath}'. Copy aborted.", sourcePath);
            throw;
        }

        try
        {
            FileCommit? commit = await WriteBytes(owner, repo, destPath, content, message, branch, authorName, authorEmail, cancellationToken).NoSync();
            _logger.LogDebug("Copied '{SourcePath}' to '{DestPath}'. Commit SHA: {Sha}", sourcePath, destPath, commit?.Commit?.Sha);
            return commit;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write copied content to '{DestPath}'.", destPath);
            throw;
        }
    }

    public async ValueTask<FileCommit?> Move(string owner, string repo, string sourcePath, string destPath, string? message = null, string branch = "main",
        string? authorName = null, string? authorEmail = null, CancellationToken cancellationToken = default)
    {
        sourcePath = sourcePath.TrimStart('/');
        destPath = destPath.TrimStart('/');
        _logger.LogInformation("Moving file from '{SourcePath}' to '{DestPath}' in '{Owner}/{Repo}'.", sourcePath, destPath, owner, repo);

        FileCommit? copyCommit;
        try
        {
            copyCommit = await Copy(owner, repo, sourcePath, destPath, message, branch, authorName, authorEmail, cancellationToken).NoSync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Copy step failed during move from '{SourcePath}' to '{DestPath}'. Move aborted.", sourcePath, destPath);
            throw;
        }

        try
        {
            FileCommit? deleteCommit = await Delete(owner, repo, sourcePath, message, branch, authorName, authorEmail, cancellationToken).NoSync();
            _logger.LogDebug("Deleted original file '{SourcePath}' after move. Commit SHA: {Sha}", sourcePath, deleteCommit?.Commit?.Sha);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete original file '{SourcePath}' after copy. Manual cleanup may be required.", sourcePath);
            throw;
        }

        return copyCommit;
    }

    public ValueTask<ContentFile> GetMetadata(string owner, string repo, string path, CancellationToken cancellationToken = default)
    {
        path = path.TrimStart('/');
        _logger.LogInformation("Getting metadata for '{Path}' in '{Owner}/{Repo}'.", path, owner, repo);
        // This is simply an alias of Get, reserved for future metadata-only implementations.
        return Get(owner, repo, path, cancellationToken);
    }

    public string GetRawDownloadUrl(string owner, string repo, string path, string branch = "main")
    {
        path = path.TrimStart('/');
        _logger.LogInformation("Generating raw download URL for '{Path}' in '{Owner}/{Repo}' on branch '{Branch}'.", path, owner, repo, branch);
        // Format: https://raw.githubusercontent.com/{owner}/{repo}/{branch}/{path}
        return $"https://raw.githubusercontent.com/{owner}/{repo}/{branch}/{path}";
    }

    private static string GetDefaultMessage(string operation, string path)
    {
        return $"[File Store Update] {operation} {path} at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
    }
}