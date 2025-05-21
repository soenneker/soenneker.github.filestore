[![](https://img.shields.io/nuget/v/soenneker.github.filestore.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.github.filestore/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.github.filestore/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.github.filestore/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.github.filestore.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.github.filestore/)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.GitHub.FileStore
### A high-level file system utility for GitHub repositories, built on the GitHub OpenAPI client. Supports reading, writing, deleting, listing, and checking file existence via the GitHub Contents API.

## Features

* 🔍 Read file content from a GitHub repository
* 📝 Write new files with commit messages and optional branch targeting
* ❌ Delete files with SHA validation
* 📂 List directory contents
* ✅ Check for file existence
* ⛓️ Built on top of a typed OpenAPI GitHub client

## Installation

```bash
dotnet add package Soenneker.GitHub.FileStore
````

## Setup

```csharp
builder.Services.AddGitHubFileStoreAsSingleton();
```

This will register all necessary dependencies, including the underlying GitHub OpenAPI client.

ℹ️ **Note**: The GitHub access token must be provided via configuration under the key: `GitHub:Token`.

## Example Usage

```csharp
public class MyService
{
    private readonly IGitHubFileStore _store;

    public MyService(IGitHubFileStore store)
    {
        _store = store;
    }

    public async Task Run()
    {
        string content = await _store.Read("owner", "repo", "README.md");

        await _store.Create("owner", "repo", "newfile.txt", "Hello world!");

        bool exists = await _store.Exists("owner", "repo", "README.md");

        var files = await _store.List("owner", "repo", "docs");

        await _store.Delete("owner", "repo", "oldfile.txt");
    }
}
```

## Related Packages

* [`Soenneker.GitHub.OpenApiClient`](https://www.nuget.org/packages/Soenneker.GitHub.OpenApiClient)