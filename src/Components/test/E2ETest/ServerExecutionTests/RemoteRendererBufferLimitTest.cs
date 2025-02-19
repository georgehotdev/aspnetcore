// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Ignitor;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;

[QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/19666")]
public class RemoteRendererBufferLimitTest : IgnitorTest<ServerStartup>
{
    public RemoteRendererBufferLimitTest(BasicTestAppServerSiteFixture<ServerStartup> serverFixture, ITestOutputHelper output)
        : base(serverFixture, output)
    {
    }

    [Fact(Skip = "https://github.com/dotnet/aspnetcore/issues/19666")]
    public async Task DispatchedEventsWillKeepBeingProcessed_ButUpdatedWillBeDelayedUntilARenderIsAcknowledged()
    {
        // Arrange
        var baseUri = new Uri(ServerFixture.RootUri, "/subdir");
        await ConnectAutomaticallyAndWait(baseUri);

        await Client.SelectAsync("test-selector-select", "BasicTestApp.LimitCounterComponent");
        Client.ConfirmRenderBatch = false;

        for (var i = 0; i < 10; i++)
        {
            await Client.ClickAsync("increment");
        }
        await Client.ClickAsync("increment", expectRenderBatch: false);

        Assert.Single(Logs, l => (LogLevel.Debug, "The queue of unacknowledged render batches is full.") == (l.LogLevel, l.Message));
        Assert.Equal("10", ((TextNode)Client.FindElementById("the-count").Children.Single()).TextContent);
        var fullCount = Batches.Count;

        // Act
        await Client.ClickAsync("increment", expectRenderBatch: false);

        Assert.Contains(Logs, l => (LogLevel.Debug, "The queue of unacknowledged render batches is full.") == (l.LogLevel, l.Message));
        Assert.Equal(fullCount, Batches.Count);
        Client.ConfirmRenderBatch = true;

        // This will resume the render batches.
        await Client.ExpectRenderBatch(() => Client.ConfirmBatch(Batches.Last().Id));

        // Assert
        Assert.Equal("12", ((TextNode)Client.FindElementById("the-count").Children.Single()).TextContent);
        Assert.Equal(fullCount + 1, Batches.Count);
    }
}
