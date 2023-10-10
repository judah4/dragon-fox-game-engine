using Microsoft.Extensions.Logging;
using Silk.NET.Input;
using Silk.NET.Windowing;
using System.Linq;

namespace DragonGameEngine.Core
{
    public class EngineInternalInput
    {

        private readonly ILogger _logger;
        private readonly IWindow _window;
        private readonly IInputContext _inputContext;
        private IKeyboard? _keyboard;

        public EngineInternalInput(IInputContext inputContext, IWindow window, ILogger logger)
        {
            _logger = logger;
            _window = window;
            _inputContext = inputContext;

            _inputContext.ConnectionChanged += ConnectionChanged;
            SetupKeyboard();

        }

        private void ConnectionChanged(IInputDevice device, bool arg2)
        {
            if (device is IKeyboard)
            {
                _logger.LogDebug("Keyboard state changed, Connected:{deviceIsConnected}", device.IsConnected);
            }
        }

        void SetupKeyboard()
        {
            if (_inputContext.Keyboards.Count == 0)
            {
                return;
            }
            _keyboard = _inputContext.Keyboards[0];
            _keyboard.KeyDown += OnKeyDown;
        }

        private void OnKeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            if (key == Key.Enter && keyboard.IsKeyPressed(Key.AltLeft) || key == Key.AltLeft && keyboard.IsKeyPressed(Key.Enter))
            {
                //resize!
                _logger.LogDebug("Resize is pressed!");
                var curState = _window.WindowState;
                switch (curState)
                {
                    case WindowState.Normal:
                        _window.WindowState = WindowState.Fullscreen;
                        break;
                    default:
                    case WindowState.Fullscreen:
                        _window.WindowState = WindowState.Normal;
                        break;
                }
            }
        }
    }
}
