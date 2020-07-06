using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace RenderLite.Core
{
    public class RenderEngine : IDisposable
    {
        private enum ComponentSelection
        {
            None,
            Next,
            Previous,
        }

        private const int _width = 100;
        private const int _height = 30;
        private const int _targetFramerate = 200;
        private const long _targetFrametimeMilliseconds = 1000 / _targetFramerate;

        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Thread _renderThread;
        private readonly Thread _inputThread;
        private readonly List<Component> _components;
        private readonly HashSet<Component> _componentsHash;
        private readonly Stopwatch _stopwatch;

        private readonly object _lock;

        public RenderEngine(CancellationTokenSource cancellationTokenSource = null)
        {
            ThreadStart render = new ThreadStart(Render);
            ThreadStart input = new ThreadStart(HandleInput);

            _cancellationTokenSource = cancellationTokenSource ?? new CancellationTokenSource();
            _renderThread = new Thread(render);
            _inputThread = new Thread(input);
            _components = new List<Component>();
            _componentsHash = new HashSet<Component>();
            _stopwatch = new Stopwatch();

            _lock = new object();
        }

        public void Begin()
        {
            _renderThread.Start();
            _inputThread.Start();
            _stopwatch.Start();
        }

        public void Dispose()
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

                SetSelectedComponent();
            }
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

                component.Dispose();

                SetSelectedComponent();
            }
        }

        public void ClearComponents()
        {
            lock (_lock)
            {
                var tempComponents = new List<Component>(_components);

                _components.Clear();
                _componentsHash.Clear();

                foreach (var component in tempComponents)
                {
                    component.Dispose();
                }

                SetSelectedComponent();
            }
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

                foreach (var component in tempComponents)
                {
                    lock (_lock)
                    {
                        component.Draw();
                    }
                }

                Console.SetWindowSize(_width, _height);
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

        private void HandleInput()
        {
            bool exitRequested = false;

            do
            {
                var consoleKeyInfo = Console.ReadKey(false);
                var selectedComponent = GetSelectedComponent();

                if (selectedComponent != null && selectedComponent.IsInFocus)
                {
                    selectedComponent.OnKeypress(consoleKeyInfo);

                    continue;
                }

                switch (consoleKeyInfo.Key)
                {
                    case ConsoleKey.Escape:
                        exitRequested = true;
                        break;
                    case ConsoleKey.Enter:
                        if (selectedComponent != null && selectedComponent.IsSelectable)
                            selectedComponent.IsInFocus = true;
                        break;
                    case ConsoleKey.UpArrow:
                        SetSelectedComponent(ComponentSelection.Previous);
                        break;
                    case ConsoleKey.DownArrow:
                        SetSelectedComponent(ComponentSelection.Next);
                        break;
                    default:
                        break;
                }
            }
            while (!exitRequested);

            Dispose();
        }

        private void SetSelectedComponent(ComponentSelection selection = ComponentSelection.None)
        {
            lock (_lock)
            {
                var tempComponents = new List<Component>(_components);
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
        }

        private Component GetSelectedComponent()
        {
            lock (_lock)
            {
                return new List<Component>(_components)
                    .Where(c => c.IsSelected)
                    .FirstOrDefault();
            }
        }

    }
}
