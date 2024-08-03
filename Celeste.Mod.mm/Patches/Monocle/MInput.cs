#pragma warning disable CS0626 // Method, operator, or accessor is marked external and has no attributes on it
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
#pragma warning disable CS0169 // The field is never used

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoMod;
using System;

namespace Monocle {
    public class patch_MInput {
        // vanilla internal field
        internal static List<VirtualInput> VirtualInputs;

        // lock VirtualInputs so that the VirtualInput class doesn't modify it while UpdateVirtualInputs() iterates it.
        private static extern void orig_UpdateVirtualInputs();
        private static void UpdateVirtualInputs() {
            lock (VirtualInputs) {
                orig_UpdateVirtualInputs();
            }
        }

        // Vanilla property
        public static patch_MouseData Mouse { get; private set; }

        public class patch_KeyboardData {
            public extern bool orig_Check(Keys key);

            public bool Check(Keys key)
                => key != Keys.None && orig_Check(key);

            public extern bool orig_Pressed(Keys key);

            public bool Pressed(Keys key)
                => key != Keys.None && orig_Pressed(key);

            public extern bool orig_Released(Keys key);

            public bool Released(Keys key)
                => key != Keys.None && orig_Released(key);
        }

        public class patch_MouseData {

            // Would be called "Buttons" but the Serializer gets mad because XNA "Buttons" also exists
            public enum MouseButtons {
                Left, Right, Middle, XButton1, XButton2
            }

            // Account for non 16:9 aspect ratios
            public Vector2 Position {
                [MonoModReplace]
                get => Vector2.Transform(new Vector2(MInput.Mouse.CurrentState.X - Engine.Viewport.X, MInput.Mouse.CurrentState.Y - Engine.Viewport.Y), Matrix.Invert(Engine.ScreenMatrix));
                [MonoModReplace]
                set {
                    Vector2 position = Vector2.Transform(value, Engine.ScreenMatrix);
                    Microsoft.Xna.Framework.Input.Mouse.SetPosition((int)Math.Round(position.X + Engine.Viewport.X), (int)Math.Round(position.Y + Engine.Viewport.Y));
                }
            }

            public bool Check(MouseButtons button) {
                return button switch {
                    MouseButtons.Left => MInput.Mouse.CheckLeftButton,
                    MouseButtons.Right => MInput.Mouse.CheckRightButton,
                    MouseButtons.Middle => MInput.Mouse.CheckMiddleButton,
                    MouseButtons.XButton1 => MInput.Mouse.CurrentState.XButton1 == ButtonState.Pressed,
                    MouseButtons.XButton2 => MInput.Mouse.CurrentState.XButton2 == ButtonState.Pressed,
                    _ => false,
                };
            }

            public bool Pressed(MouseButtons button) {
                return button switch {
                    MouseButtons.Left => MInput.Mouse.PressedLeftButton,
                    MouseButtons.Right => MInput.Mouse.PressedRightButton,
                    MouseButtons.Middle => MInput.Mouse.PressedMiddleButton,
                    MouseButtons.XButton1 => MInput.Mouse.CurrentState.XButton1 == ButtonState.Pressed &&
                        MInput.Mouse.PreviousState.XButton1 == ButtonState.Released,
                    MouseButtons.XButton2 => MInput.Mouse.CurrentState.XButton2 == ButtonState.Pressed &&
                        MInput.Mouse.PreviousState.XButton2 == ButtonState.Released,
                    _ => false,
                };
            }

            public bool Released(MouseButtons button) {
                return button switch {
                    MouseButtons.Left => MInput.Mouse.ReleasedLeftButton,
                    MouseButtons.Right => MInput.Mouse.ReleasedRightButton,
                    MouseButtons.Middle => MInput.Mouse.ReleasedMiddleButton,
                    MouseButtons.XButton1 => MInput.Mouse.CurrentState.XButton1 == ButtonState.Released &&
                        MInput.Mouse.PreviousState.XButton1 == ButtonState.Pressed,
                    MouseButtons.XButton2 => MInput.Mouse.CurrentState.XButton2 == ButtonState.Released &&
                        MInput.Mouse.PreviousState.XButton2 == ButtonState.Pressed,
                    _ => false,
                };
            }
        }
    }
}