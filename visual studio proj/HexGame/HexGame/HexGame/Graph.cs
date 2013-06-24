using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;

namespace HexGame
{
    class Graph
    {
        private const int SIZE = 400;

        public int Width, Height;
        public Tile[,] HexGrid;
        bool isOdd = false;
        private Texture2D HexTexture;
        private MouseState mouseStateCurrent, mouseStatePrevious;

        public Graph(int _Width, int _Height)
        {
            Width = _Width;
            Height = _Height;
            HexGrid = new Tile[SIZE, SIZE];
        }

        public void fillGrid()
        {

           //Fills the grid
            for (int i = 0; i < Height; i++)
            {
                for (int k = 0; k < Width; k++)
                {
                    if (!isOdd)
                        HexGrid[i, k] = new Tile(true, i, k, i * 50, k * 50);
                    else
                        HexGrid[i, k] = new Tile(false, i, k, i * 50, k * 50 + 25);
                }

                if (isOdd)
                    isOdd = false;
                else
                    isOdd = true;
            }

            //LINK THE TILES!
            Link();

        }

        //Sets ref's to each tile for traversal.
        private void Link()
        {
            for (int i = 0; i < Height; i++)
            {
                for (int k = 0; k < Width; k++)
                {
                    
                    //left/right link - OK!
                    if (k - 1 >= 0)
                    {
                        HexGrid[i, k].setLink1(ref HexGrid[i, k - 1]);
                        HexGrid[i, k - 1].setLink2(ref HexGrid[i, k]);
                    }

                    //topLeft/bottomRight
                    if (i - 1 >= 0 && k-1 >=0)
                    {
                        //EVEN ODD k-1 DIFFERENCES!
                        if (HexGrid[i, k].getIsOdd())
                        {
                            HexGrid[i, k].setLink5(ref HexGrid[i - 1, k - 1]);
                            HexGrid[i - 1, k - 1].setLink4(ref HexGrid[i, k]);
                        }
                        else
                        {
                         //   HexGrid[i,k].setLink5(ref HexGrid
                        }
                    }
/*
                    //TopRight/BottomLeft
                    if (!(k + 1 > Width) && !(i - 1 <= 0) && HexGrid[i - 1, k + 1] != null)
                    {
                        HexGrid[i, k].setLink6(ref HexGrid[i - 1, k + 1]);
                        HexGrid[i - 1, k + 1].setLink3(ref HexGrid[i, k]);
                    }
 */
                }
            }
        }

        public void LoadContent(ContentManager Content)
        {
            HexTexture = Content.Load<Texture2D>(@"Content/HEX_Fixed");
        }

        public void Update(GameTime gameTime)
        {

            mouseStateCurrent = Mouse.GetState();
            if (mouseStateCurrent.LeftButton == ButtonState.Released && mouseStatePrevious.LeftButton == ButtonState.Pressed)
            {
                for(int i = 0; i < Height; i++)
                {
                    for(int k = 0; k < Width; k++)
                    {
                        if(HexGrid[i,k] == null)
                            break;
                        if (HexGrid[i, k].getCollisionRect().Intersects(new Rectangle(mouseStateCurrent.X, mouseStateCurrent.Y, 2, 2)))
                        {
                         //   HexGrid[i, k].setColor(Color.Red);
                            HexGrid[i, k].setLinkColor(1, Color.Red);

                            //TopLeft Hex
                           HexGrid[i, k].setLinkColor(5, Color.Red);
                        }
                        else
                        {
                        //    HexGrid[i, k].setColor(Color.White);
                            HexGrid[i, k].setLinkColor(1, Color.White);
                            //TopLeft Hex
                            HexGrid[i, k].setLinkColor(5, Color.White);

                        }
                    }
                }
            }

            mouseStatePrevious = mouseStateCurrent;
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {

            for (int i = 0; i < Height; i++)
            {
                for (int k = 0; k < Width; k++)
                {
                    spriteBatch.Draw(HexTexture, new Rectangle(HexGrid[i, k].getXLocation(), HexGrid[i, k].getYLocation(), 50, 50), HexGrid[i,k].getColor());
                   // spriteBatch.Draw(HexTexture, new Rectangle(HexGrid[i,k].getLink1().getXLocation(), HexGrid[i,k].getLink1().getYLocation(), 50, 50), HexGrid[i,k].getColor());
                }
            }


            /*
            spriteBatch.Draw(HexTexture, new Rectangle(50,0,50,50), Color.Red);
            spriteBatch.Draw(HexTexture, new Rectangle(92, 0, 50, 50), Color.Red);
            spriteBatch.Draw(HexTexture, new Rectangle(134, 0, 50, 50), Color.Red);
            spriteBatch.Draw(HexTexture, new Rectangle(71, (int)(50 * .75), 50, 50), Color.Red);
             * */
        }

      
  }
}
