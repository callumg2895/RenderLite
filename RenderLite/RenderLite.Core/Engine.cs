using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace RenderLite.Core
{
    public abstract class Engine : IDisposable
    {
        protected enum ComponentSelection
        {
            None,
            Next,
            Previous,
        }

        public const int WIDTH = 100;
        public const int HEIGHT = 30;

        private const int _targetFramerate = 200;
        private const long _targetFrametimeMilliseconds = 1000 / _targetFramerate;

        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Thread _renderThread;
        private readonly List<Component> _components;
        private readonly HashSet<Component> _componentsHash;
        private readonly Stopwatch _stopwatch;

        private readonly object _lock;

        private volatile bool _triggerWindowRefresh;

        public Engine(CancellationTokenSource cancellationTokenSource = null)
        {
            ThreadStart render = new ThreadStart(Render);

            _cancellationTokenSource = cancellationTokenSource ?? new CancellationTokenSource();
            _renderThread = new Thread(render);
            _components = new List<Component>();
            _componentsHash = new HashSet<Component>();
            _stopwatch = new Stopwatch();

            _lock = new object();

            _triggerWindowRefresh = false;
        }

        public virtual void Begin()
        {
            _renderThread.Start();
            _stopwatch.Start();
        }

        public virtual void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _renderThread.Join();

            _stopwatch.Stop();

            List<Component> tempComponents = null;

            lock (_lock)
            {
                tempComponents = new List<Component>(_components);
            }

            foreach (var component in tempComponents)
            {
                RemoveComponent(component);
                component.Dispose();
            }

        }

        public CancellationToken GetCancellationToken()
        {
            return _cancellationTokenSource.Token;
        }

        public void AddComponent(Component component)
        {
            lock (_lock)
            {
                if (_componentsHash.Contains(component))
                {
                    return;
                }

                _components.Add(component);
                _componentsHash.Add(component);
            }

            TriggerWindowRefresh();
            SetSelectedComponent();
        }

        public void RemoveComponent(Component component)
        {
            lock (_lock)
            {
                if (!_componentsHash.Contains(component))
                {
                    return;
                }

                _components.Remove(component);
                _componentsHash.Remove(component);
            }

            component.Dispose();

            TriggerWindowRefresh();
            SetSelectedComponent();
        }

        public void ClearComponents()
        {
            List<Component> tempComponents = null;

            lock (_lock)
            {
                tempComponents = new List<Component>(_components);

                _components.Clear();
                _componentsHash.Clear();

            }

            foreach (var component in tempComponents)
            {
                component.Dispose();
            }

            TriggerWindowRefresh();
            SetSelectedComponent();        
        }

        private void Render()
        {
            List<Component> tempComponents = null;

            do
            {
                lock (_lock)
                {
                    tempComponents = new List<Component>(_components);
                }

                if (_triggerWindowRefresh)
                {
                    Console.Clear();
                }

                DrawComponents(_triggerWindowRefresh);

                Console.SetWindowSize(WIDTH, HEIGHT);
                Console.SetCursorPosition(0, 0);
                Console.ResetColor();

                long elapsed = _stopwatch.ElapsedMilliseconds;
                long remaining = _targetFrametimeMilliseconds > elapsed
                    ? _targetFrametimeMilliseconds - elapsed
                    : 0;

                Thread.Sleep((int)remaining);
            }
            while (!_cancellationTokenSource.Token.IsCancellationRequested);

            Console.Clear();
        }

        private void DrawComponents(bool drawAll)
        {
            List<Component> tempComponents = null;

            lock (_lock)
            {
                tempComponents = new List<Component>(_components);
            }

            _triggerWindowRefresh = false;

            foreach (var component in tempComponents)
            {
                _triggerWindowRefresh |= component.RequiresWindowRefresh;

                if (drawAll || component.RequiresUpdate)
                {
                    component.RequiresWindowRefresh = false;
                    component.RequiresUpdate = false;
                    component.Draw();
                }
            }
        }

        protected void SetSelectedComponent(ComponentSelection selection = ComponentSelection.None)
        {
            List<Component> tempComponents = null;

            lock (_lock)
            {
                tempComponents = new List<Component>(_components);
            }

            var oldSelected = tempComponents.Where(c => c.IsSelected).FirstOrDefault();

            if (tempComponents.Count == 0)
            {
                return;
            }
            else if (oldSelected == null)
            {
                oldSelected = tempComponents.First();
            }

            int oldIndex = tempComponents.IndexOf(oldSelected);
            int newIndex = oldIndex + selection switch
            {
                ComponentSelection.None => 0,
                ComponentSelection.Next => 1,
                ComponentSelection.Previous => -1,
                _ => throw new ArgumentException($"Selection {selection} not recognized")
            };

            if (newIndex != 0)
            {
                newIndex = newIndex > 0
                    ? newIndex % tempComponents.Count
                    : newIndex + tempComponents.Count;
            }

            tempComponents[oldIndex].IsSelected = false;
            tempComponents[newIndex].IsSelected = true;        
        }

        protected Component GetSelectedComponent()
        {
            lock (_lock)
            {
                return new List<Component>(_components)
                    .Where(c => c.IsSelected)
                    .FirstOrDefault();
            }
        }

        protected void TriggerWindowRefresh()
        {
            _triggerWindowRefresh = true;
        }

    }
}
