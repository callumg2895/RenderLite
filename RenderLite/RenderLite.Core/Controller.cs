using System;
using System.Collections.Generic;
using System.Text;

namespace RenderLite.Core
{
    public abstract class Controller
    {
        protected readonly Engine _renderEngine;

        public Controller(Engine renderEngine)
        {
            _renderEngine = renderEngine;
        }
    }
}
