using System;
using System.Collections.Generic;
using static System.Collections.Specialized.BitVector32;

namespace ConsoleUILib.UILib
{
    public class Marquee : WidgetBase, ISessionAware
    {
        private readonly DataComponent _content;
        private readonly int _displayLen;
        private ITimeOperator _time;
        private int _lastOffset = -1;
        private int _cycle;

        public string Content
        {
            set => _content.Bind(() => value);
        }

        public int DisplayLength => _displayLen;

        public Marquee(string initialContent, int displayLength)
        {
            _displayLen = Math.Max(1, displayLength);
            _content = new DataComponent(() => initialContent);
            _cycle = Math.Max(1, initialContent.Length);
        }

        public void OnAttached(Session session) => _time = session;
        public void OnDetached(Session session) => _time = null;

        public override void Bind(Func<object> getter) => _content.Bind(getter);

        protected override IEnumerable<IBindableComponent> GetComponents()
        {
            yield return _content;
        }

        public override void Update()
        {
            base.Update(); // 检查内容组件是否变化
            if (_time == null) return;

            // 还要检查时间偏移变化
            string val = _content.GetValue()?.ToString() ?? "";
            _cycle = Math.Max(1, val.Length);
            int offset = _time.GetTick() % _cycle;
            if (offset != _lastOffset)
            {
                _lastOffset = offset;
                IsDirty = true;
            }
        }

        public override void Print(ICanvas canvas)
        {
            if (!Visible) return;
            string val = _content.GetValue()?.ToString() ?? "";
            int offset = _lastOffset;
            string looped = val + val;
            string display = looped.Substring(offset, Math.Min(_displayLen, val.Length));
            canvas.WriteLine(display);
        }
    }
}