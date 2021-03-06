﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace SpaceGame.utility
{
    class InputManager
    {
        #region fields
        #region members
        KeyboardState previousKeyboardState;
        KeyboardState currentKeyboardState;
        MouseState previousMouseState;
        MouseState currentMouseState;
        #endregion

        #region properties

        //Input Requests
        public bool MoveLeft
        {
            get { return currentKeyboardState.IsKeyDown(Keys.A); }
        }
        public bool MoveRight 
        { 
            get {return currentKeyboardState.IsKeyDown(Keys.D);} 
        }
        public bool MoveDown 
        { 
            get {return currentKeyboardState.IsKeyDown(Keys.S);}
        }
        public bool MoveUp 
        { 
            get {return currentKeyboardState.IsKeyDown(Keys.W);}
        }

        /// <summary>
        /// Request to move selector left (use for menus)
        /// </summary>
        public bool SelectLeft
        {
            get { return keyTapped(Keys.A) || keyTapped(Keys.Left)
                || keyTapped(Keys.H); }
        }
        /// <summary>
        /// Request to move selector right (use for menus)
        /// </summary>
        public bool SelectRight 
        { 
            get { return keyTapped(Keys.D) || keyTapped(Keys.Right)
                || keyTapped(Keys.L); }
        }
        /// <summary>
        /// Request to move selector down (use for menus)
        /// </summary>
        public bool SelectDown 
        { 
            get { return keyTapped(Keys.S) || keyTapped(Keys.Down)
                || keyTapped(Keys.J); }
        }
        /// <summary>
        /// Request to move selector up (use for menus)
        /// </summary>
        public bool SelectUp 
        { 
            get { return keyTapped(Keys.W) || keyTapped(Keys.Up)
                || keyTapped(Keys.K); }
        }

        /// <summary>
        /// Confirmation button pressed (use for menus)
        /// </summary>
        public bool Confirm 
        {
            get { return keyTapped(Keys.Enter) || keyTapped(Keys.Space)
                || keyTapped(Keys.I); }
        }
        /// <summary>
        /// Cancellation/back button pressed (use for menus)
        /// </summary>
        public bool Cancel 
        {
            get { return keyTapped(Keys.Escape) || keyTapped(Keys.Back); }
        }
        /// <summary>
        /// Get requested direction based on movement keys (normalized)
        /// </summary>
        public Vector2 MoveDirection
        {
            get 
            {
                Vector2 direction = Vector2.Zero;

                if (MoveDown)
                    direction.Y = 1;
                else if (MoveUp)
                    direction.Y = -1;
                if (MoveRight)
                    direction.X = 1;
                else if (MoveLeft)
                    direction.X = -1;

                if (direction.Length() > 0)
                    direction.Normalize();
                return direction;
            }
        }
        public bool FirePrimary 
        {
            get { return currentMouseState.LeftButton == ButtonState.Pressed; }
        }
        public bool FireSecondary
        {
            get { return currentMouseState.RightButton == ButtonState.Pressed; }
        }
        public bool TriggerGadget1
        { 
            get {return (currentKeyboardState.IsKeyDown(Keys.LeftShift)
                            && previousKeyboardState.IsKeyUp(Keys.LeftShift));}
        }
        public bool TriggerGadget2 
        { 
            get {return (currentKeyboardState.IsKeyDown(Keys.Space)
                            && previousKeyboardState.IsKeyUp(Keys.Space));}
        }
        public Vector2 MouseLocation
        {
            get { return new Vector2(currentMouseState.X, currentMouseState.Y); }
        }
        public bool Exit
        {
            get { return currentKeyboardState.IsKeyDown(Keys.Escape); }
        }

        //the magical all-purpose dubugging key. Who knows what surprises it holds?
        public bool DebugKey    
        {
            get { return currentKeyboardState.IsKeyDown(Keys.B); }
        }
        #endregion
        #endregion

        #region methods
        public InputManager()
        { }

        public void Update()
        {
            previousKeyboardState = currentKeyboardState;
            currentKeyboardState = Keyboard.GetState();
            previousMouseState = currentMouseState;
            currentMouseState = Mouse.GetState();
        }

        private bool keyTapped(Keys key)
        {
            return currentKeyboardState.IsKeyDown(key)
                && previousKeyboardState.IsKeyUp(key);
        }
        #endregion
    }
}
