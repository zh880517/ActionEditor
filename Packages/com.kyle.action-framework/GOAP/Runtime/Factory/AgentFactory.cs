using System;
using System.Collections.Generic;
using System.Reflection;

namespace GOAP
{
    public static class AgentFactory
    {
        private static readonly Dictionary<Type, Func<SerializedActionData, IAction>> _builders
            = new Dictionary<Type, Func<SerializedActionData, IAction>>();

        public static Agent Create(GOAPRuntimeData data)
        {
            var agent = new Agent();

            foreach (var actionData in data.Actions)
            {
                var action = BuildAction(actionData);
                if (action != null)
                    agent.Actions.Add(action);
            }

            foreach (var goalData in data.Goals)
                agent.Goals.Add(new BasicGoal(goalData));

            return agent;
        }

        private static IAction BuildAction(SerializedActionData data)
        {
            var type = data.GetType();

            if (!_builders.TryGetValue(type, out var builder))
            {
                builder = CreateBuilder(type);
                _builders[type] = builder;
            }

            return builder?.Invoke(data);
        }

        private static Func<SerializedActionData, IAction> CreateBuilder(Type serializedType)
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

            return (SerializedActionData raw) =>
            {
                var typedData = dataField.GetValue(raw);
                return (IAction)ctor.Invoke(new object[] { raw, typedData });
            };
        }
    }
}
