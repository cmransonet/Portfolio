/* Authors: Evan Juneau and Chet Ransonet
 * CLID: eaj8153 and cxr4680
 * Class: CMPS 358
 * Assignment: Project #2
 * Due Date: 11:55 PM, November 15, 2015
 * Description: This is a class that helps manipulate tiles on the grid.
 *              
 * Certification of Authenticity:
 *      We certify that the solution code in this assignment is entirely our own work.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace SalvoClient
{
    public class Tile
    {
        private char _status;
        private Button _buttonInfo;

        public Tile(Button info)
        {
            _status = 'N';
            _buttonInfo = info;
        }

        public char Status
        {
            get { return _status; }
            set
            {
                switch (value)
                {
                    case 'H': // hit
                        _status = 'H';
                        _buttonInfo.Image = Image.FromFile(Constants.TILE_PATH + "hittile.bmp");
                        break;
                    case 'M': // miss
                        _status = 'M';
                        _buttonInfo.Image = Image.FromFile(Constants.TILE_PATH + "misstile.bmp");
                        break;
                    case 'N': // none
                        _status = 'N';
                        _buttonInfo.Image = Image.FromFile(Constants.TILE_PATH + "nonetile.bmp");
                        break;
                    case 'S': // ship
                        _status = value;
                        _buttonInfo.Image = Image.FromFile(Constants.TILE_PATH + "shiptile.bmp");
                        break;
                }
            }
        }
    }
}
