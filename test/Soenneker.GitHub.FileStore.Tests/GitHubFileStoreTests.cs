using Soenneker.GitHub.FileStore.Abstract;
using Soenneker.Tests.HostedUnit;

namespace Soenneker.GitHub.FileStore.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public class GitHubFileStoreTests : HostedUnitTest
{
    private readonly IGitHubFileStore _util;

    public GitHubFileStoreTests(Host host) : base(host)
    {
        _util = Resolve<IGitHubFileStore>(true);
    }

    [Test]
    public void Default()
    {

    }
}
