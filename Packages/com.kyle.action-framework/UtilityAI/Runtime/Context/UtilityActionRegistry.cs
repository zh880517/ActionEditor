using System;
using System.Collections.Generic;

namespace UtilityAI
{
    /// <summary>
    /// UtilityAI 动作处理器注册表，业务层在创建 Runtime 前用它绑定配置类型和无状态 Handler。
    /// </summary>
    public sealed class UtilityActionRegistry<TContext>
    {
        private readonly Dictionary<Type, ITerminalActionInvoker<TContext>> _terminalHandlers =
            new Dictionary<Type, ITerminalActionInvoker<TContext>>();

        private readonly Dictionary<Type, ISupportActionInvoker<TContext>> _supportHandlers =
            new Dictionary<Type, ISupportActionInvoker<TContext>>();

        public void Register<TConfig>(TerminalActionHandler<TContext, TConfig> handler)
            where TConfig : TerminalActionConfig
        {
            if (handler == null)
                throw new ArgumentNullException("handler");

            var configType = typeof(TConfig);
            if (_terminalHandlers.ContainsKey(configType))
                throw new InvalidOperationException("Terminal handler already registered.");

            _terminalHandlers.Add(configType, new TerminalActionInvoker<TContext, TConfig>(handler));
        }

        public void Register<TConfig>(SupportActionHandler<TContext, TConfig> handler)
            where TConfig : SupportActionConfig
        {
            if (handler == null)
                throw new ArgumentNullException("handler");

            var configType = typeof(TConfig);
            if (_supportHandlers.ContainsKey(configType))
                throw new InvalidOperationException("Support handler already registered.");

            _supportHandlers.Add(configType, new SupportActionInvoker<TContext, TConfig>(handler));
        }

        internal bool TryGetTerminalHandler(Type configType, out ITerminalActionInvoker<TContext> handler)
        {
            return _terminalHandlers.TryGetValue(configType, out handler);
        }

        internal bool TryGetSupportHandler(Type configType, out ISupportActionInvoker<TContext> handler)
        {
            return _supportHandlers.TryGetValue(configType, out handler);
        }
    }
}
