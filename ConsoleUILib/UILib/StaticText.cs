using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleUILib.UILib
{
    public class StaticText : WidgetBase
    {
        private readonly DataComponent _data = new DataComponent();
        public override void Bind(Func<object> getter) => _data.Bind(getter);
        protected override IEnumerable<IBindableComponent> GetComponents()
        {
            yield return _data;
        }
        public override void Print(ICanvas canvas)
        {
            if (!Visible) return;
            canvas.WriteLine(_data.GetValue()?.ToString() ?? "");
        }
    }
    public class Divider : StaticText
    {
        public Divider(string pattern = "----------------------------------------")
        {
            Bind(() => pattern); // 内部绑定为静态字符串
        }
    }
    public class DoubleDivider : StaticText
    {
        public DoubleDivider(string pattern = "========================================")
        {
            Bind(() => pattern);
        }
    }
}
