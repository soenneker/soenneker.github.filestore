using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.GitHub.ClientUtil.Registrars;
using Soenneker.GitHub.FileStore.Abstract;
using Soenneker.Utils.File.Registrars;

namespace Soenneker.GitHub.FileStore.Registrars;

/// <summary>
/// A high-level file system utility for GitHub repositories, built on the GitHub OpenAPI client. Supports reading, writing, deleting, listing, and checking file existence via the GitHub Contents API.
/// </summary>
public static class GitHubFileStoreRegistrar
{
    /// <summary>
    /// Adds <see cref="IGitHubFileStore"/> as a singleton service. <para/>
    /// </summary>
    public static IServiceCollection AddGitHubFileStoreAsSingleton(this IServiceCollection services)
    {
        services.AddGitHubOpenApiClientUtilAsSingleton().AddFileUtilAsSingleton().TryAddSingleton<IGitHubFileStore, GitHubFileStore>();

        return services;
    }

    /// <summary>
    /// Adds <see cref="IGitHubFileStore"/> as a scoped service. <para/>
    /// </summary>
    public static IServiceCollection AddGitHubFileStoreAsScoped(this IServiceCollection services)
    {
        services.AddGitHubOpenApiClientUtilAsSingleton().AddFileUtilAsScoped().TryAddScoped<IGitHubFileStore, GitHubFileStore>();

        return services;
    }
}