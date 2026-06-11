using DynamicNetwork.Domain.Analysis;

namespace DynamicNetwork.Tests;

[TestClass]
public class MatrixTests
{
    [TestMethod]
    public void Constructor_ThrowsWhenSizesDoNotMatch()
    {
        var rowLabels = new[] { "A", "B" };
        var columnLabels = new[] { "X" };
        var values = new List<IReadOnlyList<int>>
        {
            new[] { 1 }
        };

        Assert.ThrowsException<InvalidOperationException>(() =>
            new Matrix<int>(rowLabels, columnLabels, values));

        var values2 = new List<IReadOnlyList<int>>
        {
            new[] { 1, 2 }
        };

        Assert.ThrowsException<InvalidOperationException>(() =>
            new Matrix<int>(rowLabels, columnLabels, values2));
    }

    [TestMethod]
    public void ToFlatList_ReturnsExpectedCells()
    {
        var matrix = new Matrix<int>(
            new[] { "A", "B" },
            new[] { "X", "Y" },
            new List<IReadOnlyList<int>>
            {
                new[] { 1, 2 },
                new[] { 3, 4 }
            });

        var flat = matrix.ToFlatList().ToList();

        Assert.AreEqual(4, flat.Count);
        Assert.AreEqual("A", flat[0].RowLabel);
        Assert.AreEqual("X", flat[0].ColumnLabel);
        Assert.AreEqual(1, flat[0].Value);
        Assert.AreEqual("B", flat[3].RowLabel);
        Assert.AreEqual("Y", flat[3].ColumnLabel);
        Assert.AreEqual(4, flat[3].Value);
    }
}
