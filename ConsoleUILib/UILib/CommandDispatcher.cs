using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleUILib.UILib
{
    /// <summary>
    /// 通用命令分发器。注册命令名 → 处理函数，解析输入并分发。
    /// </summary>
    public class CommandDispatcher
    {
        private readonly Dictionary<string, Func<string[], bool>> _handlers
            = new(StringComparer.OrdinalIgnoreCase);
        public char[] Separators { get; set; } = { ' ' };
        public bool ThrowOnUnknownCommand { get; set; } = false;
        public event Action<string> OnUnknownCommand;
        public void Register(string commandName, Func<string[], bool> handler)
        {
            if (string.IsNullOrWhiteSpace(commandName)) throw new ArgumentNullException(nameof(commandName));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            _handlers[commandName.Trim()] = handler;
        }
        public bool Unregister(string commandName) => _handlers.Remove(commandName?.Trim() ?? "");
        public bool Dispatch(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            string[] parts = input.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return false;
            if (_handlers.TryGetValue(parts[0], out var handler))
                return handler(parts.Length > 1 ? parts[1..] : Array.Empty<string>());
            OnUnknownCommand?.Invoke(parts[0]);
            if (ThrowOnUnknownCommand) throw new InvalidOperationException($"Unknown command: {parts[0]}");
            return false;
        }
        public IEnumerable<string> GetRegisteredCommands() => _handlers.Keys;
    }
}
