﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SpaceGame.graphics;
using SpaceGame.utility;
using SpaceGame.units;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceGame.equipment
{
    class ProjectileWeapon : Weapon
    {
        #region static
        public static Dictionary<string, ProjectileWeaponData> ProjectileWeaponData;

        protected class Projectile
        {
            public bool Active;
            //Splashing and REadyToSplash are used to ensure that a splash effect hits each in range enemy once
            //When a projectile hits, ReadyToSplash is set. When update is called after this hit, 
            //Splashing is also set. The next series of hit checks, the splash effect is applied.
            //The next update, ReadyToSplash is set to false, so splash damage checks are no longer applied
            public bool Splashing;  //if particle is exploding & applying splash effect
            public bool ReadyToSplash;  //if projectile has hit a unit and is ready to apply splash next update
            public Vector2 Position;
            public Vector2 Velocity;
            public TimeSpan LifeLeft;
            public float Angle;     //in radians
            public Sprite ProjectileSprite;
        }
        #endregion

        #region fields
        int _maxProjectiles;    //total number of on-screen projectiles allowable
        protected Projectile[] _projectiles;
        int _projectilesPerFire;

        int _projectileDamage;
        float _projectileForce;
        float _recoilForce; 
        float _projectileSpeed;
        float _projectileSpread;
        float _projectileAcceleration;
        bool _dissipateOnHit;   //if true, the projectile dissipates after hitting a target
        TimeSpan _projectileLife;

        //only applicable for particles with splash effects
        float _splashRadius, _splashForce;
        int _splashDamage;
        //how much to scale sprite each second when splashing
        float _splashScaleRate;

        Rectangle _hitDetectionRect;    //width and height based on texture, x and y change
        Rectangle _splashDetectionRect;    //width and height based on _splashRadius, x and y change

        ParticleEffect _movementParticleEffect;
        ParticleEffect _splashParticleEffect;
        #endregion

        #region constructor
        public ProjectileWeapon(string weaponName, PhysicalUnit owner)
            : this(ProjectileWeaponData[weaponName], owner)
        { }

        protected ProjectileWeapon(ProjectileWeaponData data, PhysicalUnit owner)
            :base(TimeSpan.FromSeconds(1.0 / data.FireRate), 
                  data.MaxAmmo,
                  data.AmmoConsumption,
                  owner)
        {
            _maxProjectiles = data.MaxProjectiles;
            _projectiles = new Projectile[_maxProjectiles];
            for (int i = 0; i < _maxProjectiles; i++)
            {
                _projectiles[i] = new Projectile();
                _projectiles[i].ProjectileSprite = new Sprite(data.ProjectileSpriteName);
            }
            _projectileDamage = data.Damage;
            _projectileForce = data.ProjectileForce;
            _recoilForce = data.Recoil;
            _projectileSpeed = data.ProjectileSpeed;
            _projectileAcceleration = data.ProjectileAcceleration;
            _dissipateOnHit = data.DissipateOnHit;
            _projectileSpread = MathHelper.ToRadians(data.ProjectileSpread);
            _projectilesPerFire = data.ProjectilesPerFire;
            _projectileLife = data.ProjectileLife;

            _splashDamage = data.SplashDamage;
            _splashRadius = data.SplashRadius;
            _splashForce = data.SplashForce;

            Sprite projectileSprite = _projectiles[0].ProjectileSprite;

            if (projectileSprite.Width <= _splashRadius * 2)
            {
                _splashScaleRate = (projectileSprite.Width / _splashRadius);
                _splashScaleRate /= (float)projectileSprite.FullAnimationTime.TotalSeconds;
            }

            _hitDetectionRect = new Rectangle(0,0, 
               (int)(projectileSprite.Width), (int)projectileSprite.Height);

            if (data.MovementParticleEffect != null)
                _movementParticleEffect = new ParticleEffect(data.MovementParticleEffect);
            if (data.SplashParticleEffect != null)
                _splashParticleEffect = new ParticleEffect(data.SplashParticleEffect);
            
        }
        #endregion

        #region methods
        public override void CheckAndApplyCollision(units.PhysicalUnit unit)
        {
            Projectile p;
            for (int i = 0 ; i < _projectiles.Length ; i++)
            {
                p = _projectiles[i];
                if (_projectiles[i].Active)
                {
                    _hitDetectionRect.X = (int)_projectiles[i].Position.X;
                    _hitDetectionRect.Y = (int)_projectiles[i].Position.Y;
                    if (XnaHelper.RectsCollide(unit.HitRect, _hitDetectionRect))
                    {
                        applyProjectileHit(_projectiles[i], unit);
                    }
                }
                if (_projectiles[i].Splashing && _projectiles[i].ReadyToSplash)
                {
                    _splashDetectionRect.X = (int)_projectiles[i].Position.X;
                    _splashDetectionRect.Y = (int)_projectiles[i].Position.Y;
                    _splashDetectionRect.Width = (int)(_splashRadius * 2);
                    _splashDetectionRect.Height = (int)(_splashRadius * 2);
                    if (XnaHelper.RectsCollide(unit.HitRect, _splashDetectionRect))
                    {
                        unit.ApplyDamage(_splashDamage);
                        unit.ApplyForce(_splashForce * XnaHelper.DirectionBetween(_projectiles[i].Position, unit.Center));
                    }

                }
            }
        }

        protected virtual void applyProjectileHit(Projectile p, PhysicalUnit unit)
        {
            unit.ApplyForce(p.Velocity / p.Velocity.Length() * _projectileForce);
            unit.ApplyDamage(_projectileDamage);
            if (_dissipateOnHit)
                p.Active = false;
            if (_splashRadius > 0)
            {
                p.ReadyToSplash = true;
                p.Velocity = Vector2.Zero;
                p.ProjectileSprite.PlayAnimation(1);
            }
        }

        protected override void UpdateWeapon(GameTime gameTime)
        {
            int projectilesToSpawn = _firing ? _projectilesPerFire : 0;
            Projectile p;
            for (int i = 0; i < _projectiles.Length; i++)
            {
                p = _projectiles[i];
                TimeSpan time = gameTime.ElapsedGameTime;

                if (p.ReadyToSplash && p.Splashing)
                    p.ReadyToSplash = false;

                p.Splashing = p.ReadyToSplash || p.Splashing;

                if (p.Active)
                {
                    //updateProjectile(p, gameTime.ElapsedGameTime);
                    if (p.Velocity.Length() != 0)
                    {
                        p.Velocity += p.Velocity / p.Velocity.Length() * _projectileAcceleration;
                    }
                    p.Position += p.Velocity * (float)time.TotalSeconds;
                    p.LifeLeft -= time;
                    p.ProjectileSprite.Update(gameTime);
                    if (_movementParticleEffect != null)
                        _movementParticleEffect.Spawn(p.Position, MathHelper.ToDegrees(MathHelper.Pi + p.Angle), gameTime.ElapsedGameTime, Vector2.Zero);
                    if (p.LifeLeft <= TimeSpan.Zero || !(XnaHelper.PointInRect(p.Position, ScreenBounds)))
                        p.Active = false;
                }

                if (p.Splashing)
                {
                    p.ProjectileSprite.Update(gameTime);
                    p.Splashing = !(p.ProjectileSprite.AnimationOver);
                    p.ProjectileSprite.ScaleFactor += _splashScaleRate * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    if (_splashParticleEffect != null)
                        _splashParticleEffect.Spawn(p.Position, MathHelper.ToDegrees(p.Angle), gameTime.ElapsedGameTime, Vector2.Zero);
                }
                else if (!p.Active)
                {
                    if (projectilesToSpawn > 0)
                    {
                        //initializeProjectile(p, _fireDirection);

                        p.Active = true;
                        p.Splashing = false;
                        p.ReadyToSplash = false;
                        p.Angle = XnaHelper.RadiansFromVector(_fireDirection);
                        p.Angle = XnaHelper.RandomAngle(p.Angle, _projectileSpread);
                        p.LifeLeft = _projectileLife;
                        p.Position = _owner.Center;
                        p.Velocity = XnaHelper.VectorFromAngle(p.Angle) * _projectileSpeed;
                        projectilesToSpawn -= 1;
                        p.ProjectileSprite.Reset();
                        _owner.ApplyForce(-_recoilForce * _fireDirection);
                    }
                }

            }

            if (_movementParticleEffect != null)
                _movementParticleEffect.Update(gameTime);
            if (_splashParticleEffect != null)
                _splashParticleEffect.Update(gameTime);
        }

        protected virtual void initializeProjectile(Projectile p, Vector2 fireDirection)
        {
            p.Active = true;
            p.Angle = XnaHelper.RadiansFromVector(fireDirection);
            p.LifeLeft = _projectileLife;
            p.Position = _owner.Center;
            p.Velocity = fireDirection * _projectileSpeed;
            p.ProjectileSprite.Reset();
        }

        public override void Draw(SpriteBatch sb)
        {
            if (_movementParticleEffect != null)
                _movementParticleEffect.Draw(sb);
            if (_splashParticleEffect != null)
                _splashParticleEffect.Draw(sb);

            foreach (Projectile p in _projectiles)
            {
                if (p.Active || p.Splashing)
                {
                    p.ProjectileSprite.Draw(sb, p.Position, p.Angle);
                }
            }
        }
        #endregion
    }
}
