[![](https://img.shields.io/nuget/v/soenneker.github.filestore.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.github.filestore/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.github.filestore/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.github.filestore/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.github.filestore.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.github.filestore/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.github.filestore/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.github.filestore/actions/workflows/codeql.yml)

# Soenneker.GitHub.FileStore

Reads, writes, copies, moves, lists, and deletes repository files through GitHub's Contents API.

## Installation

```bash
dotnet add package Soenneker.GitHub.FileStore
```

## Configure and register

```json
{
  "GH": {
    "Token": "your-github-token"
  }
}
```

```csharp
using Soenneker.GitHub.FileStore.Registrars;

builder.Services.AddGitHubFileStoreAsSingleton();
```

Use a token with Contents read permission for `Get`, `Read`, `List`, and `Exists`. Writes and deletes require Contents write permission. Keep the token in secret storage.

## Read and write files

```csharp
string content = await store.Read(
    "example-org", "example-repository", "README.md", cancellationToken);

FileCommit? commit = await store.Write(
    owner: "example-org",
    repo: "example-repository",
    path: "docs/example.txt",
    content: "Hello from the Contents API",
    message: "Add example document",
    branch: "main",
    cancellationToken: cancellationToken);
```

`Write` and `WriteBytes` create a file when it does not exist and use its current SHA when updating it. The lookup is performed on the requested branch, so non-default branch updates do not reuse a SHA from `main`. Supplying both `authorName` and `authorEmail` sets the commit author; supplying only one leaves the author unset.

## Copy, move, and directory operations

```csharp
await store.Copy(
    "example-org", "example-repository",
    "docs/source.txt", "archive/source.txt",
    branch: "release",
    cancellationToken: cancellationToken);

IReadOnlyList<FileCommit> deleted = await store.DeleteDirectory(
    "example-org", "example-repository", "old-docs",
    branch: "release",
    cancellationToken: cancellationToken);
```

Copy and move read from the same branch they write to. A move is two commits—copy, then delete—so a failed delete can leave both paths and throws for the caller to handle. `WriteDirectory` and recursive deletion process files as separate commits and continue past individual file failures; their returned lists contain only successful commits.

`Delete`, `DeleteDirectory`, and `DeleteRepositoryContents` are destructive. They retrieve branch-specific SHAs immediately before deletion, but concurrent changes can still cause GitHub to reject a stale SHA.

`Exists()` returns `false` only for a not-found response. Authentication, permission, rate-limit, and transport failures propagate instead of being reported as a missing file.
