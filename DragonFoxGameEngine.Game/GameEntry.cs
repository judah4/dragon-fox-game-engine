using DragonGameEngine.Core;
using DragonGameEngine.Core.Rendering;
using Microsoft.Extensions.Logging;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.SDL;
using Silk.NET.Windowing;
using System;
using System.Linq;

namespace DragonFoxGameEngine.Game
{
    public sealed class GameEntry : IGameEntry
    {
        /// <summary>
        /// Set this name once a game is in progress
        /// </summary>
        public const string GAME_NAME = "";
        private readonly ILogger _logger;

        private IWindow? _window;
        private RendererFrontend? _renderer; //HACK: abstract this later. Keep this in the engine
        IKeyboard? _keyboard;
        IGamepad? _gamepad;

        //temp until camera
        private Matrix4X4<float> _view;
        private Vector3D<float> _cameraPosition;
        private Vector3D<float> _cameraEuler;
        private bool _cameraViewDirty;

        public GameEntry(ILogger logger)
        {
            _logger = logger;
        }

        public void Initialize(IWindow window, RendererFrontend renderer)
        {
            _window = window;
            _renderer = renderer;
            IInputContext input = window.CreateInput();
            _keyboard = input.Keyboards[0];
            _gamepad = input.Gamepads.FirstOrDefault();

            _keyboard.KeyDown += KeyDown;

            _cameraPosition = new Vector3D<float>(0, 0, 10);
            _cameraEuler = Vector3D<float>.Zero;
            _cameraViewDirty = true;


            _logger.LogDebug("Game initialized!");
        }

        public void Update(double deltaTime)
        {
            var rotateSpeedRad = 2.5f * (float)deltaTime;

            var moveSpeed = 5f * (float)deltaTime;

            var velocity = Vector3D<float>.Zero;
            if (_keyboard!.IsKeyPressed(Key.W))
            {
                var forward = MathUtils.ForwardFromMatrix(_view);
                velocity += forward;
            }
            if (_keyboard.IsKeyPressed(Key.S))
            {
                var backward = -MathUtils.ForwardFromMatrix(_view);
                velocity += backward;
            }
            if (_keyboard!.IsKeyPressed(Key.A))
            {
                var left = -MathUtils.RightFromMatrix(_view);
                velocity += left;
            }
            if (_keyboard.IsKeyPressed(Key.D))
            {
                var right = MathUtils.RightFromMatrix(_view);
                velocity += right;
            }

            if (_keyboard.IsKeyPressed(Key.Space))
            {
                velocity.Y += 1.0f;
            }
            if (_keyboard.IsKeyPressed(Key.ControlLeft))
            {
                velocity.Y += -1.0f;
            }

            if (velocity != Vector3D<float>.Zero)
            {
                MoveCamera(velocity * moveSpeed);
            }

            if (_keyboard.IsKeyPressed(Key.Q) || _keyboard.IsKeyPressed(Key.Left))
            {
                RotateCamera(new Vector3D<float>(0, rotateSpeedRad, 0));
            }
            if (_keyboard.IsKeyPressed(Key.E) || _keyboard.IsKeyPressed(Key.Right))
            {
                RotateCamera(new Vector3D<float>(0, -rotateSpeedRad, 0));
            }
            if (_keyboard.IsKeyPressed(Key.Up))
            {
                RotateCamera(new Vector3D<float>(rotateSpeedRad, 0, 0));
            }
            if (_keyboard.IsKeyPressed(Key.Down))
            {
                RotateCamera(new Vector3D<float>(-rotateSpeedRad, 0, 0));
            }

            RecalculateCameraViewMatrix();
            _renderer!.SetView(_view);
        }

        public void Render(double deltaTime)
        {
        }

        public void OnResize(Vector2D<uint> size)
        {
            _logger.LogDebug("Game resized!");
        }

        public void Shutdown()
        {
            _logger.LogDebug("Game shutdown.");
        }

        /// <summary>
        /// Keyboard pressed
        /// </summary>
        /// <param name="keyboard"></param>
        /// <param name="key"></param>
        /// <param name="arg3"></param>
        private void KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            if (key == Key.Escape)
            {
                _window!.Close();
            }
            else if(key == Key.T)
            {
                _renderer!.CycleTestTexture();
            }
        }

        private void RecalculateCameraViewMatrix()
        {
            if(!_cameraViewDirty)
            {
                return;
            }

            //todo: X and Y might need to be flipped
            var rotation = Matrix4X4.CreateFromYawPitchRoll(_cameraEuler.Y, _cameraEuler.X, _cameraEuler.Z);
            var translation = Matrix4X4.CreateTranslation(_cameraPosition);

            var view = rotation * translation;
            Matrix4X4.Invert(view, out _view); //invert to make it a proper view matrix

            _cameraViewDirty = false;
        }

        /// <summary>
        /// Rotate the camera with Yaw Pitch and Roll.
        /// </summary>
        /// <param name="amount">Yaw is Y, Pitch is X, Roll is Z.</param>
        private void RotateCamera(Vector3D<float> amount)
        {
            _cameraEuler += amount;

            var limitRad = MathUtils.ConvertDegreesToRadians(89.0);

            _cameraEuler.X = (float)Math.Clamp(_cameraEuler.X, -limitRad, limitRad);

            _cameraViewDirty = true;
        }

        private void MoveCamera(Vector3D<float> amount)
        {
            _cameraPosition += amount;
            _cameraViewDirty = true;
        }
    }
}