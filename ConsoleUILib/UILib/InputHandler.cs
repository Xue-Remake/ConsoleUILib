using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleUILib.UILib
{
    /// <summary>
    /// 通用控制台输入处理器。
    /// 后台监听键盘，通过事件通知。
    /// </summary>
    public class InputHandler : IDisposable
    {
        private readonly StringBuilder _buf = new();
        private CancellationTokenSource _cts;
        private Task _task;
        private bool _running;
        public string Prompt { get; set; } = "> ";
        public string CurrentInput => _buf.ToString();
        public bool IsRunning => _running;
        public int PollingIntervalMs { get; set; } = 30;
        /// <summary>每次按键触发，参数为当前输入字符串</summary>
        public event Action<string> OnInputChanged;
        /// <summary>回车提交触发</summary>
        public event Action<string> OnCommandSubmitted;
        /// <summary>Esc 取消触发</summary>
        public event Action OnInputCancelled;
        public void Start()
        {
            if (_running) return;
            _running = true;
            _cts = new CancellationTokenSource();
            _buf.Clear();
            _task = Task.Run(() => Loop(_cts.Token));
        }
        public void Stop()
        {
            if (!_running) return;
            _cts?.Cancel();
            _task?.Wait(TimeSpan.FromSeconds(1));
            _running = false;
        }
        public void Clear()
        {
            _buf.Clear();
            OnInputChanged?.Invoke("");
        }
        public void Dispose() { Stop(); _cts?.Dispose(); }
        private void Loop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                while (Console.KeyAvailable && !token.IsCancellationRequested)
                {
                    var k = Console.ReadKey(true);
                    switch (k.Key)
                    {
                        case ConsoleKey.Enter:
                            string cmd = _buf.ToString();
                            _buf.Clear();
                            OnInputChanged?.Invoke("");
                            if (!string.IsNullOrWhiteSpace(cmd))
                                OnCommandSubmitted?.Invoke(cmd.Trim());
                            break;
                        case ConsoleKey.Backspace:
                            if (_buf.Length > 0) { _buf.Remove(_buf.Length - 1, 1); OnInputChanged?.Invoke(_buf.ToString()); }
                            break;
                        case ConsoleKey.Escape:
                            _buf.Clear(); OnInputChanged?.Invoke(""); OnInputCancelled?.Invoke();
                            break;
                        default:
                            if (!char.IsControl(k.KeyChar) || k.Key == ConsoleKey.Tab)
                            { _buf.Append(k.KeyChar); OnInputChanged?.Invoke(_buf.ToString()); }
                            break;
                    }
                }
                try { Task.Delay(PollingIntervalMs, token).Wait(token); }
                catch (OperationCanceledException) { break; }
                catch (AggregateException) { break; }
            }
        }
    }
}
