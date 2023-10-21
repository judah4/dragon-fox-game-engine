using DragonGameEngine.Core;
using Microsoft.Extensions.Logging;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.SDL;
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

        private GameApplication? _gameApp;
        IKeyboard? _keyboard;
        IGamepad? _gamepad;

        //temp until camera
        private Matrix4X4<float> _view;
        private Vector3D<float> _cameraPosition;
        private Vector3D<float> _cameraEuler;
        private bool _cameraViewDirty;

        //Used to track change in mouse movement to allow for moving of the Camera
        private Vector2D<float> _lastMousePosition;
        private float _lastDeltaTime;
        private const float THUMBSTICK_DEADZONE = 0.01f;

        public GameEntry(ILogger logger)
        {
            _logger = logger;
        }

        public void Initialize(GameApplication gameApp)
        {
            _gameApp = gameApp;
            IInputContext input = _gameApp.Window.CreateInput();
            _keyboard = input.Keyboards.FirstOrDefault();
            _gamepad = input.Gamepads.FirstOrDefault();

            if(_keyboard != null)
            {
                _keyboard.KeyDown += KeyDown;
            }
            for (int i = 0; i < input.Mice.Count; i++)
            {
                input.Mice[i].Cursor.CursorMode = CursorMode.Normal;
                input.Mice[i].MouseMove += OnMouseMove;
            }
            //so much temp input

            SetDefaultCameraPosition();

            _logger.LogDebug("Game initialized!");
        }

        public void Update(double deltaTime)
        {
            _lastDeltaTime = (float)deltaTime;
            var rotateSpeedRad = 2.5f * (float)deltaTime;

            var moveSpeed = 5f * (float)deltaTime;

            var velocity = Vector3D<float>.Zero;
            if(_keyboard != null)
            {
                if (_keyboard.IsKeyPressed(Key.W))
                {
                    var forward = MathUtils.ForwardFromMatrix(_view);
                    velocity += forward;
                }
                if (_keyboard.IsKeyPressed(Key.S))
                {
                    var backward = -MathUtils.ForwardFromMatrix(_view);
                    velocity += backward;
                }
                if (_keyboard.IsKeyPressed(Key.A))
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
            }

            if (_gamepad != null && Math.Abs(_gamepad.Thumbsticks[0].Y) > THUMBSTICK_DEADZONE)
            {
                //pad is inverted I guess
                var forward = MathUtils.ForwardFromMatrix(_view);
                velocity += (forward * -_gamepad.Thumbsticks[0].Y);
            }

            if (_gamepad != null && Math.Abs(_gamepad.Thumbsticks[0].X) > THUMBSTICK_DEADZONE)
            {
                //pad is inverted I guess
                var right = MathUtils.RightFromMatrix(_view);
                velocity += (right * _gamepad.Thumbsticks[0].X);
            }


            if(_gamepad != null && _gamepad.Triggers.Count >= 2)
            {
                if (_gamepad.Triggers[1].Position > THUMBSTICK_DEADZONE)
                {
                    velocity.Y += _gamepad.Triggers[1].Position;
                }
                if (_gamepad.Triggers[0].Position > THUMBSTICK_DEADZONE)
                {
                    velocity.Y -= _gamepad.Triggers[0].Position;
                }
            }

            if (velocity != Vector3D<float>.Zero)
            {
                MoveCamera(velocity * moveSpeed);
            }

            if (_keyboard != null)
            {
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
            }
            if (_gamepad != null && _gamepad.Thumbsticks[1].Position > THUMBSTICK_DEADZONE)
            {
                var thumbstick = _gamepad.Thumbsticks[1];

                var xOffset = thumbstick.X * rotateSpeedRad;
                var yOffset = thumbstick.Y * rotateSpeedRad;
                RotateCamera(new Vector3D<float>(-yOffset, -xOffset, 0));
            }

            RecalculateCameraViewMatrix();
            _gameApp!.Renderer.SetView(_view);

            //debug set camera position in title
            _gameApp.UpdateWindowTitle($"Pos:{_cameraPosition}");
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
                _gameApp!.Window.Close();
            }
            else if(key == Key.T)
            {
                _gameApp!.CycleTestTexture();
            }
            else if (key == Key.R)
            {
                SetDefaultCameraPosition();
                _logger.LogDebug("Camera position reset.");
            }
        }

        private void OnMouseMove(IMouse mouse, System.Numerics.Vector2 position)
        {
            var rotateSpeedRad = 1.5f * _lastDeltaTime;

            var mousePos = new Vector2D<float>(position.X, position.Y);
            if (_lastMousePosition == default)
            {
                _lastMousePosition = mousePos;
            }
            else
            {
                var xOffset = (position.X - _lastMousePosition.X) * rotateSpeedRad;
                var yOffset = (position.Y - _lastMousePosition.Y) * rotateSpeedRad;
                _lastMousePosition = mousePos;
                if(mouse.IsButtonPressed(MouseButton.Right))
                {
                    mouse.Cursor.CursorMode = CursorMode.Raw;
                    RotateCamera(new Vector3D<float>(-yOffset, -xOffset, 0));
                }
                else
                {
                    //this needs to be in state at some point
                    mouse.Cursor.CursorMode = CursorMode.Normal;
                }
            }
        }

        private void RecalculateCameraViewMatrix()
        {
            if(!_cameraViewDirty)
            {
                return;
            }

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


        private void SetDefaultCameraPosition()
        {
            _cameraPosition = new Vector3D<float>(0, 0, 10);
            _cameraEuler = Vector3D<float>.Zero;
            _cameraViewDirty = true;
        }
    }
}