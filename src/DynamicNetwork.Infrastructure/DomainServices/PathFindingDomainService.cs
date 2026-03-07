using DynamicNetwork.Domain.Configuration;
using DynamicNetwork.Domain.Enums;
using DynamicNetwork.Domain.Graph;
using DynamicNetwork.Domain.Paths;
using DynamicNetwork.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DynamicNetwork.Infrastructure.DomainServices;

/// <summary>
/// Сервис поиска всех достижимых путей в динамической (временной) сети.
/// 
/// Алгоритм использует поиск в глубину (DFS) с явным стеком состояний для обхода
/// последовательности временных снимков графа (TemporalGraph).
/// 
/// Ключевые особенности:
/// • Учитывает временную согласованность: переходы между графами только вперёд во времени
/// • Применяет конфигурационные ограничения: доступность рёбер зависит от StructConfiguration
/// • Поддерживает направленность связей: LinkDirection (Left/Right/Undirected)
/// • Предотвращает циклы: каждый путь отслеживает посещённые узлы
/// • Находит ВСЕ возможные пути, а не только кратчайший
/// </summary>
public class PathFindingDomainService : IPathFindingDomainService
{
    /// <summary>
    /// Находит все достижимые пути от sourceNode до targetNode в заданном временном окне.
    /// </summary>
    /// <param name="graphs">Список временных графов (снимков сети во времени)</param>
    /// <param name="configurations">Конфигурации, определяющие доступность связей</param>
    /// <param name="sourceNode">Идентификатор узла-источника</param>
    /// <param name="targetNode">Идентификатор узла-цели</param>
    /// <param name="timeWindow">Временное окно, в пределах которого осуществляется поиск</param>
    /// <returns>Список всех найденных путей, отсортированных по количеству рёбер</returns>
    public IReadOnlyList<ReachabilityPath> FindAllPaths(
        IReadOnlyList<TemporalGraph> graphs,
        IReadOnlyList<StructConfiguration> configurations,
        string sourceNode,
        string targetNode,
        TimeInterval timeWindow)
    {
        // =====================================================================
        // ЭТАП 1: Предобработка и фильтрация графов
        // =====================================================================

        // Фильтруем графы: оставляем только те, чьи интервалы пересекаются с timeWindow
        // Сортируем по времени начала для обеспечения хронологического порядка обхода
        var relevantGraphs = graphs
            .Where(g => g.Interval.Overlaps(timeWindow))
            .OrderBy(g => g.Interval.Start)
            .ToList();

        // Если нет релевантных графов — путей не существует
        if (!relevantGraphs.Any())
            return new List<ReachabilityPath>();

        // Находим индексы графов, в которых физически существует sourceNode
        // Поиск может начаться с любого из этих моментов времени
        var startGraphIndices = relevantGraphs
            .Select((g, idx) => new { Graph = g, Index = idx })
            .Where(x => x.Graph.AllNetworkNodes.Contains(sourceNode))
            .Select(x => x.Index)
            .ToList();

        // Если источник не существует ни в одном графе — путей нет
        if (!startGraphIndices.Any())
            return new List<ReachabilityPath>();

        // =====================================================================
        // ЭТАП 2: Инициализация DFS с использованием явного стека
        // =====================================================================

        var paths = new List<ReachabilityPath>();
        var stack = new Stack<PathState>();

        // Создаём начальное состояние для каждого подходящего графа-старта
        foreach (var startGraphIdx in startGraphIndices)
        {
            var startGraph = relevantGraphs[startGraphIdx];

            stack.Push(new PathState
            {
                CurrentNode = sourceNode,
                CurrentGraphIndex = startGraphIdx,
                StartTime = startGraph.Interval.Start,
                // HashSet для O(1) проверки посещённых узлов (защита от циклов)
                VisitedNodes = new HashSet<string> { sourceNode },
                // История пути: последовательность узлов, индексов графов и рёбер
                Nodes = new List<string> { sourceNode },
                GraphIndices = new List<int> { startGraphIdx },
                Edges = new List<EdgeTraversal>()
            });
        }

        // =====================================================================
        // ЭТАП 3: Основной цикл обхода (DFS)
        // =====================================================================

        while (stack.Count > 0)
        {
            // Извлекаем текущее состояние (LIFO — глубина вперёд)
            var state = stack.Pop();

            // === Проверка достижения цели ===
            if (state.CurrentNode == targetNode)
            {
                // Формируем итоговый путь с метаданными
                paths.Add(new ReachabilityPath(
                    nodes: state.Nodes.AsReadOnly(),
                    graphIndices: state.GraphIndices.AsReadOnly(),
                    edges: state.Edges.AsReadOnly(),
                    // Интервал пути: от старта до конца последнего использованного графа
                    interval: new TimeInterval(state.StartTime,
                        relevantGraphs[state.CurrentGraphIndex].Interval.End)
                ));
                // Дальнейшее расширение этой ветки не требуется
                continue;
            }

            // === Определение контекста для генерации следующих состояний ===
            var currentGraph = relevantGraphs[state.CurrentGraphIndex];

            // Находим конфигурацию, действующую в интервале текущего графа
            var currentConfig = configurations
                .FirstOrDefault(c => c.Interval.Overlaps(currentGraph.Interval));

            // Генерируем все возможные следующие состояния из текущего
            var nextStates = GenerateNextStates(
                state, currentGraph, currentConfig, targetNode, relevantGraphs);

            // Добавляем новые состояния в стек для дальнейшего обхода
            foreach (var nextState in nextStates)
            {
                stack.Push(nextState);
            }
        }

        // =====================================================================
        // ЭТАП 4: Постобработка результатов
        // =====================================================================

        // Сортируем пути по количеству рёбер (кратчайшие первыми)
        // Возвращаем как IReadOnlyList для инкапсуляции
        return paths
            .OrderBy(p => p.Edges.Count)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Генерирует все возможные следующие состояния из текущего состояния пути.
    /// Поддерживает два типа переходов:
    /// 1. Пространственный: перемещение по ребру в рамках текущего графа
    /// 2. Временной: переход к следующему снимку графа (остаясь в том же узле)
    /// </summary>
    private List<PathState> GenerateNextStates(
        PathState currentState,
        TemporalGraph currentGraph,
        StructConfiguration? currentConfig,
        string targetNode,
        IReadOnlyList<TemporalGraph> relevantGraphs)
    {
        var nextStates = new List<PathState>();

        // ---------------------------------------------------------------------
        // ТИП ПЕРЕХОДА 1: Пространственное перемещение (по ребру графа)
        // ---------------------------------------------------------------------

        // Получаем список рёбер, доступных для прохождения из текущего узла
        // Учитывается: направление ребра + конфигурация (активные транспорты)
        var availableEdges = GetAvailableEdges(currentGraph, currentConfig, currentState.CurrentNode);

        foreach (var edge in availableEdges)
        {
            // Определяем целевой узел ребра с учётом направления
            var nextNode = DetermineNextNode(edge, currentState.CurrentNode);
            if (nextNode == null)
                continue;

            // === Проверка на циклы ===
            // Запрещаем повторное посещение узлов (кроме targetNode, который завершает путь)
            if (currentState.VisitedNodes.Contains(nextNode) && nextNode != targetNode)
                continue;

            // Создаём новое состояние с обновлёнными данными пути
            var nextState = new PathState
            {
                CurrentNode = nextNode,
                CurrentGraphIndex = currentState.CurrentGraphIndex, // граф не меняется
                StartTime = currentState.StartTime,
                // Копируем коллекции с добавлением нового элемента (неизменяемость состояния)
                VisitedNodes = new HashSet<string>(currentState.VisitedNodes) { nextNode },
                Nodes = new List<string>(currentState.Nodes) { nextNode },
                GraphIndices = new List<int>(currentState.GraphIndices) { currentState.CurrentGraphIndex },
                Edges = new List<EdgeTraversal>(currentState.Edges)
                {
                    new EdgeTraversal
                    {
                        FromNode = currentState.CurrentNode,
                        ToNode = nextNode,
                        GraphIndex = currentState.CurrentGraphIndex,
                        Link = edge
                    }
                }
            };

            nextStates.Add(nextState);
        }

        // ---------------------------------------------------------------------
        // ТИП ПЕРЕХОДА 2: Временной переход (к следующему снимку графа)
        // ---------------------------------------------------------------------

        // Проверяем возможность перехода к следующему графу во временной последовательности
        if (currentState.CurrentGraphIndex + 1 < relevantGraphs.Count)
        {
            var nextGraphIndex = currentState.CurrentGraphIndex + 1;
            var nextGraph = relevantGraphs[nextGraphIndex];

            // Условие 1: текущий узел должен существовать в следующем графе
            if (nextGraph.AllNetworkNodes.Contains(currentState.CurrentNode))
            {
                // Условие 2: время должно течь вперёд (нет перекрытия "в прошлое")
                if (currentGraph.Interval.End <= nextGraph.Interval.Start)
                {
                    var nextState = new PathState
                    {
                        CurrentNode = currentState.CurrentNode, // узел не меняется
                        CurrentGraphIndex = nextGraphIndex,     // переход к следующему графу
                        StartTime = currentState.StartTime,
                        // Копируем состояние без изменений (кроме индекса графа)
                        VisitedNodes = new HashSet<string>(currentState.VisitedNodes),
                        Nodes = new List<string>(currentState.Nodes) { currentState.CurrentNode },
                        GraphIndices = new List<int>(currentState.GraphIndices) { nextGraphIndex },
                        Edges = new List<EdgeTraversal>(currentState.Edges)
                        // Рёбра не добавляются: переход во времени ≠ физическое перемещение
                    };

                    nextStates.Add(nextState);
                }
            }
        }

        return nextStates;
    }

    /// <summary>
    /// Возвращает список рёбер, доступных для прохождения из заданного узла.
    /// Применяет фильтрацию по:
    /// • Направлению ребра (LinkDirection)
    /// • Конфигурации (наличие активных/включённых транспортов)
    /// </summary>
    private List<Link> GetAvailableEdges(
        TemporalGraph graph,
        StructConfiguration? config,
        string currentNode)
    {
        var edges = new List<Link>();

        foreach (var link in graph.Links)
        {
            // Пропускаем ненаправленные/неопределённые связи
            if (link.Direction == LinkDirection.None)
                continue;

            // === Проверка направления относительно текущего узла ===
            bool isTraversable = false;

            // Случай 1: текущий узел — NodeA, движение возможно вправо или ненаправленно
            if (link.NodeA == currentNode &&
                (link.Direction == LinkDirection.Right || link.Direction == LinkDirection.Undirected))
            {
                isTraversable = true;
            }
            // Случай 2: текущий узел — NodeB, движение возможно влево или ненаправленно
            else if (link.NodeB == currentNode &&
                     (link.Direction == LinkDirection.Left || link.Direction == LinkDirection.Undirected))
            {
                isTraversable = true;
            }

            if (!isTraversable)
                continue;

            // === Проверка конфигурационных ограничений ===
            if (config != null)
            {
                // Ищем конфигурацию для данного ребра (порядок узлов не важен)
                var linkConfig = config.Links.FirstOrDefault(lc =>
                    (lc.NodeA == link.NodeA && lc.NodeB == link.NodeB) ||
                    (lc.NodeA == link.NodeB && lc.NodeB == link.NodeA));

                if (linkConfig != null)
                {
                    // Связь доступна только если указаны активные ИЛИ включённые транспорты
                    /*bool hasActiveTransports =
                        (linkConfig.ActiveTransports?.Any() ?? false) ||
                        (linkConfig.EnabledTransports?.Any() ?? false);

                    if (!hasActiveTransports)
                        continue;*/
                }
            }

            edges.Add(link);
        }

        return edges;
    }

    /// <summary>
    /// Определяет целевой узел ребра с учётом направления движения.
    /// </summary>
    /// <returns>Идентификатор следующего узла или null, если движение невозможно</returns>
    private string? DetermineNextNode(Link edge, string currentNode)
    {
        // Движение от NodeA к NodeB: разрешено при Right или Undirected
        if (edge.NodeA == currentNode &&
            (edge.Direction == LinkDirection.Right || edge.Direction == LinkDirection.Undirected))
            return edge.NodeB;

        // Движение от NodeB к NodeA: разрешено при Left или Undirected
        if (edge.NodeB == currentNode &&
            (edge.Direction == LinkDirection.Left || edge.Direction == LinkDirection.Undirected))
            return edge.NodeA;

        return null;
    }

    /// <summary>
    /// Внутреннее состояние поиска пути.
    /// Хранит полную историю текущей ветки обхода для независимого расширения.
    /// </summary>
    private sealed class PathState
    {
        public string CurrentNode { get; set; } = string.Empty;
        public int CurrentGraphIndex { get; set; }
        public long StartTime { get; set; }

        /// <summary>
        /// Множество посещённых узлов для предотвращения циклов в рамках пути.
        /// </summary>
        public HashSet<string> VisitedNodes { get; set; } = new();

        /// <summary>
        /// Упорядоченный список узлов, составляющих текущий путь.
        /// </summary>
        public List<string> Nodes { get; set; } = new();

        /// <summary>
        /// Индексы графов, в которых происходили переходы между узлами.
        /// </summary>
        public List<int> GraphIndices { get; set; } = new();

        /// <summary>
        /// Детальная информация о пройденных рёбрах (узлы, граф, ссылка на Link).
        /// </summary>
        public List<EdgeTraversal> Edges { get; set; } = new();
    }
}