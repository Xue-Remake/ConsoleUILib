using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleUILib.UILib
{
    public class BufferedCanvas : ICanvas
    {
        private string[] _buffer;
        private string[] _previous;
        private int _width, _height;
        private int _cursorRow;
        public int CurrentRow
        {
            get => _cursorRow;
            set => _cursorRow = Math.Clamp(value, 0, _height - 1);
        }
        public int Height => _height;
        public int Width => _width;
        public BufferedCanvas(int width, int height)
        {
            _width = Math.Max(width, 1);
            _height = Math.Max(height, 1);
            _buffer = new string[_height];
            _previous = new string[_height];
            for (int i = 0; i < _height; i++)
            {
                _buffer[i] = new string(' ', _width);
                _previous[i] = new string(' ', _width);
            }
        }
        public void Resize(int width, int height)
        {
            width = Math.Max(width, 1);
            height = Math.Max(height, 1);
            if (_width == width && _height == height) return;
            _width = width;
            _height = height;
            _buffer = new string[_height];
            _previous = new string[_height];
            for (int i = 0; i < _height; i++)
            {
                _buffer[i] = new string(' ', _width);
                _previous[i] = new string(' ', _width);
            }
            _cursorRow = Math.Min(_cursorRow, _height - 1);
        }
        public void Clear()
        {
            for (int i = 0; i < _height; i++)
                _buffer[i] = new string(' ', _width);
        }
        public void Write(string text, int row, int col)
        {
            if (row < 0 || row >= _height || col >= _width) return;
            char[] line = _buffer[row].ToCharArray();
            int len = Math.Min(text.Length, _width - col);
            for (int i = 0; i < len; i++)
                line[col + i] = text[i];
            _buffer[row] = new string(line);
        }
        public void WriteLine(string text, int row)
        {
            if (row < 0 || row >= _height) return;
            char[] line = new char[_width];
            for (int i = 0; i < _width; i++) line[i] = ' ';
            int len = Math.Min(text.Length, _width);
            for (int i = 0; i < len; i++) line[i] = text[i];
            _buffer[row] = new string(line);
        }
        public void WriteLine(string text)
        {
            if (_cursorRow < 0 || _cursorRow >= _height) return;
            WriteLine(text, _cursorRow);
            _cursorRow++;
        }
        public void Render()
        {
            Console.CursorVisible = false;
            for (int i = 0; i < _height; i++)
            {
                if (_buffer[i] != _previous[i])
                {
                    Console.SetCursorPosition(0, i);
                    Console.Write(_buffer[i]);
                    _previous[i] = _buffer[i];
                }
            }
            Console.SetCursorPosition(0, _height - 1);
            Console.CursorVisible = true;
        }
    }
}
