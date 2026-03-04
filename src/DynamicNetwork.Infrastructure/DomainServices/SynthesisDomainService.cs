using DynamicNetwork.Domain.Configuration;
using DynamicNetwork.Domain.Functions;
using DynamicNetwork.Domain.Graph;
using DynamicNetwork.Domain.Paths;
using DynamicNetwork.Domain.Services;
using DynamicNetwork.Domain.Synthesis;

namespace DynamicNetwork.Infrastructure.DomainServices;

/// <summary>
/// Сервис синтеза конфигураций структуры сети на основе потоков данных.
/// 
/// Алгоритм преобразует абстрактные требования потоков (DataFlow) в конкретные
/// конфигурации узлов и связей (StructConfiguration), гарантируя выполнение
/// всех необходимых трансформаций данных в заданные временные интервалы.
/// 
/// Архитектура (4 этапа):
/// 1. BuildIntervalToBaseConfigMap — маппинг интервалов на базовые конфигурации
/// 2. PlanFlowRoutes — планирование маршрутов и трансформаций для каждого потока
/// 3. AggregateResourceRequirements — агрегация требований к ресурсам
/// 4. SynthesizeConfigurations — генерация финальных конфигураций из библиотеки функций
/// 
/// Ключевые принципы:
/// • Декларативный синтез: адаптация базовых конфигураций, а не создание с нуля
/// • Временная привязка: все требования и конфигурации имеют TimeInterval
/// • Разделение ресурсов: процессы, транспорты и хранилища учитываются независимо
/// • Библиотечная модель: выбор функций только из разрешённых в FunctionLibrary
/// </summary>
public class SynthesisDomainService : ISynthesisDomainService
{
    /// <summary>
    /// Выполняет полный цикл синтеза конфигураций для заданных потоков данных.
    /// </summary>
    /// <param name="request">Параметры запроса на синтез</param>
    /// <param name="graphs">Временные графы сети</param>
    /// <param name="flows">Потоки данных, требующие маршрутизации</param>
    /// <param name="functionLibrary">Библиотека доступных функций (процессы/транспорты/хранилища)</param>
    /// <param name="baseConfigurations">Базовые конфигурации-шаблоны</param>
    /// <param name="reachablePaths">Предварительно найденные достижимые пути</param>
    /// <returns>Список синтезированных конфигураций, отсортированных по времени</returns>
    public IReadOnlyList<StructConfiguration> SynthesizeAll(
        StructConfigurationSynthesisRequest request,
        IReadOnlyList<TemporalGraph> graphs,
        IReadOnlyList<DataFlow> flows,
        FunctionLibrary functionLibrary,
        IReadOnlyList<StructConfiguration> baseConfigurations,
        IReadOnlyList<ReachabilityPath> reachablePaths)
    {
        // =====================================================================
        // ЭТАП 1: Построение маппинга интервалов -> базовые конфигурации
        // =====================================================================
        // Создаёт словарь для быстрого доступа к шаблону конфигурации по интервалу времени
        var intervalToBaseConfig = BuildIntervalToBaseConfigMap(
            graphs,
            reachablePaths,
            baseConfigurations);

        // =====================================================================
        // ЭТАП 2: Планирование маршрутов для каждого потока
        // =====================================================================
        // Для каждого потока и каждого достижимого пути строится план выполнения:
        // • Последовательность трансформаций типов данных на узлах
        // • Маршруты транспортировки между узлами
        // • Отслеживание типа данных на каждом этапе пути
        var flowRoutePlans = new List<FlowRoutePlan>();
        foreach (var flow in flows)
        {
            flowRoutePlans.AddRange(PlanFlowRoutes(
                flow,
                reachablePaths,
                request,
                intervalToBaseConfig));
        }

        // =====================================================================
        // ЭТАП 3: Агрегация требований к ресурсам
        // =====================================================================
        // Объединяет требования всех планов в единую структуру:
        // • NodeProcessRequirements: какие трансформации нужны на каждом узле
        // • LinkTransportRequirements: какие типы данных транспортировать по связям
        // • NodeStorageRequirements: какие объёмы данных хранить на узлах
        var resourceRequirements = AggregateResourceRequirements(
            flowRoutePlans,
            intervalToBaseConfig);

        // =====================================================================
        // ЭТАП 4: Синтез финальных конфигураций
        // =====================================================================
        // На основе агрегированных требований и библиотеки функций генерирует
        // итоговые конфигурации узлов и связей для каждого временного интервала
        var synthesizedStructs = SynthesizeConfigurations(
            resourceRequirements,
            intervalToBaseConfig,
            functionLibrary);

        // Возвращаем конфигурации, отсортированные по времени начала интервала
        return synthesizedStructs
            .OrderBy(c => c.Interval.Start)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Строит словарь маппинга: TimeInterval -> StructConfiguration.
    /// Используется для быстрого поиска базовой конфигурации-шаблона по интервалу.
    /// </summary>
    /// <remarks>
    /// Важное ограничение: маппинг работает только при ТОЧНОМ совпадении интервалов.
    /// Частичные перекрытия интервалов не обрабатываются на этом этапе.
    /// </remarks>
    private Dictionary<TimeInterval, StructConfiguration> BuildIntervalToBaseConfigMap(
        IReadOnlyList<TemporalGraph> graphs,
        IReadOnlyList<ReachabilityPath> paths,
        IReadOnlyList<StructConfiguration> baseConfigurations)
    {
        var intervalToBaseConfig = new Dictionary<TimeInterval, StructConfiguration>();

        // Извлекаем уникальные интервалы из найденных путей
        var intervals = paths
            .Select(p => p.Interval)
            .Where(i => i != TimeInterval.Empty)
            .Distinct()
            .OrderBy(i => i.Start)
            .ToList();

        // Шаг 1: Маппинг интервалов путей на базовые конфигурации
        foreach (var interval in intervals)
        {
            // Ищем конфигурацию с точным совпадением интервала
            var baseConfig = baseConfigurations.FirstOrDefault(c => c.Interval == interval);
            if (baseConfig != null)
                intervalToBaseConfig[interval] = baseConfig;
        }

        // Шаг 2: Дополняем маппинг интервалами графов (для покрытия всех временных срезов)
        if (paths.Any())
        {
            var firstPathInterval = paths.First().Interval;
            foreach (var graph in graphs.Where(g => g.Interval.Overlaps(firstPathInterval)))
            {
                if (!intervalToBaseConfig.ContainsKey(graph.Interval))
                {
                    var baseConfig = baseConfigurations.FirstOrDefault(c => c.Interval == graph.Interval);
                    if (baseConfig != null)
                        intervalToBaseConfig[graph.Interval] = baseConfig;
                }
            }
        }

        return intervalToBaseConfig;
    }

    /// <summary>
    /// Планирует маршруты и трансформации для одного потока данных по всем достижимым путям.
    /// </summary>
    /// <returns>
    /// Список планов (FlowRoutePlan), каждый из которых содержит:
    /// • Последовательность шагов трансформации/транспортировки
    /// • Тип данных на каждом узле пути
    /// • Ссылки на исходный поток и путь
    /// </returns>
    private List<FlowRoutePlan> PlanFlowRoutes(
       DataFlow flow,
       IReadOnlyList<ReachabilityPath> allPaths,
       StructConfigurationSynthesisRequest request,
       Dictionary<TimeInterval, StructConfiguration> intervalToBaseConfig)
    {
        var plans = new List<FlowRoutePlan>();

        foreach (var path in allPaths)
        {
            var transformations = new List<FlowTransformationStep>();
            var nodeFlowTypes = new Dictionary<string, string>();

            // Определяем начальный тип данных для потока
            // Если есть трансформации — берём входной тип первой, иначе используем ID потока
            var currentFlowType = flow.Transformations.Any()
                ? flow.Transformations.First().InputType
                : flow.Id;

            // Инициализируем тип данных для стартового узла
            nodeFlowTypes[path.Nodes[0]] = currentFlowType;

            // -----------------------------------------------------------------
            // Часть A: Планирование трансформаций на узлах
            // -----------------------------------------------------------------
            for (int i = 0; i < path.Nodes.Count; i++)
            {
                var currentNode = path.Nodes[i];
                var edgeInterval = path.Interval;

                // Ищем трансформацию, применимую к текущему типу данных
                var requiredTransformation = flow.Transformations
                    .FirstOrDefault(t => t.InputType == currentFlowType);

                if (requiredTransformation != null)
                {
                    // Добавляем шаг трансформации: узел преобразует InputType -> OutputType
                    transformations.Add(new FlowTransformationStep
                    {
                        NodeId = currentNode,
                        InputType = currentFlowType,
                        OutputType = requiredTransformation.OutputType,
                        Interval = edgeInterval,
                        FlowVolume = flow.Volume
                    });

                    // Обновляем тип данных после применения трансформации
                    currentFlowType = requiredTransformation.OutputType;
                    nodeFlowTypes[currentNode] = currentFlowType;
                }
            }

            // -----------------------------------------------------------------
            // Часть B: Планирование транспортировки по рёбрам
            // -----------------------------------------------------------------
            for (int i = 0; i < path.Edges.Count; i++)
            {
                var edge = path.Edges[i];
                var currentNode = path.Nodes[i];
                var nextNode = path.Nodes[i + 1];
                var edgeInterval = path.Interval;

                // Добавляем шаг транспортировки: данные текущего типа перемещаются между узлами
                transformations.Add(new FlowTransformationStep
                {
                    SourceNode = currentNode,
                    TargetNode = nextNode,
                    TransportType = currentFlowType, // текущий тип данных после всех трансформаций
                    Interval = edgeInterval,
                    FlowVolume = flow.Volume
                });
            }

            plans.Add(new FlowRoutePlan
            {
                Flow = flow,
                Path = path,
                Transformations = transformations,
                NodeFlowTypes = nodeFlowTypes
            });
        }

        return plans;
    }

    /// <summary>
    /// Агрегирует требования к ресурсам из всех планов маршрутов.
    /// </summary>
    /// <remarks>
    /// Структура агрегации:
    /// • Процессы на узлах: объединение через HashSet (уникальные трансформации)
    /// • Транспорты на связях: объединение через HashSet (уникальные типы данных)
    /// • Хранилища на узлах: суммирование объёмов по типу данных и интервалу
    /// </remarks>
    private ResourceRequirements AggregateResourceRequirements(
        List<FlowRoutePlan> flowRoutePlans,
        Dictionary<TimeInterval, StructConfiguration> intervalToBaseConfig)
    {
        var requirements = new ResourceRequirements
        {
            NodeProcessRequirements = new Dictionary<string, Dictionary<TimeInterval, HashSet<string>>>(),
            LinkTransportRequirements = new Dictionary<(string, string), Dictionary<TimeInterval, HashSet<string>>>(),
            NodeStorageRequirements = new Dictionary<string, Dictionary<TimeInterval, Dictionary<string, double>>>()
        };

        foreach (var plan in flowRoutePlans)
        {
            // Обработка шагов трансформации и транспортировки
            foreach (var transformation in plan.Transformations)
            {
                // === Требования к процессам на узлах ===
                if (!string.IsNullOrEmpty(transformation.NodeId))
                {
                    if (!requirements.NodeProcessRequirements.ContainsKey(transformation.NodeId))
                        requirements.NodeProcessRequirements[transformation.NodeId] = new Dictionary<TimeInterval, HashSet<string>>();

                    if (!requirements.NodeProcessRequirements[transformation.NodeId].ContainsKey(transformation.Interval))
                        requirements.NodeProcessRequirements[transformation.NodeId][transformation.Interval] = new HashSet<string>();

                    // Добавляем требование трансформации "InputType->OutputType"
                    requirements.NodeProcessRequirements[transformation.NodeId][transformation.Interval]
                        .Add($"{transformation.InputType}->{transformation.OutputType}");
                }
                // === Требования к транспорту на связях ===
                else if (!string.IsNullOrEmpty(transformation.SourceNode))
                {
                    var linkKey = (transformation.SourceNode, transformation.TargetNode);

                    if (!requirements.LinkTransportRequirements.ContainsKey(linkKey))
                        requirements.LinkTransportRequirements[linkKey] = new Dictionary<TimeInterval, HashSet<string>>();

                    if (!requirements.LinkTransportRequirements[linkKey].ContainsKey(transformation.Interval))
                        requirements.LinkTransportRequirements[linkKey][transformation.Interval] = new HashSet<string>();

                    // Добавляем требование транспортировки типа данных
                    requirements.LinkTransportRequirements[linkKey][transformation.Interval]
                        .Add(transformation.TransportType);
                }
            }

            // === Требования к хранилищам на узлах ===
            // Для каждого узла и типа данных суммируем объёмы потоков
            foreach (var nodeFlow in plan.NodeFlowTypes)
            {
                var nodeId = nodeFlow.Key;
                var flowType = nodeFlow.Value;

                if (!requirements.NodeStorageRequirements.ContainsKey(nodeId))
                    requirements.NodeStorageRequirements[nodeId] = new Dictionary<TimeInterval, Dictionary<string, double>>();

                // Примечание: SplitInterval сейчас возвращает интервал без изменений
                // В будущем здесь может быть логика разбиения пересекающихся интервалов
                foreach (var interval in SplitInterval(plan.Path.Interval))
                {
                    if (!requirements.NodeStorageRequirements[nodeId].ContainsKey(interval))
                        requirements.NodeStorageRequirements[nodeId][interval] = new Dictionary<string, double>();

                    if (!requirements.NodeStorageRequirements[nodeId][interval].ContainsKey(flowType))
                        requirements.NodeStorageRequirements[nodeId][interval][flowType] = 0;

                    // Суммируем объёмы: несколько потоков могут требовать хранения одного типа данных
                    requirements.NodeStorageRequirements[nodeId][interval][flowType] += plan.Flow.Volume;
                }
            }
        }

        return requirements;
    }

    /// <summary>
    /// Синтезирует финальные конфигурации на основе агрегированных требований.
    /// </summary>
    /// <remarks>
    /// Для каждого интервала:
    /// 1. Берёт базовую конфигурацию как шаблон
    /// 2. Для каждого узла: выбирает процессы и настраивает ёмкости хранилищ
    /// 3. Для каждой связи: выбирает транспорты для требуемых типов данных
    /// 4. Возвращает новую конфигурацию с активированными ресурсами
    /// </remarks>
    private List<StructConfiguration> SynthesizeConfigurations(
        ResourceRequirements requirements,
        Dictionary<TimeInterval, StructConfiguration> intervalToBaseConfig,
        FunctionLibrary functionLibrary)
    {
        var synthesizedStructs = new List<StructConfiguration>();
        var processedIntervals = new HashSet<TimeInterval>();

        foreach (var kvp in intervalToBaseConfig)
        {
            var interval = kvp.Key;
            var baseConfig = kvp.Value;

            // Пропускаем уже обработанные интервалы (защита от дубликатов)
            if (processedIntervals.Contains(interval))
                continue;

            var synthesizedNodes = new List<NodeConfiguration>();
            var synthesizedLinks = new List<LinkConfiguration>();

            // Синтез конфигураций для всех узлов базовой конфигурации
            foreach (var baseNode in baseConfig.Nodes)
            {
                var synthesizedNode = SynthesizeNodeConfiguration(
                    baseNode,
                    requirements,
                    functionLibrary,
                    interval
                );
                synthesizedNodes.Add(synthesizedNode);
            }

            // Синтез конфигураций для всех связей базовой конфигурации
            foreach (var baseLink in baseConfig.Links)
            {
                var synthesizedLink = SynthesizeLinkConfiguration(
                    baseLink,
                    requirements,
                    functionLibrary,
                    interval
                );
                synthesizedLinks.Add(synthesizedLink);
            }

            var synthesizedStruct = new StructConfiguration(
                interval,
                synthesizedNodes,
                synthesizedLinks
            );

            synthesizedStructs.Add(synthesizedStruct);
            processedIntervals.Add(interval);
        }

        // Сортировка по времени для согласованного порядка возврата
        synthesizedStructs.Sort((a, b) => a.Interval.Start.CompareTo(b.Interval.Start));
        return synthesizedStructs;
    }

    /// <summary>
    /// Синтезирует конфигурацию отдельного узла.
    /// </summary>
    /// <remarks>
    /// Логика выбора ресурсов:
    /// • Процессы: ищутся в functionLibrary по сигнатуре трансформации (Input->Output)
    ///            и фильтруются по baseNode.EnabledProcesses
    /// • Хранилища: ищутся по поддержке типа данных (AllowedFlowTypes),
    ///              ёмкость устанавливается как MAX(текущая, требуемая)
    /// </remarks>
    private NodeConfiguration SynthesizeNodeConfiguration(
        NodeConfiguration baseNode,
        ResourceRequirements requirements,
        FunctionLibrary functionLibrary,
        TimeInterval interval)
    {
        var processes = new List<string>();
        // Копируем базовые ёмкости хранилищ для последующего обновления
        var storageCapacities = new Dictionary<string, double>(baseNode.StorageCapacities);

        // === Обработка требований к процессам ===
        if (requirements.NodeProcessRequirements.TryGetValue(baseNode.NodeId, out var intervalProcesses) &&
            intervalProcesses.TryGetValue(interval, out var requiredTransformations))
        {
            foreach (var transformation in requiredTransformations)
            {
                var parts = transformation.Split("->");
                if (parts.Length == 2)
                {
                    var inputType = parts[0];
                    var outputType = parts[1];

                    // Ищем процесс в библиотеке, соответствующий трансформации и разрешённый в базовой конфигурации
                    var matchingProcess = functionLibrary.Processes
                        .FirstOrDefault(p =>
                            p.InputFlowType == inputType &&
                            p.OutputFlowType == outputType &&
                            baseNode.EnabledProcesses.Contains(p.Id));

                    if (matchingProcess != null && !processes.Contains(matchingProcess.Id))
                        processes.Add(matchingProcess.Id);
                }
            }
        }

        // === Обработка требований к хранилищам ===
        if (requirements.NodeStorageRequirements.TryGetValue(baseNode.NodeId, out var intervalStorages) &&
            intervalStorages.TryGetValue(interval, out var requiredStorages))
        {
            foreach (var requiredStorage in requiredStorages)
            {
                var flowType = requiredStorage.Key;
                var requiredVolume = requiredStorage.Value;

                // Ищем хранилище, поддерживающее данный тип данных
                var matchingStorage = functionLibrary.Storages
                    .FirstOrDefault(s => s.AllowedFlowTypes.Contains(flowType));

                if (matchingStorage != null)
                {
                    // Обновляем ёмкость: берём максимум из текущей и требуемой
                    if (storageCapacities.ContainsKey(matchingStorage.Id))
                    {
                        storageCapacities[matchingStorage.Id] = Math.Max(
                            storageCapacities[matchingStorage.Id],
                            requiredVolume
                        );
                    }
                    else
                    {
                        storageCapacities[matchingStorage.Id] = requiredVolume;
                    }
                }
            }
        }

        return new NodeConfiguration(
            baseNode.NodeId,
            baseNode.EnabledProcesses,      // все разрешённые процессы (базовый набор)
            baseNode.InputsVolumes.ToDictionary(),
            baseNode.Outputs,
            storageCapacities,              // обновлённые ёмкости хранилищ
            processes);                     // активированные процессы (только необходимые)
    }

    /// <summary>
    /// Синтезирует конфигурацию отдельной связи.
    /// </summary>
    /// <remarks>
    /// Логика выбора транспортов:
    /// • Для каждого требуемого типа данных ищется транспорт в functionLibrary
    /// • Транспорт должен поддерживать тип данных (FlowType) и быть разрешённым (EnabledTransports)
    /// • В результат попадают только ID необходимых транспортов (ActiveTransports)
    /// </remarks>
    private LinkConfiguration SynthesizeLinkConfiguration(
        LinkConfiguration baseLink,
        ResourceRequirements requirements,
        FunctionLibrary functionLibrary,
        TimeInterval interval)
    {
        var transports = new List<string>();
        var linkKey = (baseLink.NodeA, baseLink.NodeB);

        if (requirements.LinkTransportRequirements.TryGetValue(linkKey, out var intervalTransports) &&
            intervalTransports.TryGetValue(interval, out var requiredFlowTypes))
        {
            foreach (var flowType in requiredFlowTypes)
            {
                // Ищем транспорт, поддерживающий тип данных и разрешённый в базовой конфигурации
                var matchingTransport = functionLibrary.Transports
                    .FirstOrDefault(t =>
                        t.FlowType == flowType &&
                        baseLink.EnabledTransports.Contains(t.Id));

                if (matchingTransport != null && !transports.Contains(matchingTransport.Id))
                    transports.Add(matchingTransport.Id);
            }
        }

        return new LinkConfiguration(
            baseLink.NodeA,
            baseLink.NodeB,
            baseLink.EnabledTransports,  // все разрешённые транспорты (базовый набор)
            transports);                 // активированные транспорты (только необходимые)
    }

    /// <summary>
    /// Внутренний класс: шаг трансформации/транспортировки в плане маршрута.
    /// Использует discriminated union-подобный паттерн:
    /// • NodeId + InputType + OutputType — трансформация на узле
    /// • SourceNode + TargetNode + TransportType — транспортировка по связи
    /// </summary>
    private sealed class FlowTransformationStep
    {
        public string? NodeId { get; init; }
        public string? InputType { get; init; }
        public string? OutputType { get; init; }
        public string? SourceNode { get; init; }
        public string? TargetNode { get; init; }
        public string? TransportType { get; init; }
        public required TimeInterval Interval { get; init; }
        public required double FlowVolume { get; init; }
    }

    /// <summary>
    /// Внутренний класс: план маршрута для одного потока.
    /// </summary>
    private sealed class FlowRoutePlan
    {
        public required DataFlow Flow { get; init; }
        public required ReachabilityPath Path { get; init; }
        public required IReadOnlyList<FlowTransformationStep> Transformations { get; init; }
        public required IReadOnlyDictionary<string, string> NodeFlowTypes { get; init; }
    }

    /// <summary>
    /// Внутренний класс: агрегированные требования к ресурсам.
    /// </summary>
    private sealed class ResourceRequirements
    {
        /// <summary>
        /// Требования к процессам: NodeId -> Interval -> Набор трансформаций "A->B"
        /// </summary>
        public Dictionary<string, Dictionary<TimeInterval, HashSet<string>>> NodeProcessRequirements { get; init; } = new();

        /// <summary>
        /// Требования к транспорту: (NodeA, NodeB) -> Interval -> Набор типов данных
        /// </summary>
        public Dictionary<(string, string), Dictionary<TimeInterval, HashSet<string>>> LinkTransportRequirements { get; init; } = new();

        /// <summary>
        /// Требования к хранилищам: NodeId -> Interval -> (FlowType -> Объём)
        /// </summary>
        public Dictionary<string, Dictionary<TimeInterval, Dictionary<string, double>>> NodeStorageRequirements { get; init; } = new();
    }

    /// <summary>
    /// Разбивает интервал на подынтервалы для более точной агрегации требований.
    /// </summary>
    /// <remarks>
    /// Текущая реализация возвращает интервал без изменений.
    /// TODO: Реализовать алгоритм разбиения пересекающихся интервалов
    /// для повышения точности учёта ресурсов в динамических сценариях.
    /// </remarks>
    private List<TimeInterval> SplitInterval(TimeInterval interval)
    {
        return new List<TimeInterval> { interval };
    }
}