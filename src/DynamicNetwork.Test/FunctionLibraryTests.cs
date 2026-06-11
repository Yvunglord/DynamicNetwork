using DynamicNetwork.Domain.Functions;

namespace DynamicNetwork.Tests;

[TestClass]
public class FunctionLibraryTests
{
    [TestMethod]
    public void AddProcesses_DoesNotDuplicateExistingIds()
    {
        var existing = new FunctionLibrary(
            new[] { new ProcessType("P1", 1, "A", "B", 0.5) },
            Array.Empty<TransportType>(),
            Array.Empty<StorageType>(),
            Array.Empty<FlowType>());

        var updated = existing.AddProcesses(new[] {
            new ProcessType("P1", 2, "A", "B", 1.0),
            new ProcessType("P2", 3, "B", "C", 1.0)
        });

        Assert.AreEqual(2, updated.Processes.Count);
        Assert.IsTrue(updated.Processes.Any(p => p.Id == "P1"));
        Assert.IsTrue(updated.Processes.Any(p => p.Id == "P2"));
        Assert.AreEqual(1, updated.Processes.Count(p => p.Id == "P1"));
    }

    [TestMethod]
    public void UpdateProcesses_ReplacesExistingAndAddsNew()
    {
        var source = new FunctionLibrary(
            new[] { new ProcessType("P1", 1, "A", "B", 0.5) },
            Array.Empty<TransportType>(),
            Array.Empty<StorageType>(),
            Array.Empty<FlowType>());

        var updated = source.UpdateProcesses(new[] {
            new ProcessType("P1", 4, "A", "C", 2.0),
            new ProcessType("P2", 5, "C", "D", 1.5)
        });

        Assert.AreEqual(2, updated.Processes.Count);
        Assert.AreEqual(4, updated.GetProcessById("P1")!.TimePerUnit);
        Assert.AreEqual("C", updated.GetProcessById("P1")!.OutputFlowType);
        Assert.IsNotNull(updated.GetProcessById("P2"));
    }

    [TestMethod]
    public void GetById_ReturnsNullWhenMissing()
    {
        var library = new FunctionLibrary(
            Array.Empty<ProcessType>(),
            Array.Empty<TransportType>(),
            Array.Empty<StorageType>(),
            new[] { new FlowType("F1") });

        Assert.IsNull(library.GetProcessById("P1"));
        Assert.IsNotNull(library.GetFlowById("F1"));
    }

    [TestMethod]
    public void UpdateProcesses_NullCollection_Throws()
    {
        var library = new FunctionLibrary(
            Array.Empty<ProcessType>(),
            Array.Empty<TransportType>(),
            Array.Empty<StorageType>(),
            Array.Empty<FlowType>());

        Assert.ThrowsException<ArgumentNullException>(() => library.UpdateProcesses(null!));
    }

    [TestMethod]
    public void RemoveProcesses_DeletesExistingIds()
    {
        var library = new FunctionLibrary(
            new[] { new ProcessType("P1", 1, "A", "B", 0.5), new ProcessType("P2", 2, "B", "C", 0.8) },
            Array.Empty<TransportType>(),
            Array.Empty<StorageType>(),
            Array.Empty<FlowType>());

        var result = library.RemoveProcesses(new[] { "P1" });

        Assert.AreEqual(1, result.Processes.Count);
        Assert.IsNull(result.GetProcessById("P1"));
        Assert.IsNotNull(result.GetProcessById("P2"));
    }

    [TestMethod]
    public void AddTransports_DoesNotDuplicateExistingIds()
    {
        var library = new FunctionLibrary(
            Array.Empty<ProcessType>(),
            new[] { new TransportType("T1", 0.2, "FlowA", 10) },
            Array.Empty<StorageType>(),
            Array.Empty<FlowType>());

        var result = library.AddTransports(new[] { new TransportType("T1", 0.3, "FlowA", 20), new TransportType("T2", 0.4, "FlowB", 30) });

        Assert.AreEqual(2, result.Transports.Count);
        Assert.AreEqual("T1", result.GetTransportById("T1")!.Id);
        Assert.AreEqual("T2", result.GetTransportById("T2")!.Id);
    }

    [TestMethod]
    public void UpdateTransports_ReplacesExistingAndKeepsOthers()
    {
        var library = new FunctionLibrary(
            Array.Empty<ProcessType>(),
            new[] { new TransportType("T1", 0.2, "FlowA", 10) },
            Array.Empty<StorageType>(),
            Array.Empty<FlowType>());

        var updated = library.UpdateTransports(new[] { new TransportType("T1", 0.5, "FlowA", 50), new TransportType("T2", 0.4, "FlowB", 30) });

        Assert.AreEqual(2, updated.Transports.Count);
        Assert.AreEqual(0.5, updated.GetTransportById("T1")!.Time);
        Assert.IsNotNull(updated.GetTransportById("T2"));
    }

    [TestMethod]
    public void AddStoragesAndRemoveStorages_WorkCorrectly()
    {
        var library = new FunctionLibrary(
            Array.Empty<ProcessType>(),
            Array.Empty<TransportType>(),
            new[] { new StorageType("S1", new[] { "F1" }) },
            Array.Empty<FlowType>());

        var added = library.AddStorages(new[] { new StorageType("S1", new[] { "F2" }), new StorageType("S2", new[] { "F3" }) });

        Assert.AreEqual(2, added.Storages.Count);
        Assert.IsNotNull(added.GetStorageById("S2"));

        var removed = added.RemoveStorages(new[] { "S1" });
        Assert.AreEqual(1, removed.Storages.Count);
        Assert.IsNull(removed.GetStorageById("S1"));
    }

    [TestMethod]
    public void UpdateStorages_ReplacesExistingStorage()
    {
        var library = new FunctionLibrary(
            Array.Empty<ProcessType>(),
            Array.Empty<TransportType>(),
            new[] { new StorageType("S1", new[] { "F1" }) },
            Array.Empty<FlowType>());

        var updated = library.UpdateStorages(new[] { new StorageType("S1", new[] { "F2" }), new StorageType("S2", new[] { "F3" }) });

        Assert.AreEqual(2, updated.Storages.Count);
        Assert.AreEqual("F2", updated.GetStorageById("S1")!.AllowedFlowTypes.First());
        Assert.IsNotNull(updated.GetStorageById("S2"));
    }

    [TestMethod]
    public void AddFlowsRemoveFlowsAndUpdateFlows_WorkCorrectly()
    {
        var library = new FunctionLibrary(
            Array.Empty<ProcessType>(),
            Array.Empty<TransportType>(),
            Array.Empty<StorageType>(),
            new[] { new FlowType("F1") });

        var added = library.AddFlows(new[] { new FlowType("F1"), new FlowType("F2") });
        Assert.AreEqual(2, added.Flows.Count);

        var removed = added.RemoveFlows(new[] { "F1" });
        Assert.AreEqual(1, removed.Flows.Count);
        Assert.IsNull(removed.GetFlowById("F1"));

        var updated = removed.UpdateFlows(new[] { new FlowType("F2") });
        Assert.AreEqual(1, updated.Flows.Count);
        Assert.IsNotNull(updated.GetFlowById("F2"));
    }

    [TestMethod]
    public void GetTransportStorageFlowById_ReturnsCorrectValues()
    {
        var library = new FunctionLibrary(
            Array.Empty<ProcessType>(),
            new[] { new TransportType("T1", 0.2, "FlowA", 10) },
            new[] { new StorageType("S1", new[] { "F1" }) },
            new[] { new FlowType("F1") });

        Assert.IsNotNull(library.GetTransportById("T1"));
        Assert.IsNotNull(library.GetStorageById("S1"));
        Assert.IsNotNull(library.GetFlowById("F1"));
        Assert.IsNull(library.GetTransportById("Unknown"));
        Assert.IsNull(library.GetStorageById("Unknown"));
        Assert.IsNull(library.GetFlowById("Unknown"));
    }
}
