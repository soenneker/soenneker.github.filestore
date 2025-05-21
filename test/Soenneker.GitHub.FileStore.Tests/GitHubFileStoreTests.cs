using Soenneker.GitHub.FileStore.Abstract;
using Soenneker.Tests.FixturedUnit;
using Xunit;

namespace Soenneker.GitHub.FileStore.Tests;

[Collection("Collection")]
public class GitHubFileStoreTests : FixturedUnitTest
{
    private readonly IGitHubFileStore _util;

    public GitHubFileStoreTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        _util = Resolve<IGitHubFileStore>(true);
    }

    [Fact]
    public void Default()
    {

    }
}
