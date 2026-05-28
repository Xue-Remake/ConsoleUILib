// VarMonitor.cs  (修正版)
using System;
using System.Collections.Generic;

namespace ConsoleUILib.UILib
{
    /// <summary>
    /// 变量监控控件。通过 Func 委托绑定变量，按 Session 刷新周期取样，
    /// 仅在值变化时触发局部重绘。底层变量更新频率无上限。
    /// </summary>
    public class VarMonitor : WidgetBase
    {
        private readonly string _title;
        private readonly List<Slot> _slots = new();

        // ★ 关键：用 class 而非 struct
        private sealed class Slot
        {
            public string Label;
            public Func<object> Getter;
            public object Cached;
            public string Format;
        }

        public VarMonitor(string title = "Monitor")
        {
            _title = title ?? "";
        }

        /// <summary>绑定一个通用变量</summary>
        public void Bind(string label, Func<object> getter, string format = null)
        {
            if (string.IsNullOrEmpty(label)) throw new ArgumentNullException(nameof(label));
            if (getter == null) throw new ArgumentNullException(nameof(getter));
            var slot = new Slot
            {
                Label = label,
                Getter = getter,
                Cached = getter(),        // ← 立即取值，消除 null
                Format = format
            };
            _slots.Add(slot);
            MarkDirty();
        }

        /// <summary>绑定 double 变量的便捷重载</summary>
        public void Bind(string label, Func<double> getter, string format = "F3")
        {
            Bind(label, () => (object)getter(), format);
        }

        /// <summary>解绑所有变量</summary>
        public void ClearBindings()
        {
            _slots.Clear();
            MarkDirty();
        }

        public override void Update()
        {
            bool anyChanged = false;
            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];       // class 引用，直接操作
                object newVal;
                try { newVal = slot.Getter(); }
                catch { newVal = "<error>"; }

                if (!Equals(newVal, slot.Cached))
                {
                    slot.Cached = newVal;
                    anyChanged = true;
                }
            }
            if (anyChanged)
                MarkDirty();
        }

        public override void Print() => Print(Console.Out);

        public override void Print(ICanvas canvas)
        {
            if (!Visible) return;

            canvas.WriteLine($"=== {_title} ===");
            foreach (var slot in _slots)
            {
                string valStr = FormatValue(slot.Cached, slot.Format);
                canvas.WriteLine($"  {slot.Label}: {valStr}");
            }
            canvas.WriteLine("──────────────────────────");
        }

        private static string FormatValue(object val, string format)
        {
            if (val == null) return "(null)";
            if (!string.IsNullOrEmpty(format) && val is IFormattable f)
                return f.ToString(format, null);
            return val.ToString();
        }

        private void Print(System.IO.TextWriter tw)
        {
            tw.WriteLine($"=== {_title} ===");
            foreach (var slot in _slots)
            {
                string valStr = FormatValue(slot.Cached, slot.Format);
                tw.WriteLine($"  {slot.Label}: {valStr}");
            }
            tw.WriteLine("──────────────────────────");
        }
    }
}
