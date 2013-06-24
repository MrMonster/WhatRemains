using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace HexGame
{
    class Tile
    {
        int xGrid, yGrid, yLocation, xLocation;
        bool isOdd;  //determines the upper and lower bounds
        private Tile link1, link2, link3, link4, link5, link6;
        private Rectangle collisionRect;
        private Color cHover;



        public Tile(bool _isOdd, int _x, int _y, int _yLocation, int _xLocation)
        {
            isOdd = _isOdd;

            xGrid = _x;
            yGrid = _y;
            yLocation = _yLocation;
            xLocation = _xLocation;
            collisionRect = new Rectangle(xLocation, yLocation, 50, 50);
            cHover = Color.White;

            link1 = this;
            link2 = this;
            link3 = this;
            link4 = this;
            link5 = this;
            link6 = this;

        }

        //sets the color of all the links through their reference
        public void setLinkColor(int i, Color c)
        {
            
            switch (i)
            {
                case 1: link1.setColor(c);
                    break;
                case 2: link2.setColor(c);
                    break;
                case 3: link3.setColor(c);
                    break;
                case 4: link4.setColor(c);
                    break;
                case 5: link5.setColor(c);
                    break;
                case 6: link6.setColor(c);
                    break;
                default: 
                    break;

            } 
        }

        public Color getColor() { return cHover; }
        public void setColor(Color c) { cHover = c; }
        public Rectangle getCollisionRect() { return collisionRect; }
        public void setCollisionRect(Rectangle Rect) { collisionRect = Rect; }
        public bool getIsOdd() { return isOdd; }
        public int getX(){return xGrid;}
        public int getY() {return yGrid;}
        public int getXLocation() { return xLocation; }
        public int getYLocation() { return yLocation; }

        public Tile getLink1() { return link1; }
        public Tile getLink2() { return link2; }
        public Tile getLink3() { return link3; }
        public Tile getLink4() { return link4; }
        public Tile getLink5() { return link5; }
        public Tile getLink6() { return link6; }

        public void setLink1(ref Tile t) { link1 = t; }
        public void setLink2(ref Tile t) { link2 = t; }
        public void setLink3(ref Tile t) { link3 = t; }
        public void setLink4(ref Tile t) { link4 = t; }
        public void setLink5(ref Tile t) { link5 = t; }
        public void setLink6(ref Tile t) { link6 = t; }

        ///////////////////////////////////////////////
        //
        // Left = link1
        // Right = link2
        // BLeft = link3
        // BRight = link4
        // TLeft = link5
        // TRight = link6
        //
        ///////////////////////////////////////////////
        
    }
}
