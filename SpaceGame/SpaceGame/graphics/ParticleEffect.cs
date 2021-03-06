using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

using SpaceGame.utility;

namespace SpaceGame.graphics
{
    public class ParticleEffect
    {
        #region static
        //stores all data for particle effects
        public static Dictionary<string, ParticleEffectData> Data;

        static Texture2D particleTexture;
        //default texture to draw particles with. Hardcoded single pixel assigned in Game.LoadContent
        public static Texture2D ParticleTexture
        {
            get { return particleTexture; }
            set
            { 
                particleTexture = value;
            }
        }


        static Random rand = new Random();
        #endregion

        #region fields
        //range of angles through which particles can be spawned, in degrees
        float _arc;
        //speed with which particles are spawned, and random variance factor
        float _particleSpeed, _speedVariance;
        //fraction of speed that is reduced each second
        float _particleDecelerationFactor;
        //time particle exists
        TimeSpan _particleLife;
        //percent variance in life of particles
        float _particleLifeVariance;
        //how many particles to spawn per second 
        int _spawnRate;
        //time till spawning another particle
        TimeSpan _tillNextParticleSpawn;
        //starting scale of particles, percent variance in starting scale, and increase in scale per second
        float _particleScale, _scaleVariance, _scaleRate;
        //rotation, in radians per second
        float _particleRotationSpeed;
        //starting color, andchange in color per second, represented as 4-vectors
        Color _startColor, _endColor;
        List<Particle> _particles;

        Vector2 _textureCenter;
        Texture2D _particleTexture;

        ParticleEffectData _particleEffectData;

        float _speedFactor;
        #endregion

        #region properties
        public bool Reversed { get; set; }
        public float IntensityFactor 
        {
            get { return _speedFactor; }
            set
            {
                _speedFactor = value;
                _particleSpeed = _particleEffectData.Speed * IntensityFactor;
                //_particleLife = TimeSpan.FromSeconds((float)_particleEffectData.ParticleLife.TotalSeconds / IntensityFactor);
                _scaleRate =
                    (((_particleEffectData.EndScale - _particleEffectData.StartScale) / _particleTexture.Width)
                    / ((float)_particleLife.TotalSeconds));
                _spawnRate = (int)((float)_particleEffectData.SpawnRate * IntensityFactor);
            }
        }
        #endregion

        class Particle
        {
            public Vector2 Position, Velocity;
            public float Scale, Angle;     //size and rotation(radians)
            public TimeSpan LifeTime, TimeAlive;        //How many seconds the particle should exist and has existed
        }

        /// <summary>
        /// Create a new particle effect, based on parameters stored in ParticleEffectData.xml
        /// </summary>
        /// <param name="effectKey">string identifier used to fetch parameters. Must match Name attribute in XML</param>
        public ParticleEffect(string effectKey)
        {
            _particleEffectData = Data[effectKey];
            _particleSpeed = _particleEffectData.Speed;
            _speedVariance = _particleEffectData.SpeedVariance;
            _particleDecelerationFactor = _particleEffectData.DecelerationFactor;

            _particleTexture = (_particleEffectData.UniqueParticle == null) ? particleTexture : _particleEffectData.UniqueParticle;
            _textureCenter = new Vector2(_particleTexture.Width / 2.0f, particleTexture.Height / 2.0f); 

            _particleScale = _particleEffectData.StartScale / _particleTexture.Width;
            _scaleRate = 
                (((_particleEffectData.EndScale - _particleEffectData.StartScale) / _particleTexture.Width) 
                / ((float)_particleEffectData.ParticleLife.TotalSeconds));
            _scaleVariance = _particleEffectData.ScaleVariance;
            _arc = _particleEffectData.SpawnArc;
            _particleLife = _particleEffectData.ParticleLife;
            _particleLifeVariance = _particleEffectData.ParticleLifeVariance;
            _particleRotationSpeed = MathHelper.ToRadians(
                _particleEffectData.ParticleRotation / (float)_particleEffectData.ParticleLife.TotalSeconds);
            if (_particleEffectData.Reversed)
            {
                _startColor = _particleEffectData.EndColor;
                _endColor = _particleEffectData.StartColor;
                Reversed = true;
            }
            else
            {
                _startColor = _particleEffectData.StartColor;
                _endColor = _particleEffectData.EndColor;
                Reversed = false;
            }

            _spawnRate = _particleEffectData.SpawnRate;
            _tillNextParticleSpawn = TimeSpan.FromSeconds(1.0f / (float)_spawnRate);
            _particles = new List<Particle>();
            IntensityFactor = 1.0f;
        }

        public void Update(GameTime gameTime)
        {
            for (int i = _particles.Count - 1 ; i >= 0 ; i--)
            {
                Particle particle = _particles[i];
                if (particle.LifeTime < particle.TimeAlive)
                    _particles.RemoveAt(i);
                else
                {
                    //reduce life
                    particle.TimeAlive += gameTime.ElapsedGameTime;
                    //move particle
                    particle.Position += particle.Velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    //scale down velocity
                    particle.Velocity -= particle.Velocity * _particleDecelerationFactor * (float)gameTime.ElapsedGameTime.TotalSeconds;

                    if (Reversed)
                    {
                        //rotate particle
                        particle.Angle -= _particleRotationSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
                        //adjust scale
                        particle.Scale -= _scaleRate * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    }
                    else
                    {
                        //rotate particle
                        particle.Angle += _particleRotationSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
                        //adjust scale
                        particle.Scale += _scaleRate * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    }
                }
            }
        }

        private Particle newParticle(Vector2 pos, float angle, Vector2 sourceVelocity)
        {
            Particle particle = new Particle();
            particle.Position = pos;
            float directionAngle = (float)MathHelper.ToRadians((XnaHelper.RandomAngle(angle, _arc)));
            float speed = applyVariance(_particleSpeed, _speedVariance);
            particle.Velocity = speed * XnaHelper.VectorFromAngle(directionAngle) + sourceVelocity;
            particle.Scale = applyVariance(_particleScale, _scaleVariance);
            particle.Angle = 0.0f;
            particle.LifeTime = TimeSpan.FromSeconds(applyVariance((float)_particleLife.TotalSeconds, _particleLifeVariance));

            if (Reversed)
            {
                float secondsAlive = (float)particle.LifeTime.TotalSeconds;
                //start at the end
                particle.Position = particle.Position + particle.Velocity * secondsAlive; 
                //comment above and uncomment below for a cool effect (unintentional side effect while working on particles.
                //not sure why it looks so awesome, but it does)
                //particle.Position = particle.Position + particle.Velocity * secondsAlive * (1 - _particleDecelerationFactor);

                //movce in reverse
                particle.Velocity = Vector2.Negate(particle.Velocity);
                //start at end scale
                particle.Scale = _particleScale + _scaleRate * secondsAlive;
                //start at end rotation
                particle.Angle = _particleRotationSpeed * secondsAlive;
            }
            return particle;
        }

        /// <summary>
        /// Spawn new particles
        /// </summary>
        /// <param name="position">Location at which to spawn particles</param>
        /// <param name="angle">direction at which to spawn particles (degrees)</param>
        /// <param name="sourceVeloctiy">Velocity of particle source, added to all particles</param>
        /// <param name="time"></param>
        public void Spawn(Vector2 position, float angle, TimeSpan time, Vector2 sourceVelocity)
        {
            Spawn(position, angle, time, sourceVelocity, 1.0f);
        }

        /// <summary>
        /// Spawn new particles
        /// </summary>
        /// <param name="position">Location at which to spawn particles</param>
        /// <param name="angle">direction at which to spawn particles (degrees)</param>
        /// <param name="sourceVeloctiy">Velocity of particle source, added to all particles</param>
        /// <param name="time"></param>
        /// <param name="multiplier">Multiplier to apply to default spawn rate </param>
        public void Spawn(Vector2 position, float angle, TimeSpan time, Vector2 sourceVelocity, float multiplier)
        {
            //fractional number of particles to spawn
            float particlesToSpawn = multiplier * (float)(_spawnRate * (float)time.TotalSeconds);            
            //spawn integer number of particles
            for(int i = 0 ; i < (int)particlesToSpawn ; i++)
            {
                _particles.Add(newParticle(position, angle, sourceVelocity));
            }
            //now deal with fractional part
            _tillNextParticleSpawn -= TimeSpan.FromSeconds((double)(particlesToSpawn - (int)particlesToSpawn));
            if (_tillNextParticleSpawn < TimeSpan.Zero)
            {
                _particles.Add(newParticle(position, angle, sourceVelocity));
                _tillNextParticleSpawn = TimeSpan.FromSeconds(1.0f / (float)_spawnRate);
            }
        }

        private float applyVariance(float baseFloat, float variance)
        {
            return baseFloat + baseFloat * variance * (1.0f - 2 * (float)rand.NextDouble());
        }

        public void Draw(SpriteBatch sb)
        {

            foreach (Particle p in _particles)
            {
                Color drawColor;
                if (Reversed)
                    drawColor = Color.Lerp(_endColor, _startColor, (float)p.TimeAlive.TotalSeconds / (float)p.LifeTime.TotalSeconds);
                else
                    drawColor = Color.Lerp(_startColor, _endColor, (float)p.TimeAlive.TotalSeconds / (float)p.LifeTime.TotalSeconds);

                sb.Draw(_particleTexture, p.Position, null, drawColor, p.Angle, _textureCenter, p.Scale, SpriteEffects.None, 0 );
            }
        }

        public void Draw(SpriteBatch sb, Vector2 origin)
        {

            foreach (Particle p in _particles)
            {
                Color drawColor;
                if (Reversed)
                    drawColor = Color.Lerp(_endColor, _startColor, (float)p.TimeAlive.TotalSeconds / (float)p.LifeTime.TotalSeconds);
                else
                    drawColor = Color.Lerp(_startColor, _endColor, (float)p.TimeAlive.TotalSeconds / (float)p.LifeTime.TotalSeconds);

                sb.Draw(_particleTexture, p.Position, null, drawColor, p.Angle, origin - p.Position + _textureCenter, p.Scale, SpriteEffects.None, 0 );
            }
        }

    }
}
