﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using SpaceGame.graphics;
using SpaceGame.units;
using SpaceGame.utility;

namespace SpaceGame.equipment
{
    abstract class Weapon
    {
        #region static
        public static Rectangle ScreenBounds;
        #endregion

        #region fields
        //minimum time between shots and till next shot
        TimeSpan _fireDelay, _tillNextFire;
        //maximum possible ammo, current ammo, and ammo use per Fire
        int _maxAmmo, _currentAmmo, _ammoConsumption;

        //whether the weapon is firing, and if so, what direction
        //set during Weapon.Trigger
        //check and apply during weapon.Update()
        protected bool _firing;
        protected Vector2 _fireDirection;

        protected PhysicalUnit _owner;
        #endregion

        #region constructor
        /// <summary>
        /// Create a new weapon
        /// </summary>
        /// <param name="fireDelay">Time between successive shots</param>
        /// <param name="maxAmmo">Total ammo capacity. Set to 1 for infinite ammo.</param>
        /// <param name="ammoConsumption">Ammo used per shot. Set to 0 for infinite ammo.</param>
        public Weapon(TimeSpan fireDelay, int maxAmmo, int ammoConsumption, PhysicalUnit owner)
        {
            _fireDelay = fireDelay;
            _maxAmmo = maxAmmo;
            _currentAmmo = maxAmmo;
            _ammoConsumption = ammoConsumption;
            _owner = owner;
        }
        #endregion

        #region concrete methods
        /// <summary>
        /// Attempt to fire a weapon.
        /// Only fires if enough time has passed since the last fire and enough ammo is available
        /// </summary>
        /// <param name="firePosition"></param>
        /// <param name="targetPosition"></param>
        public void Trigger(Vector2 firePosition, Vector2 targetPosition)
        {
            if (_currentAmmo >= _ammoConsumption && _tillNextFire.TotalSeconds <= 0)
            {
                _firing = true;
                _fireDirection = XnaHelper.DirectionBetween(firePosition, targetPosition);

                _currentAmmo -= _ammoConsumption;
                _tillNextFire = _fireDelay;
            }
        }

        public void Update(GameTime gameTime)
        {
            _tillNextFire -= gameTime.ElapsedGameTime;
            UpdateWeapon(gameTime);
            _firing = false;
        }
        #endregion

        #region abstract methods
        /// <summary>
        /// Check if weapon is hitting a target, and apply its affects if so
        /// Call during the update loop on each unit
        /// </summary>
        /// <param name="unit"></param>
        public abstract void CheckAndApplyCollision(PhysicalUnit unit);
        //update projectiles and add new projectiles if _firing
        protected abstract void UpdateWeapon(GameTime gameTime);
        public abstract void Draw(SpriteBatch sb);
        #endregion
    }
}
