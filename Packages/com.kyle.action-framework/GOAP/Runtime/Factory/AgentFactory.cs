using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Reflection;

namespace GOAP
{
    public sealed class AgentFactoryOptions
    {
        public bool RequireActionRunner { get; set; } = true;
        public bool ThrowOnError { get; set; }

        public static AgentFactoryOptions Default => new AgentFactoryOptions();
        public static AgentFactoryOptions Compatibility => new AgentFactoryOptions { RequireActionRunner = false };
        public static AgentFactoryOptions Strict => new AgentFactoryOptions { ThrowOnError = true };
    }

    public sealed class AgentFactoryResult
    {
        private readonly List<string> _diagnostics = new List<string>();

        internal AgentFactoryResult(Agent agent)
        {
            Agent = agent;
        }

        public Agent Agent { get; }
        public IReadOnlyList<string> Diagnostics => new ReadOnlyCollection<string>(_diagnostics);
        public bool Success => _diagnostics.Count == 0;

        internal void AddDiagnostic(string message)
        {
            if (!string.IsNullOrEmpty(message))
                _diagnostics.Add(message);
        }
    }

    public static class AgentFactory
    {
        private static readonly Dictionary<Type, ActionBuilder> _builders
            = new Dictionary<Type, ActionBuilder>();

        public static Agent Create(GOAPRuntimeData data)
            => Create(data, AgentFactoryOptions.Compatibility);

        public static Agent Create(GOAPRuntimeData data, AgentFactoryOptions options)
        {
            var result = CreateWithDiagnostics(data, options);
            if ((options == null ? AgentFactoryOptions.Default : options).ThrowOnError && !result.Success)
                throw new InvalidOperationException(string.Join(Environment.NewLine, result.Diagnostics));
            return result.Agent;
        }

        public static AgentFactoryResult CreateWithDiagnostics(
            GOAPRuntimeData data,
            AgentFactoryOptions options = null)
        {
            var agent = new Agent();
            var result = new AgentFactoryResult(agent);
            options = options ?? AgentFactoryOptions.Default;

            if (data == null)
            {
                result.AddDiagnostic("GOAP runtime data is null.");
                return result;
            }

            var actionIds = new HashSet<string>();
            var goalIds = new HashSet<string>();

            if (data.Actions == null)
            {
                result.AddDiagnostic("Action list is null.");
            }
            else foreach (var actionData in data.Actions)
            {
                if (!ValidateActionData(actionData, actionIds, result))
                    continue;

                var action = BuildAction(actionData, options, result);
                if (action != null)
                    agent.Actions.Add(action);
            }

            if (data.Goals == null)
            {
                result.AddDiagnostic("Goal list is null.");
            }
            else foreach (var goalData in data.Goals)
            {
                if (!ValidateGoalData(goalData, goalIds, result))
                    continue;

                agent.Goals.Add(new BasicGoal(goalData));
            }

            return result;
        }

        public static IReadOnlyList<string> Validate(
            GOAPRuntimeData data,
            AgentFactoryOptions options = null)
        {
            return CreateWithDiagnostics(data, options).Diagnostics;
        }

        private static IAction BuildAction(
            SerializedActionData data,
            AgentFactoryOptions options,
            AgentFactoryResult result)
        {
            var type = data.GetType();

            if (!_builders.TryGetValue(type, out var actionBuilder))
            {
                actionBuilder = CreateBuilder(type);
                _builders[type] = actionBuilder;
            }

            if (actionBuilder == null)
            {
                result.AddDiagnostic($"Action '{data.Id}' uses unsupported data type '{type.Name}'.");
                return null;
            }

            if (options.RequireActionRunner && !actionBuilder.HasRunner())
            {
                result.AddDiagnostic($"Action '{data.Id}' has no registered runner for '{actionBuilder.DataType.Name}'.");
                return null;
            }

            return actionBuilder.Build(data);
        }

        private static ActionBuilder CreateBuilder(Type serializedType)
        {
            if (!serializedType.IsGenericType
                || serializedType.GetGenericTypeDefinition() != typeof(TSerializedActionData<>))
                return null;

            var dataType = serializedType.GetGenericArguments()[0];
            var runtimeActionType = typeof(RuntimeAction<>).MakeGenericType(dataType);
            var ctor = runtimeActionType.GetConstructor(new[] { typeof(SerializedActionData), dataType });
            var dataField = serializedType.GetField("Data", BindingFlags.Public | BindingFlags.Instance);

            if (ctor == null || dataField == null)
                return null;

            var runnerProperty = typeof(ActionRunner<>)
                .MakeGenericType(dataType)
                .GetProperty("Runner", BindingFlags.Public | BindingFlags.Static);

            return new ActionBuilder(
                dataType,
                (SerializedActionData raw) =>
                {
                    var typedData = dataField.GetValue(raw);
                    return (IAction)ctor.Invoke(new object[] { raw, typedData });
                },
                () => runnerProperty != null && runnerProperty.GetValue(null) != null);
        }

        private static bool ValidateActionData(
            SerializedActionData data,
            HashSet<string> actionIds,
            AgentFactoryResult result)
        {
            if (data == null)
            {
                result.AddDiagnostic("Action data is null.");
                return false;
            }

            bool valid = true;
            if (string.IsNullOrEmpty(data.Id))
            {
                result.AddDiagnostic("Action id is empty.");
                valid = false;
            }
            else if (!actionIds.Add(data.Id))
            {
                result.AddDiagnostic($"Duplicate action id '{data.Id}'.");
                valid = false;
            }

            if (data.Cost < 0f)
            {
                result.AddDiagnostic($"Action '{data.Id}' has negative cost.");
                valid = false;
            }

            if (data.Effects == null || data.Effects.Count == 0)
            {
                result.AddDiagnostic($"Action '{data.Id}' has no effects.");
                valid = false;
            }

            if (!ValidateEntryCount(data.Preconditions, $"Action '{data.Id}' preconditions", result))
                valid = false;
            if (!ValidateEntryCount(data.Effects, $"Action '{data.Id}' effects", result))
                valid = false;

            return valid;
        }

        private static bool ValidateGoalData(
            GoalRuntimeData data,
            HashSet<string> goalIds,
            AgentFactoryResult result)
        {
            if (data == null)
            {
                result.AddDiagnostic("Goal data is null.");
                return false;
            }

            bool valid = true;
            if (string.IsNullOrEmpty(data.Id))
            {
                result.AddDiagnostic("Goal id is empty.");
                valid = false;
            }
            else if (!goalIds.Add(data.Id))
            {
                result.AddDiagnostic($"Duplicate goal id '{data.Id}'.");
                valid = false;
            }

            if (data.DesiredState == null || data.DesiredState.Count == 0)
            {
                result.AddDiagnostic($"Goal '{data.Id}' has no desired state.");
                valid = false;
            }

            if (!ValidateEntryCount(data.DesiredState, $"Goal '{data.Id}' desired state", result))
                valid = false;

            return valid;
        }

        private static bool ValidateEntryCount(
            List<WorldStateEntry> entries,
            string label,
            AgentFactoryResult result)
        {
            if (entries == null)
                return true;

            if (entries.Count <= WorldState.MaxKeys)
                return true;

            result.AddDiagnostic($"{label} exceeds WorldState capacity ({WorldState.MaxKeys}).");
            return false;
        }

        private sealed class ActionBuilder
        {
            private readonly Func<SerializedActionData, IAction> _build;
            private readonly Func<bool> _hasRunner;

            public ActionBuilder(
                Type dataType,
                Func<SerializedActionData, IAction> build,
                Func<bool> hasRunner)
            {
                DataType = dataType;
                _build = build;
                _hasRunner = hasRunner;
            }

            public Type DataType { get; }

            public IAction Build(SerializedActionData data) => _build(data);

            public bool HasRunner() => _hasRunner();
        }
    }
}
