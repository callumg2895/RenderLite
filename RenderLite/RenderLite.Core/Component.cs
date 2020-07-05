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
        protected readonly Dictionary<ConsoleKey, Action> _keyPressActions;
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
            _keyPressActions = new Dictionary<ConsoleKey, Action>();
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

        public void OnKeypress(ConsoleKeyInfo keyInfo)
        {
            if (!_keyPressActions.TryGetValue(keyInfo.Key, out Action action))
            {
                return;
            }

            action.Invoke();

            _requiresUpdate = true;
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
