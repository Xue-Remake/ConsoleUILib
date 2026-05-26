using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleUILib.UILib
{
    public class Session : ITimeOperator, IDisposable
    {
        private readonly List<WidgetBase> _widgets = new();
        private readonly object _lock = new();
        private CancellationTokenSource _cts;
        private Task _renderTask;
        private int _tick;
        private int _intervalMs = 500;
        private bool _isRunning;
        private readonly Stopwatch _sw = new();

        // 双缓冲画布
        private BufferedCanvas _canvas;

        // 控件区域记录：行号 -> 控件
        private readonly Dictionary<int, WidgetBase> _occupiedRows = new();

        // 脏控件列表
        private readonly HashSet<WidgetBase> _dirtyWidgets = new();
        private readonly object _dirtyLock = new();

        // 单例
        private static readonly Lazy<Session> _default = new(() => new Session());
        public static Session Default => _default.Value;

        // 属性
        public bool IsRunning => _isRunning;
        public int TickCount => _tick;
        public TimeSpan Elapsed => _sw.Elapsed;
        public int UpdateIntervalMs
        {
            get => _intervalMs;
            set => _intervalMs = Math.Max(16, value);
        }
        public int WidgetCount { get { lock (_lock) return _widgets.Count; } }

        // 事件
        public event Action<int> OnTick;
        public event Action OnBeforeRender;
        public event Action OnAfterRender;
        public event Action<WidgetBase> OnWidgetAdded;
        public event Action<WidgetBase> OnWidgetRemoved;
        public event Action OnStopped;

        // ITimeOperator
        public int GetTick() => _tick;

        public Session()
        {
            _canvas = new BufferedCanvas(Console.WindowWidth, Console.WindowHeight);
        }

        // 控件管理
        public void AddWidget(WidgetBase widget)
        {
            if (widget == null) return;
            lock (_lock) _widgets.Add(widget);
            (widget as ISessionAware)?.OnAttached(this);
            widget.OnDirty += OnWidgetDirty;
            OnWidgetAdded?.Invoke(widget);

            // 初次添加立即绘制
            if (_isRunning)
            {
                FullRedraw();
            }
        }

        public bool RemoveWidget(WidgetBase widget)
        {
            if (widget == null) return false;
            bool ok;
            lock (_lock) ok = _widgets.Remove(widget);
            if (ok)
            {
                widget.OnDirty -= OnWidgetDirty;
                lock (_dirtyLock) _dirtyWidgets.Remove(widget);
                (widget as ISessionAware)?.OnDetached(this);
                OnWidgetRemoved?.Invoke(widget);
                if (_isRunning) FullRedraw();
            }
            return ok;
        }

        public bool RemoveWidgetAt(int index)
        {
            WidgetBase w = null;
            lock (_lock)
            {
                if (index >= 0 && index < _widgets.Count)
                {
                    w = _widgets[index];
                    _widgets.RemoveAt(index);
                }
            }
            if (w != null) return RemoveWidget(w);
            return false;
        }

        public bool RemoveWidgetByName(string name)
        {
            WidgetBase w = null;
            lock (_lock)
            {
                w = _widgets.Find(x => x.Name == name);
                if (w != null) _widgets.Remove(w);
            }
            if (w != null) return RemoveWidget(w);
            return false;
        }

        public void ClearWidgets()
        {
            List<WidgetBase> copy;
            lock (_lock)
            {
                copy = new List<WidgetBase>(_widgets);
                _widgets.Clear();
            }
            foreach (var w in copy) RemoveWidget(w);
        }

        public WidgetBase GetWidgetAt(int index)
        {
            lock (_lock) return (index >= 0 && index < _widgets.Count) ? _widgets[index] : null;
        }

        public WidgetBase GetWidgetByName(string name)
        {
            lock (_lock) return _widgets.Find(w => w.Name == name);
        }

        public List<T> GetWidgetsByType<T>() where T : WidgetBase
        {
            lock (_lock) return _widgets.OfType<T>().ToList();
        }

        public IReadOnlyList<WidgetBase> GetWidgets()
        {
            lock (_lock) return _widgets.ToArray();
        }

        // 生命周期
        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;
            _cts = new CancellationTokenSource();
            _sw.Restart();
            _tick = 0;
            ResizeCanvas(); // 根据控制台窗口大小重设画布
            FullRedraw();
            _renderTask = Task.Run(() => RenderLoop(_cts.Token));
        }

        public void Run()
        {
            Start();
            _renderTask?.Wait();
        }

        public void Stop()
        {
            if (!_isRunning) return;
            _cts?.Cancel();
            _renderTask?.Wait(TimeSpan.FromSeconds(2));
            _isRunning = false;
            _sw.Stop();
            Console.CursorVisible = true;
            OnStopped?.Invoke();
        }

        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
            ClearWidgets();
        }

        // 渲染循环（主要变更）
        private async Task RenderLoop(CancellationToken token)
        {
            Console.CursorVisible = false;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    // 1. 处理所有控件的 Update()
                    UpdateAllWidgets();

                    // 2. 绘制所有标记为脏的控件
                    ProcessDirtyWidgets();

                    // 3. 触发时钟事件
                    OnTick?.Invoke(_tick);
                    _tick++;

                    try { await Task.Delay(_intervalMs, token); }
                    catch (OperationCanceledException) { break; }
                }
            }
            finally { Console.CursorVisible = true; }
        }

        private void UpdateAllWidgets()
        {
            WidgetBase[] snapshot;
            lock (_lock) snapshot = _widgets.ToArray();
            foreach (var w in snapshot)
            {
                try { w.Update(); }
                catch { /* 忽略错误防止渲染崩溃 */ }
            }
        }

        // 脏控件处理
        private void OnWidgetDirty(WidgetBase widget)
        {
            lock (_dirtyLock) _dirtyWidgets.Add(widget);
        }

        private void ProcessDirtyWidgets()
        {
            WidgetBase[] dirtySnapshot;
            lock (_dirtyLock)
            {
                if (_dirtyWidgets.Count == 0) return;
                dirtySnapshot = _dirtyWidgets.ToArray();
                _dirtyWidgets.Clear();
            }

            // 仅重绘脏控件的区域
            foreach (var widget in dirtySnapshot)
            {
                // 查找该控件在布局中的起始行
                int startRow = -1;
                lock (_lock)
                {
                    for (int row = 0; row < _canvas.Height; row++)
                    {
                        if (_occupiedRows.TryGetValue(row, out var w) && w == widget)
                        {
                            startRow = row;
                            break;
                        }
                    }
                }

                if (startRow >= 0)
                {
                    _canvas.CurrentRow = startRow;
                    try { widget.Print(_canvas); }
                    catch (Exception ex)
                    {
                        _canvas.WriteLine($"[{widget.GetType().Name} Error] {ex.Message}");
                    }
                    // 清除可能残留的旧行（如果新内容行数减少）
                    ClearBelow(_canvas.CurrentRow, widget, startRow);
                }
                else
                {
                    // 控件未找到，可能因添加/删除导致布局变化，全量重绘
                    FullRedraw();
                    break;
                }
            }
            // 输出差异
            _canvas.Render();
        }

        // 全量重绘（初次或结构变化时）
        private void FullRedraw()
        {
            ResizeCanvas();
            _canvas.Clear();
            _occupiedRows.Clear();

            int row = 0;
            WidgetBase[] snapshot;
            lock (_lock) snapshot = _widgets.ToArray();

            foreach (var widget in snapshot)
            {
                if (!widget.Visible) continue;

                int startRow = row;
                _canvas.CurrentRow = row;

                try { widget.Print(_canvas); }
                catch (Exception ex)
                {
                    _canvas.WriteLine($"[{widget.GetType().Name} Error] {ex.Message}");
                }

                int endRow = _canvas.CurrentRow - 1; // 最末有效行
                for (int r = startRow; r <= endRow && r < _canvas.Height; r++)
                {
                    _occupiedRows[r] = widget;
                }
                row = _canvas.CurrentRow;
            }

            // 清除剩余行
            for (int i = row; i < _canvas.Height; i++)
                _occupiedRows.Remove(i);

            _canvas.Render();
        }

        // 清除控件区域下方多余的行（当控件行数变少时）
        private void ClearBelow(int newEndRow, WidgetBase widget, int oldStartRow)
        {
            for (int r = newEndRow; r < _canvas.Height; r++)
            {
                if (_occupiedRows.TryGetValue(r, out var w) && w == widget)
                {
                    _canvas.WriteLine("", r);
                    _occupiedRows.Remove(r);
                }
                else break;
            }
        }

        private void ResizeCanvas()
        {
            int w = Console.WindowWidth;
            int h = Console.WindowHeight;
            if (_canvas.Width != w || _canvas.Height != h)
                _canvas.Resize(w, h);
        }
    }
}