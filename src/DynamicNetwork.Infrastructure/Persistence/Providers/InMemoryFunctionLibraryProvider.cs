using DynamicNetwork.Application.Interfaces.Providers;
using DynamicNetwork.Domain.Functions;

namespace DynamicNetwork.Infrastructure.Persistence.Providers;

public class InMemoryFunctionLibraryProvider : IFunctionLibraryProvider
{
    private FunctionLibrary _library;

    public InMemoryFunctionLibraryProvider()
    {
        _library = new FunctionLibrary(
            Enumerable.Empty<ProcessType>(),
            Enumerable.Empty<TransportType>(),
            Enumerable.Empty<StorageType>(),
            Enumerable.Empty<FlowType>());
    }

    public InMemoryFunctionLibraryProvider(FunctionLibrary initialLibrary)
    {
        ArgumentNullException.ThrowIfNull(initialLibrary);

        _library = initialLibrary;
    }

    public FunctionLibrary GetCurrent() => _library;

    public void Update(FunctionLibrary library)
    {
        ArgumentNullException.ThrowIfNull(library);

        _library = library;
    }
}
