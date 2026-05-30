using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleUILib.UILib
{
    public class Session : ITimeOperator, IDisposable
    {
        private readonly List<WidgetBase> _widgets = new List<WidgetBase>();
        private readonly object _lock = new object();
        private CancellationTokenSource _cts;
        private Task _renderTask;
        private int _tick;
        private readonly int _intervalMs;
        private bool _isRunning;
        private readonly Stopwatch _sw = new Stopwatch();
        private BufferedCanvas _canvas;

        // 控件行索引映射 (用于局部重绘)
        private readonly Dictionary<int, WidgetBase> _occupiedRows = new Dictionary<int, WidgetBase>();

        public bool IsRunning => _isRunning;
        public int TickCount => _tick;
        public TimeSpan Elapsed => _sw.Elapsed;
        public int UpdateIntervalMs => _intervalMs;
        public int WidgetCount { get { lock (_lock) return _widgets.Count; } }

        // 事件注入
        public event Action<int> OnTick;
        public event Action OnBeforeRender;
        public event Action OnAfterRender;
        public event Action<WidgetBase> OnWidgetAdded;
        public event Action<WidgetBase> OnWidgetRemoved;
        public event Action OnStopped;

        public int GetTick() => _tick;

        public Session(int intervalMs)
        {
            _intervalMs = Math.Max(1, intervalMs);
            _canvas = new BufferedCanvas(Console.WindowWidth, Console.WindowHeight);
        }

        // ---------- 控件管理 ----------
        public void Add(WidgetBase widget)
        {
            if (widget == null) return;
            lock (_lock) _widgets.Add(widget);
            (widget as ISessionAware)?.OnAttached(this);
            OnWidgetAdded?.Invoke(widget);
            if (_isRunning) RestartFullRedraw(); // 重新全量绘制以避免布局错乱
        }

        public bool Remove(WidgetBase widget)
        {
            if (widget == null) return false;
            bool ok;
            lock (_lock) ok = _widgets.Remove(widget);
            if (ok)
            {
                (widget as ISessionAware)?.OnDetached(this);
                OnWidgetRemoved?.Invoke(widget);
                if (_isRunning) RestartFullRedraw();
            }
            return ok;
        }

        public void Clear()
        {
            List<WidgetBase> copy;
            lock (_lock)
            {
                copy = new List<WidgetBase>(_widgets);
                _widgets.Clear();
            }
            foreach (var w in copy) Remove(w);
        }

        // ---------- 生命周期 ----------
        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;
            _cts = new CancellationTokenSource();
            _sw.Restart();
            _tick = 0;
            ResizeCanvas();
            FullRedraw(); // 初始全量绘制
            _renderTask = Task.Run(() => RenderLoop(_cts.Token));
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
            Clear();
        }

        // ---------- 渲染循环 ----------
        private async Task RenderLoop(CancellationToken token)
        {
            Console.CursorVisible = false;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    OnBeforeRender?.Invoke();

                    // 1. 更新所有控件（控件自行检测脏标记）
                    UpdateAllWidgets();

                    // 2. 绘制所有脏控件
                    bool anyDirty = false;
                    WidgetBase[] snapshot;
                    lock (_lock) snapshot = _widgets.ToArray();

                    if (snapshot.Length > 0)
                    {
                        // 记录现有的行映射，用于脏绘制的起点
                        int currentRow = 0;
                        bool needFullRedraw = false;

                        foreach (var widget in snapshot)
                        {
                            if (!widget.Visible) continue;
                            if (widget.IsDirty)
                            {
                                anyDirty = true;
                                // 尝试查找该控件之前的起始行
                                int oldStartRow = -1;
                                for (int r = 0; r < _canvas.Height; r++)
                                {
                                    if (_occupiedRows.TryGetValue(r, out var w) && w == widget)
                                    {
                                        oldStartRow = r;
                                        break;
                                    }
                                }

                                if (oldStartRow >= 0)
                                {
                                    _canvas.CurrentRow = oldStartRow;
                                    widget.Print(_canvas);
                                    // 清除可能多余的旧行
                                    ClearBelow(_canvas.CurrentRow, widget, oldStartRow);
                                }
                                else
                                {
                                    // 找不到旧位置或首次绘制，全量重绘更安全
                                    needFullRedraw = true;
                                    break;
                                }
                            }
                        }

                        if (needFullRedraw)
                        {
                            FullRedraw();
                        }
                        else if (anyDirty)
                        {
                            // 重新整理行映射（因为可能部分控件重绘后移动了行）
                            RebuildRowMapping(snapshot);
                            _canvas.Render();
                        }
                    }

                    // 重置所有控件的脏标记
                    foreach (var w in snapshot) w.CleanDirty();

                    OnTick?.Invoke(_tick);
                    _tick++;

                    OnAfterRender?.Invoke();

                    try { await Task.Delay(_intervalMs, token); }
                    catch (OperationCanceledException) { break; }
                }
            }
            finally
            {
                Console.CursorVisible = true;
            }
        }

        private void UpdateAllWidgets()
        {
            WidgetBase[] snapshot;
            lock (_lock) snapshot = _widgets.ToArray();
            foreach (var w in snapshot)
            {
                try { w.Update(); }
                catch { /* 忽略用户代码异常 */ }
            }
        }

        // ---------- 全量重绘 ----------
        private void FullRedraw()
        {
            ResizeCanvas();
            _canvas.Clear();
            _occupiedRows.Clear();

            WidgetBase[] snapshot;
            lock (_lock) snapshot = _widgets.ToArray();

            int row = 0;
            foreach (var widget in snapshot)
            {
                if (!widget.Visible) continue;
                _canvas.CurrentRow = row;
                try { widget.Print(_canvas); }
                catch (Exception ex) { _canvas.WriteLine($"[Error] {ex.Message}"); }

                int endRow = _canvas.CurrentRow - 1;
                for (int r = row; r <= endRow && r < _canvas.Height; r++)
                    _occupiedRows[r] = widget;
                row = _canvas.CurrentRow;
            }
            _canvas.Render();
        }

        private void RestartFullRedraw()
        {
            if (_isRunning) FullRedraw();
        }

        private void RebuildRowMapping(WidgetBase[] widgets)
        {
            _occupiedRows.Clear();
            int row = 0;
            foreach (var widget in widgets)
            {
                if (!widget.Visible) continue;
            }
        }

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