using DynamicNetwork.Domain.Graph;

namespace DynamicNetwork.Tests;

[TestClass]
public class TimeIntervalTests
{
    [TestMethod]
    public void Constructor_InvalidRange_Throws()
    {
        Assert.ThrowsException<ArgumentException>(() => new TimeInterval(10, 5));
    }

    [TestMethod]
    public void Overlaps_Intersection_And_Covers_WorkCorrectly()
    {
        var first = new TimeInterval(0, 10);
        var second = new TimeInterval(5, 15);
        var third = new TimeInterval(10, 20);

        Assert.IsTrue(first.Overlaps(second));
        Assert.IsFalse(first.Overlaps(third));

        var intersection = first.Intersection(second);
        Assert.IsNotNull(intersection);
        Assert.AreEqual(new TimeInterval(5, 10), intersection);

        Assert.IsTrue(first.Covers(0));
        Assert.IsTrue(first.Covers(10));
        Assert.IsTrue(first.Covers(new TimeInterval(2, 8)));
        Assert.IsFalse(first.Covers(new TimeInterval(-1, 20)));
    }

    [TestMethod]
    public void ComparisonOperators_WorkAsExpected()
    {
        var first = new TimeInterval(0, 5);
        var second = new TimeInterval(0, 10);

        Assert.IsTrue(first < second);
        Assert.IsTrue(second > first);
        Assert.IsFalse(first != new TimeInterval(0, 5));
    }

    [TestMethod]
    public void Intersection_NoOverlap_ReturnsNull()
    {
        var first = new TimeInterval(0, 5);
        var second = new TimeInterval(6, 10);

        Assert.IsNull(first.Intersection(second));
    }

    [TestMethod]
    public void EqualsAndHashCode_EqualIntervals_Work()
    {
        var first = new TimeInterval(0, 5);
        var second = new TimeInterval(0, 5);

        Assert.IsTrue(first.Equals(second));
        Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
    }
}
