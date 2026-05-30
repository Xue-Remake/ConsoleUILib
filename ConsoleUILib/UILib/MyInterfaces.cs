using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleUILib.UILib
{
    public interface ICanvas
    {
        int CurrentRow { get; set; }
        void Write(string text, int row, int col);
        void WriteLine(string text, int row);
        void WriteLine(string text);
        void Clear();
        int Height { get; }
        int Width { get; }
    }
    public interface ITimeOperator
    {
        int GetTick();
        TimeSpan Elapsed { get; }
    }
    public interface ISessionAware
    {
        void OnAttached(Session session);
        void OnDetached(Session session);
    }
    public interface IBindableComponent
    {
        void Bind(Func<object> getter);
        bool CheckAndUpdate();
        object GetValue();
    }
}
