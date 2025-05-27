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

    public GitHubFileStore(IGitHubOpenApiClientUtil clientUtil, IFileUtil fileUtil)
    {
        _clientUtil = clientUtil;
        _fileUtil = fileUtil;
    }

    public async ValueTask<ContentFile> Get(string owner, string repo, string path, CancellationToken cancellationToken = default)
    {
        GitHubOpenApiClient client = await _clientUtil.Get(cancellationToken).NoSync();

        WithPathItemRequestBuilder.WithPathGetResponse? response =
            await client.Repos[owner][repo].Contents[path].GetAsWithPathGetResponseAsync(cancellationToken: cancellationToken).NoSync();

        return response?.ContentFile ?? throw new FileNotFoundException($"File not found: {path}");
    }

    public async ValueTask<string> Read(string owner, string repo, string path, CancellationToken cancellationToken = default)
    {
        byte[] bytes = await ReadToBytes(owner, repo, path, cancellationToken).NoSync();
        return bytes.ToStr();
    }

    public async ValueTask<byte[]> ReadToBytes(string owner, string repo, string path, CancellationToken cancellationToken = default)
    {
        ContentFile contentFile = await Get(owner, repo, path, cancellationToken).NoSync();

        if (contentFile.Content == null)
            throw new FileNotFoundException($"File not found: {path}");

        return contentFile.Encoding?.ToLowerInvariantFast() == "base64" ? contentFile.Content.ToBytesFromBase64() : contentFile.Content.ToBytes();
    }

    public async ValueTask ReadToFile(string owner, string repo, string path, string filePath, CancellationToken cancellationToken = default)
    {
        byte[] fileBytes = await ReadToBytes(owner, repo, path, cancellationToken).NoSync();
        await _fileUtil.Write(filePath, fileBytes, cancellationToken).NoSync();
    }

    public async ValueTask<FileCommit?> Write(string owner, string repo, string path, string content, string? message = null, string branch = "main",
        string? authorName = null, string? authorEmail = null, CancellationToken cancellationToken = default)
    {
        byte[] bytes = content.ToBytes();
        return await WriteBytes(owner, repo, path, bytes, message, branch, authorName, authorEmail, cancellationToken).NoSync();
    }

    public async ValueTask<FileCommit?> WriteFromFile(string owner, string repo, string path, string filePath, string? message = null, string branch = "main",
        string? authorName = null, string? authorEmail = null, CancellationToken cancellationToken = default)
    {
        byte[] fileBytes = await _fileUtil.ReadToBytes(filePath, cancellationToken).NoSync();
        return await WriteBytes(owner, repo, path, fileBytes, message, branch, authorName, authorEmail, cancellationToken).NoSync();
    }

    public async ValueTask<FileCommit?> WriteBytes(string owner, string repo, string path, byte[] content, string? message = null, string branch = "main",
        string? authorName = null, string? authorEmail = null, CancellationToken cancellationToken = default)
    {
        GitHubOpenApiClient client = await _clientUtil.Get(cancellationToken).NoSync();

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

        return await client.Repos[owner][repo].Contents[path].PutAsync(requestBody, cancellationToken: cancellationToken).NoSync();
    }

    public async ValueTask<FileCommit?> Delete(string owner, string repo, string path, string? message = null, string branch = "main",
        string? authorName = null, string? authorEmail = null, CancellationToken cancellationToken = default)
    {
        ContentFile existingFile = await Get(owner, repo, path, cancellationToken).NoSync();

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

        GitHubOpenApiClient client = await _clientUtil.Get(cancellationToken).NoSync();

        return await client.Repos[owner][repo].Contents[path].DeleteAsync(requestBody, cancellationToken: cancellationToken).NoSync();
    }

    public async ValueTask<ContentFile[]> List(string owner, string repo, string path, CancellationToken cancellationToken = default)
    {
        GitHubOpenApiClient client = await _clientUtil.Get(cancellationToken).NoSync();

        WithPathItemRequestBuilder.WithPathGetResponse? response =
            await client.Repos[owner][repo].Contents[path].GetAsWithPathGetResponseAsync(cancellationToken: cancellationToken).NoSync();

        if (response?.WithPathGetResponseMember1 == null)
            throw new InvalidOperationException($"Path is not a directory: {path}");

        return response.WithPathGetResponseMember1.Select(item => new ContentFile
                       {
                           Name = item.AdditionalData["name"] as string,
                           Path = item.AdditionalData["path"] as string,
                           Sha = item.AdditionalData["sha"] as string,
                           Size = item.AdditionalData["size"] as int?,
                           Type = (item.AdditionalData["type"] as string) == "dir" ? ContentFile_type.File : ContentFile_type.File,
                           Url = item.AdditionalData["url"] as string
                       })
                       .ToArray();
    }

    public async ValueTask<bool> Exists(string owner, string repo, string path, CancellationToken cancellationToken = default)
    {
        try
        {
            await Get(owner, repo, path, cancellationToken).NoSync();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static string GetDefaultMessage(string operation, string path)
    {
        return $"[File Store Update] {operation} {path} at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
    }
}