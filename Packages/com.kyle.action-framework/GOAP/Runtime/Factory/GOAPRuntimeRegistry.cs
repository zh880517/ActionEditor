using System;
using System.Collections.Generic;

namespace GOAP
{
    // GOAP 运行时注册表，由 Editor 生成代码填充。
    // 运行时工厂优先通过这里的静态委托构建 Action，避免在常规路径中使用反射。
    public static class GOAPRuntimeRegistry
    {
        private static readonly Dictionary<Type, ActionRegistration> _actions
            = new Dictionary<Type, ActionRegistration>();

        public static void RegisterAction<T>(
            Type serializedType,
            TActionRunner<T> runner)
            where T : struct, IActionData
        {
            if (serializedType == null)
                throw new ArgumentNullException(nameof(serializedType));
            if (!typeof(SerializedActionData).IsAssignableFrom(serializedType))
                throw new ArgumentException("serializedType must inherit SerializedActionData.", nameof(serializedType));

            if (runner != null)
                ActionRunner<T>.Runner = runner;

            _actions[serializedType] = new ActionRegistration(
                typeof(T),
                raw =>
                {
                    var typed = raw as TSerializedActionData<T>;
                    if (typed == null)
                        return null;
                    return new RuntimeAction<T>(typed, typed.Data);
                },
                () => ActionRunner<T>.Runner != null,
                () => ActionRunner<T>.Runner = null);
        }

        public static bool TryBuildAction(
            SerializedActionData data,
            bool requireRunner,
            out IAction action,
            out string diagnostic)
        {
            action = null;
            diagnostic = null;

            if (data == null)
                return false;

            if (!_actions.TryGetValue(data.GetType(), out var registration))
                return false;

            if (requireRunner && !registration.HasRunner())
            {
                diagnostic = $"Action '{data.Id}' has no registered runner for '{registration.DataType.Name}'.";
                return true;
            }

            action = registration.Build(data);
            if (action == null)
                diagnostic = $"Action '{data.Id}' cannot be converted to registered data type '{registration.DataType.Name}'.";
            return true;
        }

        public static void Clear()
        {
            foreach (var registration in _actions.Values)
                registration.ClearRunner();
            _actions.Clear();
        }

        private sealed class ActionRegistration
        {
            private readonly Func<SerializedActionData, IAction> _build;
            private readonly Func<bool> _hasRunner;
            private readonly Action _clearRunner;

            public ActionRegistration(
                Type dataType,
                Func<SerializedActionData, IAction> build,
                Func<bool> hasRunner,
                Action clearRunner)
            {
                DataType = dataType;
                _build = build;
                _hasRunner = hasRunner;
                _clearRunner = clearRunner;
            }

            public Type DataType { get; }

            public IAction Build(SerializedActionData data) => _build(data);

            public bool HasRunner() => _hasRunner();

            public void ClearRunner() => _clearRunner();
        }
    }
}
