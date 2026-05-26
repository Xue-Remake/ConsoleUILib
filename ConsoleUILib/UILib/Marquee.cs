using System;

namespace ConsoleUILib.UILib
{
    public class Marquee : WidgetBase
    {
        private string _content;
        private int _displayLen;
        private int _cycle;
        private readonly ITimeOperator _time;
        private int _lastOffset = -1;

        public int DisplayLength
        {
            get => _displayLen;
            set
            {
                _displayLen = Math.Max(1, value);
                MarkDirty();
            }
        }

        public string Content
        {
            get => _content;
            set
            {
                _content = value ?? "";
                _cycle = Math.Max(1, _content.Length);
                _lastOffset = -1;
                MarkDirty();
            }
        }

        public Marquee(string content, int displayLength, ITimeOperator timeOp)
        {
            _time = timeOp ?? throw new ArgumentNullException(nameof(timeOp));
            _content = content ?? "";
            _displayLen = Math.Max(1, displayLength);
            _cycle = Math.Max(1, _content.Length);
        }

        public override void Print()
        {
            int offset = _time.GetTick() % _cycle;
            string looped = _content + _content;
            Console.WriteLine(looped.Substring(offset, Math.Min(_displayLen, _content.Length)));
        }

        public override void Print(ICanvas canvas)
        {
            if (!Visible) return;
            int offset = _time.GetTick() % _cycle;
            string looped = _content + _content;
            canvas.WriteLine(looped.Substring(offset, Math.Min(_displayLen, _content.Length)));
            _lastOffset = offset;
        }

        public override void Update()
        {
            // 由 Session 在每个 tick 调用，检查偏移是否变化
            int offset = _time.GetTick() % _cycle;
            if (offset != _lastOffset)
            {
                MarkDirty(); // 存在变化，请求重绘
            }
        }
    }
}