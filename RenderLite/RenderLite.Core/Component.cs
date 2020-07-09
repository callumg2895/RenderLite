using System;
using System.Collections.Generic;
using System.Threading;

namespace RenderLite.Core
{
    public abstract class Component : IDisposable
    {
        internal volatile bool RequiresUpdate;

        protected readonly Thread _updateThread;
        protected readonly CancellationTokenSource _cancellationTokenSource;
        protected readonly RenderEngine _renderEngine;
        protected readonly List<Component> _components;
        protected readonly Dictionary<ConsoleKey, Action> _focusKeyPressActions;
        protected readonly Dictionary<ConsoleKey, Action> _selectedKeyPressActions;
        protected readonly object _lock;

        private volatile bool _isSelected;
        private volatile bool _isInFocus;

        public Component(RenderEngine renderEngine)
        {
            ThreadStart threadStart = new ThreadStart(Update);

            RequiresUpdate = false;

            _updateThread = new Thread(threadStart);
            _cancellationTokenSource = new CancellationTokenSource();
            _renderEngine = renderEngine;
            _components = new List<Component>();
            _focusKeyPressActions = new Dictionary<ConsoleKey, Action>();
            _selectedKeyPressActions = new Dictionary<ConsoleKey, Action>();
            _lock = new object();

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
                    RequiresUpdate = true;
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
                    RequiresUpdate = true;
                }
            }
        }

        protected int XPosition { get; set; }

        protected int YPosition { get; set; }

        public abstract void Draw();

        public bool OnKeypress(ConsoleKeyInfo keyInfo)
        {
            if (!IsSelected)
            {
                return false;
            }

            var actionsLookup = IsInFocus
                ? _focusKeyPressActions
                : _selectedKeyPressActions;

            if (actionsLookup.TryGetValue(keyInfo.Key, out Action action))
            {
                action.Invoke();
                RequiresUpdate = true;
                return true;
            }

            return false;
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
