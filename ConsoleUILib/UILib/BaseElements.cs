using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleUILib.UILib
{
    /// <summary>
    /// 所有控件的抽象基类
    /// 内部通过组合 Component 获取数据，脏标记自动管理
    /// </summary>
    public abstract class WidgetBase
    {
        public string Name { get; set; }
        public bool Visible { get; set; } = true;
        /// <summary>控件自身是否脏（需要重绘）</summary>
        public bool IsDirty { get; set; }
        /// <summary>通用绑定方法，默认绑定到第一个组件（子类可重写）</summary>
        public virtual void Bind(Func<object> getter)
        {
            var first = GetFirstComponent();
            if (first == null)
                throw new NotSupportedException("This widget has no default component to bind.");
            first.Bind(getter);
        }
        /// <summary>
        /// 更新控件状态，检查所有组件是否变化并设置脏标记。
        /// </summary>
        public virtual void Update()
        {
            foreach (var comp in GetComponents())
            {
                if (comp.CheckAndUpdate())
                {
                    IsDirty = true;
                    break;
                }
            }
        }
        /// <summary>渲染到指定画布（子类必须实现）</summary>
        public abstract void Print(ICanvas canvas);
        /// <summary>重置脏状态（Session 在渲染后调用）</summary>
        public void CleanDirty() => IsDirty = false;
        /// <summary>获取控件关联的组件集合，用于 Update 检查</summary>
        protected abstract System.Collections.Generic.IEnumerable<IBindableComponent> GetComponents();
        /// <summary>获取默认绑定的第一个组件（用于通用 Bind）</summary>
        protected virtual IBindableComponent GetFirstComponent()
        {
            using (var enumerator = GetComponents().GetEnumerator())
                return enumerator.MoveNext() ? enumerator.Current : null;
        }
        private bool CheckComponentsForChange()
        {
            bool anyChange = false;
            foreach (var comp in GetComponents())
            {
                if (comp.CheckAndUpdate())
                    anyChange = true;
            }
            return anyChange;
        }
    }
    /// <summary>
    /// 数据组件基类，实现 IBindableComponent。
    /// </summary>
    public abstract class BaseComponent : IBindableComponent
    {
        protected object _value;
        public abstract void Bind(Func<object> getter);
        public virtual object GetValue() => _value;
        public abstract bool CheckAndUpdate();
    }

    public class StaticComponent : BaseComponent
    {
        public StaticComponent(object value = null)
        {
            _value = value;
        }
        public override void Bind(Func<object> getter)
        {
            _value = getter?.Invoke();
        }
        public override bool CheckAndUpdate() => false;
    }

    public class DataComponent : BaseComponent
    {
        private Func<object> _getter;
        public DataComponent(Func<object> getter = null)
        {
            if (getter != null)
                Bind(getter);
        }
        public override void Bind(Func<object> getter)
        {
            _getter = getter ?? throw new ArgumentException(nameof(getter));
            _value = _getter();
        }
        public override bool CheckAndUpdate()
        {
            if (_getter == null) return false;
            object newVal = _getter();
            if (!Equals(newVal, _value))
            {
                _value = newVal;
                return true;
            }
            return false;
        }
    }
}
