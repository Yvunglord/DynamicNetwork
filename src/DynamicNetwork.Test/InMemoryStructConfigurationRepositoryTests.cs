using DynamicNetwork.Domain.Configuration;
using DynamicNetwork.Domain.Graph;
using DynamicNetwork.Infrastructure.Persistence.Repositories;

namespace DynamicNetwork.Tests;

[TestClass]
public class InMemoryStructConfigurationRepositoryTests
{
    [TestMethod]
    public void Add_ReturnsFalseWhenIntervalExists()
    {
        var repo = new InMemoryStructConfigurationRepository();
        var config = new StructConfiguration(new TimeInterval(0, 10), Array.Empty<NodeConfiguration>(), Array.Empty<LinkConfiguration>());

        Assert.IsTrue(repo.Add(config));
        Assert.IsFalse(repo.Add(config));
    }

    [TestMethod]
    public void GetByInterval_And_Exists_Work()
    {
        var repo = new InMemoryStructConfigurationRepository();
        var config = new StructConfiguration(new TimeInterval(0, 10), Array.Empty<NodeConfiguration>(), Array.Empty<LinkConfiguration>());
        repo.Add(config);

        Assert.IsTrue(repo.Exists(config.Interval));
        Assert.AreSame(config, repo.GetByInterval(config.Interval));
    }

    [TestMethod]
    public void GetByTimeRange_ReturnsOverlappingConfigurations()
    {
        var repo = new InMemoryStructConfigurationRepository();
        var a = new StructConfiguration(new TimeInterval(0, 10), Array.Empty<NodeConfiguration>(), Array.Empty<LinkConfiguration>());
        var b = new StructConfiguration(new TimeInterval(10, 20), Array.Empty<NodeConfiguration>(), Array.Empty<LinkConfiguration>());

        repo.Add(a);
        repo.Add(b);

        var result = repo.GetByTimeRange(new TimeInterval(5, 15));

        Assert.AreEqual(2, result.Count);
    }

    [TestMethod]
    public void Update_ReplacesExistingConfiguration()
    {
        var repo = new InMemoryStructConfigurationRepository();
        var original = new StructConfiguration(new TimeInterval(0, 10), Array.Empty<NodeConfiguration>(), Array.Empty<LinkConfiguration>());
        repo.Add(original);

        var updated = new StructConfiguration(new TimeInterval(0, 10), new[] { new NodeConfiguration("A", Array.Empty<string>(), new Dictionary<string, double>(), Array.Empty<string>(), new Dictionary<string, double>(), Array.Empty<string>()) }, Array.Empty<LinkConfiguration>());
        repo.Update(updated);

        Assert.AreSame(updated, repo.GetByInterval(updated.Interval));
    }

    [TestMethod]
    public void Delete_RemovesConfiguration()
    {
        var repo = new InMemoryStructConfigurationRepository();
        var config = new StructConfiguration(new TimeInterval(0, 10), Array.Empty<NodeConfiguration>(), Array.Empty<LinkConfiguration>());
        repo.Add(config);
        repo.Delete(config.Interval);

        Assert.IsNull(repo.GetByInterval(config.Interval));
        Assert.IsFalse(repo.Exists(config.Interval));
    }

    [TestMethod]
    public void GetAll_ReturnsAllConfigurations()
    {
        var repo = new InMemoryStructConfigurationRepository();
        var a = new StructConfiguration(new TimeInterval(0, 10), Array.Empty<NodeConfiguration>(), Array.Empty<LinkConfiguration>());
        var b = new StructConfiguration(new TimeInterval(10, 20), Array.Empty<NodeConfiguration>(), Array.Empty<LinkConfiguration>());

        repo.Add(a);
        repo.Add(b);

        var all = repo.GetAll();

        Assert.AreEqual(2, all.Count);
        CollectionAssert.Contains(all.ToList(), a);
        CollectionAssert.Contains(all.ToList(), b);
    }

    [TestMethod]
    public void Update_AddsNewConfigurationWhenIntervalMissing()
    {
        var repo = new InMemoryStructConfigurationRepository();
        var config = new StructConfiguration(new TimeInterval(0, 10), Array.Empty<NodeConfiguration>(), Array.Empty<LinkConfiguration>());

        repo.Update(config);

        Assert.IsTrue(repo.Exists(config.Interval));
        Assert.AreSame(config, repo.GetByInterval(config.Interval));
    }

    [TestMethod]
    public void GetByTimeRange_ReturnsEmptyWhenNoOverlap()
    {
        var repo = new InMemoryStructConfigurationRepository();
        var config = new StructConfiguration(new TimeInterval(0, 10), Array.Empty<NodeConfiguration>(), Array.Empty<LinkConfiguration>());
        repo.Add(config);

        var result = repo.GetByTimeRange(new TimeInterval(10, 20));

        Assert.AreEqual(0, result.Count);
    }
}
