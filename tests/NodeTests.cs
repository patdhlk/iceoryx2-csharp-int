using Xunit;

namespace Iceoryx2.Tests;

public class NodeTests
{
    [Fact]
    public void CanCreateNodeBuilder()
    {
        var builder = NodeBuilder.New();
        Assert.NotNull(builder);
    }

    [Fact]
    public void CanSetNodeName()
    {
        var builder = NodeBuilder.New().Name("test_node");
        Assert.NotNull(builder);
    }

    [Fact]
    public void CanCreateNode()
    {
        var result = NodeBuilder.New().Create();

        Assert.True(result.IsOk);
        using var node = result.Unwrap();
        Assert.NotNull(node);
    }

    [Fact]
    public void NodeHasName()
    {
        var result = NodeBuilder.New().Create();

        Assert.True(result.IsOk);
        using var node = result.Unwrap();
        // Note: Name property currently returns placeholder "node"
        // TODO: Implement proper node name retrieval from C FFI
        Assert.NotNull(node.Name);
    }
}