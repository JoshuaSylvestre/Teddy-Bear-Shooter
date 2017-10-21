using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GameProject
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        // game objects. Using inheritance would make this
        // easier, but inheritance isn't a GDD 1200 topic
        Burger burger;
        List<TeddyBear> bears = new List<TeddyBear>();
        static List<Projectile> projectiles = new List<Projectile>();
        List<Explosion> explosions = new List<Explosion>();

        // projectile and explosion sprites. Saved so they don't have to
        // be loaded every time projectiles or explosions are created
        static Texture2D frenchFriesSprite;
        static Texture2D teddyBearProjectileSprite;
        static Texture2D explosionSpriteStrip;

        // scoring support
        int score = 0;
        string scoreString = GameConstants.ScorePrefix + 0;

        // health support
        string healthString = GameConstants.HealthPrefix +
               GameConstants.BurgerInitialHealth;
        bool burgerDead = false;

        // text display support
        SpriteFont font;

        // sound effects
        SoundEffect burgerDamage;
        SoundEffect burgerDeath;
        SoundEffect burgerShot;
        SoundEffect explosion;
        SoundEffect teddyBounce;
        SoundEffect teddyShot;

        public Game()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            // set resolution
            graphics.PreferredBackBufferWidth = GameConstants.WindowWidth;
            graphics.PreferredBackBufferHeight = GameConstants.WindowHeight;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            RandomNumberGenerator.Initialize();

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);


            // load audio content

            burgerDamage = Content.Load<SoundEffect>(@"audio/BurgerDamage");
            burgerDeath = Content.Load<SoundEffect>(@"audio/BurgerDeath");
            burgerShot = Content.Load<SoundEffect>(@"audio/BurgerShot");
            explosion = Content.Load<SoundEffect>(@"audio/Explosion");
            teddyBounce = Content.Load<SoundEffect>(@"audio/TeddyBounce");
            teddyShot = Content.Load<SoundEffect>(@"audio/TeddyShot");

            // load sprite font
            font = Content.Load<SpriteFont>(@"fonts/Arial20");

            // load projectile and explosion sprites
            frenchFriesSprite = Content.Load<Texture2D>(@"graphics/frenchfries");
            teddyBearProjectileSprite = Content.Load<Texture2D>(@"graphics/teddybearprojectile");
            explosionSpriteStrip = Content.Load<Texture2D>(@"graphics/explosion");

            // add initial game objects
            burger = new Burger(Content, @"graphics/burger" , Window.ClientBounds.Width/2, Window.ClientBounds.Height/8*7, burgerShot);

            for (var i = 0; i < GameConstants.MaxBears; i++)
            {
                SpawnBear();
            }

            // set initial health and score strings
            healthString = GameConstants.HealthPrefix + burger.BurgerHealth;
            scoreString = GameConstants.ScorePrefix + score;
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // get current mouse state and update burger
            KeyboardState keyboard = Keyboard.GetState();
            burger.Update(gameTime, keyboard);

            // update other game objects
            foreach (TeddyBear bear in bears)
            {
                bear.Update(gameTime);
            }
            foreach (Projectile projectile in projectiles)
            {
                projectile.Update(gameTime);
            }
            foreach (Explosion explosion in explosions)
            {
                explosion.Update(gameTime);
            }

            // check and resolve collisions between teddy bears
            for(var i = 0; i < bears.Count; i++)
            {
                for(var j = i + 1; j < bears.Count; j++)
                {
                    if(bears[i].Active == true && bears[j].Active == true)
                    {
                        var bearCollision = CollisionUtils.CheckCollision((int)gameTime.ElapsedGameTime.Milliseconds, 
                            GameConstants.WindowWidth, GameConstants.WindowHeight,
                            bears[i].Velocity, bears[i].DrawRectangle, bears[j].Velocity, bears[j].DrawRectangle);

                        if(bearCollision != null)
                        {
                            teddyBounce.Play();

                            if(bearCollision.FirstOutOfBounds == true)
                            {
                                bears[i].Active = false;
                            }
                            else
                            {
                                bears[i].Velocity = bearCollision.FirstVelocity;
                                bears[i].DrawRectangle = bearCollision.FirstDrawRectangle;
                            }

                            if(bearCollision.SecondOutOfBounds == true)
                            {
                                bears[j].Active = false;
                            }
                            else
                            {
                                bears[j].Velocity = bearCollision.SecondVelocity;
                                bears[j].DrawRectangle = bearCollision.SecondDrawRectangle;
                            }
                        }
                    }
                }
            }

            // check and resolve collisions between burger and teddy bears
            foreach(TeddyBear teddy in bears)
            {
                if(teddy.Active == true && teddy.CollisionRectangle.Intersects(burger.CollisionRectangle) == true)
                {
                    burger.BurgerHealth -= GameConstants.BearDamage;
                    teddy.Active = false;
                    explosions.Add(new Explosion(explosionSpriteStrip, teddy.CollisionRectangle.Center.X, teddy.CollisionRectangle.Center.Y, explosion));
                    healthString = GameConstants.HealthPrefix + burger.BurgerHealth;
                    burgerDamage.Play();
                    CheckBurgerKill();
                }
            }

            // check and resolve collisions between burger and projectiles
            foreach(Projectile projectile in projectiles)
            {
                if(projectile.Type == ProjectileType.TeddyBear
                    && projectile.Active == true 
                    && projectile.CollisionRectangle.Intersects(burger.CollisionRectangle))
                {
                    projectile.Active = false;
                    burger.BurgerHealth -= GameConstants.BearDamage;
                    healthString = GameConstants.HealthPrefix + burger.BurgerHealth;
                    burgerDamage.Play();
                    CheckBurgerKill();
                }
            }


            // check and resolve collisions between teddy bears and projectiles
            foreach(TeddyBear teddy in bears)
            {
                foreach(Projectile projectile in projectiles)
                {
                    if (projectile.Type == ProjectileType.FrenchFries && projectile.CollisionRectangle.Intersects(teddy.CollisionRectangle))
                    {
                        teddy.Active = false;
                        projectile.Active = false;
                        explosions.Add(new Explosion(explosionSpriteStrip, teddy.CollisionRectangle.Center.X, teddy.CollisionRectangle.Center.Y, explosion));
                        score += GameConstants.BearPoints;
                        scoreString = GameConstants.ScorePrefix + score;
                    }
                }
            }

            // clean out inactive teddy bears and add new ones as necessary
            for(var i = bears.Count - 1; i >= 0; i--)
            {
                if(bears[i].Active == false)
                {
                    bears.RemoveAt(i);
                }
            }
            // clean out inactive projectiles
            for(var i = projectiles.Count-1; i >= 0; i--)
            {
                if(projectiles[i].Active == false)
                {
                    projectiles.RemoveAt(i);
                }
            }
            // clean out finished explosions
            for(var i = explosions.Count - 1; i >= 0; i--)
            {
                if(explosions[i].Finished == true)
                {
                    explosions.RemoveAt(i);
                }
            }
            
            while(bears.Count < GameConstants.MaxBears)
            {
                SpawnBear();
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();

            // draw game objects
            burger.Draw(spriteBatch);
            foreach (TeddyBear bear in bears)
            {
                bear.Draw(spriteBatch);
            }
            foreach (Projectile projectile in projectiles)
            {
                projectile.Draw(spriteBatch);
            }
            foreach (Explosion explosion in explosions)
            {
                explosion.Draw(spriteBatch);
            }

            // draw score and health
            spriteBatch.DrawString(font, healthString, GameConstants.HealthLocation, Color.White);
            spriteBatch.DrawString(font, scoreString, GameConstants.ScoreLocation, Color.White);

            spriteBatch.End();

            base.Draw(gameTime);
        }

        #region Public methods

        /// <summary>
        /// Gets the projectile sprite for the given projectile type
        /// </summary>
        /// <param name="type">the projectile type</param>
        /// <returns>the projectile sprite for the type</returns>
        public static Texture2D GetProjectileSprite(ProjectileType type)
        {
            // replace with code to return correct projectile sprite based on projectile type
            if (type == ProjectileType.FrenchFries)
                return frenchFriesSprite;
            else if (type == ProjectileType.TeddyBear)
                return teddyBearProjectileSprite;

            return frenchFriesSprite;
        }

        /// <summary>
        /// Adds the given projectile to the game
        /// </summary>
        /// <param name="projectile">the projectile to add</param>
        public static void AddProjectile(Projectile projectile)
        {
            projectiles.Add(projectile);
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Spawns a new teddy bear at a random location
        /// </summary>
        private void SpawnBear()
        {
            // generate random locations for X and Y, both outside of the spawn border sizes
            int randomLocationX = GetRandomLocation(GameConstants.SpawnBorderSize,
                Window.ClientBounds.Right - GameConstants.SpawnBorderSize);
            int randomLocationY = GetRandomLocation(GameConstants.SpawnBorderSize, 
                Window.ClientBounds.Bottom - GameConstants.SpawnBorderSize);

            // generate random velocity up to the speed range
            float speed = GameConstants.MinBearSpeed + RandomNumberGenerator.NextFloat(GameConstants.BearSpeedRange);
            
            //generate a random angle in radians
            float randomRadian = RandomNumberGenerator.NextFloat((float)Math.PI);

            //generates random velocity. Cosine is used to get negative or positve angles. Sine is used to get positive angles.
            //There is a minimum speed added to both x and y to assure the bear travels
            Vector2 bearVelocity = new Vector2((float)Math.Cos(randomRadian)*speed + GameConstants.MinBearSpeed, 
                ((float)Math.Sin(randomRadian)*speed) + GameConstants.MinBearSpeed);


            // create new bear
            TeddyBear newbear = new TeddyBear(Content, @"graphics/teddybear", 
                randomLocationX, randomLocationY, bearVelocity, teddyBounce, teddyShot);

            // make sure we don't spawn into a collision
            while(CollisionUtils.IsCollisionFree(newbear.CollisionRectangle, GetCollisionRectangles()) == false)
            {
                newbear.X = GetRandomLocation(GameConstants.SpawnBorderSize, Window.ClientBounds.Right - GameConstants.SpawnBorderSize);
                newbear.Y = GetRandomLocation(GameConstants.SpawnBorderSize, Window.ClientBounds.Bottom - GameConstants.SpawnBorderSize);
            }

            // add new bear to list
            bears.Add(newbear);
        }

        /// <summary>
        /// Gets a random location using the given min and range
        /// </summary>
        /// <param name="min">the minimum</param>
        /// <param name="range">the range</param>
        /// <returns>the random location</returns>
        private int GetRandomLocation(int min, int range)
        {
            return min + RandomNumberGenerator.Next(range);
        }

        /// <summary>
        /// Gets a list of collision rectangles for all the objects in the game world
        /// </summary>
        /// <returns>the list of collision rectangles</returns>
        private List<Rectangle> GetCollisionRectangles()
        {
            List<Rectangle> collisionRectangles = new List<Rectangle>();
            collisionRectangles.Add(burger.CollisionRectangle);
            foreach (TeddyBear bear in bears)
            {
                collisionRectangles.Add(bear.CollisionRectangle);
            }
            foreach (Projectile projectile in projectiles)
            {
                collisionRectangles.Add(projectile.CollisionRectangle);
            }
            foreach (Explosion explosion in explosions)
            {
                collisionRectangles.Add(explosion.CollisionRectangle);
            }
            return collisionRectangles;
        }

        /// <summary>
        /// Checks to see if the burger has just been killed
        /// </summary>
        private void CheckBurgerKill()
        {
            if(burger.BurgerHealth == 0 && burgerDead != true)
            {
                burgerDead = true;
                burgerDeath.Play();
            }
        }

        #endregion
    }
}
