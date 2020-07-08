using System;
using System.Collections.Generic;
using System.Threading;

namespace RenderLite.Core
{
    public abstract class Component : IDisposable
    {
        protected readonly Thread _updateThread;
        protected readonly CancellationTokenSource _cancellationTokenSource;
        protected readonly RenderEngine _renderEngine;
        protected readonly List<Component> _components;
        protected readonly Dictionary<ConsoleKey, Action> _focusKeyPressActions;
        protected readonly Dictionary<ConsoleKey, Action> _selectedKeyPressActions;
        protected readonly object _lock;

        protected volatile bool _requiresUpdate;
        private volatile bool _isSelected;
        private volatile bool _isInFocus;

        public Component(RenderEngine renderEngine)
        {
            ThreadStart threadStart = new ThreadStart(Update);

            _updateThread = new Thread(threadStart);
            _cancellationTokenSource = new CancellationTokenSource();
            _renderEngine = renderEngine;
            _components = new List<Component>();
            _focusKeyPressActions = new Dictionary<ConsoleKey, Action>();
            _selectedKeyPressActions = new Dictionary<ConsoleKey, Action>();
            _lock = new object();

            _requiresUpdate = false;

            IsSelectable = false;
            IsSelected = false;
            IsInFocus = false;
            XPosition = 0;
            YPosition = 0;

            _updateThread.Start();
        }

        public bool IsSelectable { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                lock (_lock)
                {
                    _isSelected = value;
                    _requiresUpdate = true;
                }
            }
        }

        public bool IsInFocus
        {
            get => _isInFocus;
            set
            {
                lock (_lock)
                {
                    _isInFocus = value;
                    _requiresUpdate = true;
                }
            }
        }

        protected int XPosition { get; set; }

        protected int YPosition { get; set; }

        public abstract void Draw();

        public bool OnKeypress(ConsoleKeyInfo keyInfo)
        {
            bool success = true;

            if (!IsSelected)
            {
                success = false;
            }
            else if (IsInFocus && _focusKeyPressActions.TryGetValue(keyInfo.Key, out Action focusAction))
            {
                focusAction.Invoke();
            }
            else if (_selectedKeyPressActions.TryGetValue(keyInfo.Key, out Action selectedAction))
            {
                selectedAction.Invoke();
            }
            else
            {
                success = false;
            }

            _requiresUpdate = _requiresUpdate || success;

            return success;
        }

        public void Dispose()
        {
            foreach (var component in _components)
            {
                component.Dispose();
            }

            _cancellationTokenSource.Cancel();
            _updateThread.Join();

            Console.Clear();
        }

        protected abstract void Update();

    }
}
