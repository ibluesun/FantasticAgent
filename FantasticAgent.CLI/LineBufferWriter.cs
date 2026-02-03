using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace FantasticAgent.CLI
{
    class LineBufferWriter : TextWriter
    {
        private readonly ConcurrentQueue<string> _lines = new ConcurrentQueue<string>();
        private readonly StringBuilder _currentLine = new();

        public ConcurrentQueue<string> Lines => _lines;

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            if (value == '\n')
            {
                if (_currentLine.Length > 0)
                {
                    _lines.Enqueue(_currentLine.ToString());
                    _currentLine.Clear();
                }

                while (_lines.Count > 50)
                {
                    _lines.TryDequeue(out _);
                }
            }
            else if (value != '\r')
            {
                _currentLine.Append(value);
            }
        }

        public override void WriteLine(string? value)
        {
            if (_currentLine.Length > 0)
            {
                _lines.Enqueue(_currentLine.ToString() + value);
                _currentLine.Clear();
            }
            else
            {
                _lines.Enqueue(value ?? string.Empty);
            }

            while (_lines.Count > 50)
            {
                _lines.TryDequeue(out _);
            }

        }

    }
}
