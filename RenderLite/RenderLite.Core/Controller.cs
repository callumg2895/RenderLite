using System;
using System.Collections.Generic;
using System.Text;

namespace RenderLite.Core
{
    public abstract class Controller
    {
        protected readonly RenderEngine _renderEngine;

        public Controller(RenderEngine renderEngine)
        {
            _renderEngine = renderEngine;
        }
    }
}
