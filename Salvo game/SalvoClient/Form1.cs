/* Authors: Evan Juneau and Chet Ransonet
 * CLID: eaj8153 and cxr4680
 * Class: CMPS 358
 * Assignment: Project #2
 * Due Date: 11:55 PM, November 15, 2015
 * Description: Play a simple game of Salvo!
 *              Connect to a Salvo Server via IP address and wait for an opponent.
 *              When two players are connected, place your ships by clicking two 
 *              points where you want the ends of the ships to go and take 
 *              turns launching attacks!
 *              
 * Certification of Authenticity:
 *      We certify that the solution code in this assignment is entirely our own work.
 */

using System;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace SalvoClient
{
    public enum Stage
    {
        CONNECTWAIT,
        CONNECTWAITOPPONENT,
        PLACEBATTLESHIP,
        PLACECRUISER,
        PLACESUBMARINE,
        PLACEDESTROYER,
        WAITOPPONENTSHIPS,
        PLAYERTURN,
        OPPONENTTURN,
        WIN,
        LOSE,
    }

    // Form1 must be first class in file
    public partial class Form1 : Form
    {
        // prevent attack spamming
        // this should be done in the server for security reasons, but we're assuming both players are nice and fair.
        private object _syncButtonObj = new object();

        private Tile[][] _playerTiles = new Tile[Constants.GRID_SIZE][];
        private Tile[][] _opponentTiles = new Tile[Constants.GRID_SIZE][];
        
        private TcpClient _sock;

        // types of packets
        enum PacketAction
        {
            OPPONENTREADY,
            PLACESHIP,
            GAMEREADY,
            PLAYERTURN,
            OPPONENTTURN,
            PLAYERATTACK,
            PLAYERHIT,
            PLAYERMISS,
            OPPONENTATTACK,
            TOOMANYCONNECTIONS,
            OPPONENTDISCONNECTED,
            PLAYERWIN,
            PLAYERLOSS,
            OPPONENTFORFEIT,
            DISPLAYSHIP,
        }

        private Stage _curStage;
        public Stage CurStage
        {
            get { return _curStage; }
            set
            {
                switch (value)
                {
                    case Stage.CONNECTWAIT: turnLabel.Invoke(new MethodInvoker(delegate() { turnLabel.Text = "Please connect to a server."; turnLabel.ForeColor = Color.Black; })); break;
                    case Stage.CONNECTWAITOPPONENT: turnLabel.Invoke(new MethodInvoker(delegate() { turnLabel.Text = "Please wait for an opponent."; })); break;
                    case Stage.PLACEBATTLESHIP: turnLabel.Invoke(new MethodInvoker(delegate() { turnLabel.Text = "Please place your battleship (5 tiles)."; })); break;
                    case Stage.PLACECRUISER: turnLabel.Invoke(new MethodInvoker(delegate() { turnLabel.Text = "Please place your cruiser (4 tiles)."; })); break;
                    case Stage.PLACESUBMARINE: turnLabel.Invoke(new MethodInvoker(delegate() { turnLabel.Text = "Please place your submarine (3 tiles)."; })); break;
                    case Stage.PLACEDESTROYER: turnLabel.Invoke(new MethodInvoker(delegate() { turnLabel.Text = "Please place your destroyer (2 tiles)."; })); break;
                    case Stage.WAITOPPONENTSHIPS: turnLabel.Invoke(new MethodInvoker(delegate() { turnLabel.Text = "Waiting on opponent to place ships..."; })); break;
                    case Stage.PLAYERTURN: turnLabel.Invoke(new MethodInvoker(delegate() { turnLabel.Text = "Your turn."; turnLabel.ForeColor = Color.Blue; })); break;
                    case Stage.OPPONENTTURN: turnLabel.Invoke(new MethodInvoker(delegate() { turnLabel.Text = "Opponent's turn."; turnLabel.ForeColor = Color.Red; })); break;
                    case Stage.WIN: turnLabel.Invoke(new MethodInvoker(delegate() { turnLabel.Text = "You win!"; turnLabel.ForeColor = Color.Blue; })); break;
                    case Stage.LOSE: turnLabel.Invoke(new MethodInvoker(delegate() { turnLabel.Text = "You lost..."; turnLabel.ForeColor = Color.Red; })); break;
                }
                _curStage = value;
            }
        }
        private Point _scratchPoint = new Point(999, 999);

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (!Directory.Exists(Constants.TILE_PATH))
            {
                MessageBox.Show("Error: Can't find the tile image path!");
                Environment.Exit(0);
            }

            // generate labels for x
            for (int i = 0; i < Constants.GRID_SIZE; i++)
            {
                Label playerLabel = new Label();
                playerLabel.Name = "label_playerx" + i.ToString();
                playerLabel.Location = new Point(Constants.PLAYER_LABEL_X + Constants.TILE_SIZE + Constants.TILE_SIZE * i,
                            Constants.PLAYER_LABEL_Y);
                playerLabel.Font = new Font("Myriad Pro", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                playerLabel.Size = new Size(24, 24);
                playerLabel.TextAlign = ContentAlignment.TopCenter;
                playerLabel.Text = Convert.ToChar((byte)'A' + i).ToString();

                this.Controls.Add(playerLabel);

                Label opponentLabel = new Label();
                opponentLabel.Name = "label_opponentx" + i.ToString();
                opponentLabel.Location = new Point(Constants.OPPONENT_LABEL_X + Constants.TILE_SIZE + Constants.TILE_SIZE * i,
                            Constants.OPPONENT_LABEL_Y);
                opponentLabel.Font = new Font("Myriad Pro", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                opponentLabel.Size = new Size(24, 24);
                opponentLabel.TextAlign = ContentAlignment.TopCenter;
                opponentLabel.Text = Convert.ToChar((byte)'A' + i).ToString();

                this.Controls.Add(opponentLabel);
            }

            // generate labels for y
            for (int i = 0; i < Constants.GRID_SIZE; i++)
            {
                Label playerLabel = new Label();
                playerLabel.Name = "label_playery" + i.ToString();
                playerLabel.Location = new Point(Constants.PLAYER_LABEL_X,
                            Constants.PLAYER_LABEL_Y + Constants.TILE_SIZE + Constants.TILE_SIZE * i);
                playerLabel.Font = new Font("Myriad Pro", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                playerLabel.Size = new Size(24, 24);
                playerLabel.TextAlign = ContentAlignment.TopCenter;
                playerLabel.Text = (i + 1).ToString();

                this.Controls.Add(playerLabel);

                Label opponentLabel = new Label();
                opponentLabel.Name = "label_opponenty" + i.ToString();
                opponentLabel.Location = new Point(Constants.OPPONENT_LABEL_X,
                            Constants.OPPONENT_LABEL_Y + Constants.TILE_SIZE + Constants.TILE_SIZE * i);
                opponentLabel.Font = new Font("Myriad Pro", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                opponentLabel.Size = new Size(24, 24);
                opponentLabel.TextAlign = ContentAlignment.TopCenter;
                opponentLabel.Text = (i + 1).ToString();

                this.Controls.Add(opponentLabel);
            }

            // initialize tiles
            for (int i = 0; i < Constants.GRID_SIZE; i++)
            {
                _playerTiles[i] = new Tile[Constants.GRID_SIZE];
                _opponentTiles[i] = new Tile[Constants.GRID_SIZE];
            }

            // set up buttons and construct tiles
            for (int i = 0; i < Constants.GRID_SIZE; i++)
            {
                for (int j = 0; j < Constants.GRID_SIZE; j++)
                {
                    Button playerButton = new Button();
                    playerButton.Location = new Point(Constants.TILE_SIZE * j + Constants.PLAYER_GRID_X,
                        Constants.TILE_SIZE * i + Constants.PLAYER_GRID_Y);
                    playerButton.Name = "buttonP_" + j.ToString() + "_" + i.ToString();
                    playerButton.Size = new System.Drawing.Size(Constants.TILE_SIZE, Constants.TILE_SIZE);
                    playerButton.Text = "";
                    playerButton.Image = Image.FromFile(Constants.TILE_PATH + "nonetile.bmp");
                    playerButton.UseVisualStyleBackColor = true;
                    playerButton.Click += new System.EventHandler(playerButtonClick);

                    this.Controls.Add(playerButton);
                    _playerTiles[j][i] = new Tile(playerButton);

                    Button opponentButton = new Button();
                    opponentButton.Location = new Point(Constants.TILE_SIZE * j + Constants.OPPONENT_GRID_X,
                        Constants.TILE_SIZE * i + Constants.OPPONENT_GRID_Y);
                    opponentButton.Name = "buttonO_" + j.ToString() + "_" + i.ToString();
                    opponentButton.Size = new System.Drawing.Size(Constants.TILE_SIZE, Constants.TILE_SIZE);
                    opponentButton.Text = "";
                    opponentButton.Image = Image.FromFile(Constants.TILE_PATH + "nonetile.bmp");
                    opponentButton.UseVisualStyleBackColor = true;
                    opponentButton.Click += new System.EventHandler(opponentButtonClick);

                    this.Controls.Add(opponentButton);
                    _opponentTiles[j][i] = new Tile(opponentButton);
                }
            }

            CurStage = Stage.CONNECTWAIT;
        }

        /// <summary>
        /// Disconnects client gracefully if the GUI is closed.
        /// </summary>
        /// <param name="e">Required for base call.</param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (_sock != null && _sock.Connected)
                DisconnectClient();
        }

        // show opponent attack on grid
        private void ProcessOpponentAttack(int x, int y)
        {
            if (_playerTiles[x][y].Status == 'S')
            {
                _playerTiles[x][y].Status = 'H';
                statusBox.Invoke(new MethodInvoker(delegate() { statusBox.Text = "Opponent hit at " + IndexToMnemonic(x, y) + "."; }));
            }
            else
            {
                _playerTiles[x][y].Status = 'M';
                statusBox.Invoke(new MethodInvoker(delegate() { statusBox.Text = "Opponent miss at " + IndexToMnemonic(x, y) + "."; }));
            }
        }

        // packet handler
        private void ProcessPacket(string allPacketString)
        {
            string[] allPackets = allPacketString.Split('\0');
            for (int i = 0; i < allPackets.Length; i++)
            {
                string[] packet = allPackets[i].Split(' ');

                if (string.IsNullOrEmpty(packet[0]))
                    break;

                // get packet type
                PacketAction action = (PacketAction)int.Parse(packet[0]);

                // handle packet based on type
                switch (action)
                {
                    // opponent attacking player
                    case PacketAction.OPPONENTATTACK:
                        {
                            Tuple<int, int> coord = MnemonicToIndex(packet[1]);
                            ProcessOpponentAttack(coord.Item1, coord.Item2);
                            CurStage = Stage.PLAYERTURN;
                            break;
                        }

                    // opponent connected
                    case PacketAction.OPPONENTREADY:
                        CurStage = Stage.PLACEBATTLESHIP;
                        break;

                    // ship placement turn
                    case PacketAction.PLACESHIP:
                        CurStage++;
                        break;

                    // player's turn
                    case PacketAction.PLAYERTURN:
                        CurStage = Stage.PLAYERTURN;
                        break;

                    // opponent's turn
                    case PacketAction.OPPONENTTURN:
                        CurStage = Stage.OPPONENTTURN;
                        break;

                    // feedback for player's attack succeeding
                    case PacketAction.PLAYERHIT:
                        {
                            Tuple<int, int> coord = MnemonicToIndex(packet[1]);
                            _opponentTiles[coord.Item1][coord.Item2].Status = 'H';
                            statusBox.Invoke(new MethodInvoker(delegate() { statusBox.Text = "Hit at " + packet[1] + "."; }));
                            CurStage = Stage.OPPONENTTURN;
                            break;
                        }

                    // feedback for player's attack failing
                    case PacketAction.PLAYERMISS:
                        {
                            Tuple<int, int> coord = MnemonicToIndex(packet[1]);
                            _opponentTiles[coord.Item1][coord.Item2].Status = 'M';
                            statusBox.Invoke(new MethodInvoker(delegate() { statusBox.Text = "Miss at " + packet[1] + "."; }));
                            CurStage = Stage.OPPONENTTURN;
                            break;
                        }

                    // your opponent has been disconnected, disconnect player
                    case PacketAction.OPPONENTFORFEIT:
                        CurStage = Stage.WIN;

                        // see results, but close connection.
                        _sock.Close();
                        _sock = null;
                        statusBox.Invoke(new MethodInvoker(delegate() { statusBox.Text = "Your opponent has disconnected and forfeited!"; }));
                        break;

                    // server is full, disconnect
                    case PacketAction.TOOMANYCONNECTIONS:
                        DisconnectClient();
                        statusBox.Invoke(new MethodInvoker(delegate() { statusBox.Text = "Error: Too many connections!"; }));
                        break;

                    // display opponent's ships when lost.
                    case PacketAction.DISPLAYSHIP:
                        {
                            Tuple<int, int> coord = MnemonicToIndex(packet[1]);
                            if (_opponentTiles[coord.Item1][coord.Item2].Status == 'N')
                                _opponentTiles[coord.Item1][coord.Item2].Status = 'S';

                            break;
                        }

                    // player wins!
                    case PacketAction.PLAYERWIN:
                        {
                            Tuple<int, int> coord = MnemonicToIndex(packet[1]);
                            _opponentTiles[coord.Item1][coord.Item2].Status = 'H';
                            CurStage = Stage.WIN;

                            // see results, but close connection.
                            _sock.Close();
                            _sock = null;
                            statusBox.Invoke(new MethodInvoker(delegate() { statusBox.Text = "You won! To play again, please disconnect and reconnect to the server."; }));
                            break;
                        }

                    // player loses!
                    case PacketAction.PLAYERLOSS:
                        {
                            Tuple<int, int> coord = MnemonicToIndex(packet[1]);
                            _playerTiles[coord.Item1][coord.Item2].Status = 'H';
                            CurStage = Stage.LOSE;

                            // see results, but close connection.
                            _sock.Close();
                            _sock = null;
                            statusBox.Invoke(new MethodInvoker(delegate() { statusBox.Text = "You lost... To play again, please disconnect and reconnect to the server."; }));
                            break;
                        }

                    // error, disconnect
                    default:
                        DisconnectClient(); break;
                }
            }
        }

        // changes two ints x and y into a string "xy"
        public string IndexToMnemonic(int i1, int i2)
        {
            return Convert.ToChar((byte)'A' + i1).ToString() + (i2 + 1).ToString();
        }

        // changes a string "xy" into a Tuple <x,y>
        public Tuple<int,int> MnemonicToIndex(string s)
        {
            string x = ((byte)s[0] - (byte)'A').ToString();
            string y = s.Substring(1);
            return Tuple.Create(int.Parse(x), int.Parse(y) - 1);
        }

        // controls ship placement
        public void PlaceShip(int numTilesAway, int xIndex, int yIndex)
        {
            string mnemonic = IndexToMnemonic(xIndex, yIndex);

            if (_scratchPoint.X != 999 && _scratchPoint.Y != 999)   // not sentinel
            {
                int xdist = Math.Abs(_scratchPoint.X - xIndex);
                int ydist = Math.Abs(_scratchPoint.Y - yIndex);
                if ((xdist != numTilesAway || ydist != 0) && (xdist != 0 || ydist != numTilesAway))
                {
                    statusBox.Text = "Error: Please select a tile " + numTilesAway.ToString()
                        + " horizontal or vertical spaces away to place the ship.";
                    _scratchPoint = new Point(999, 999);
                    return;
                }

                if (xdist == numTilesAway)
                {
                    for (int i = Math.Min(_scratchPoint.X, xIndex); i <= Math.Max(_scratchPoint.X, xIndex); i++)
                    {
                        if (_playerTiles[i][yIndex].Status != 'N')
                        {
                            statusBox.Text = "Error: Can't place ship here due to overlap.";
                            _scratchPoint = new Point(999, 999);
                            return;
                        }
                    }

                    for (int i = Math.Min(_scratchPoint.X, xIndex); i <= Math.Max(_scratchPoint.X, xIndex); i++)
                    {
                        _playerTiles[i][yIndex].Status = 'S';
                        string message = ((int)PacketAction.PLACESHIP).ToString() + " " + IndexToMnemonic(i, yIndex);
                        SendPacket(message);
                    }
                }
                else if (ydist == numTilesAway)
                {
                    for (int i = Math.Min(_scratchPoint.Y, yIndex); i <= Math.Max(_scratchPoint.Y, yIndex); i++)
                    {
                        if (_playerTiles[xIndex][i].Status != 'N')
                        {
                            statusBox.Text = "Error: Can't place ship here due to overlap.";
                            _scratchPoint = new Point(999, 999);
                            return;
                        }
                    }

                    for (int i = Math.Min(_scratchPoint.Y, yIndex); i <= Math.Max(_scratchPoint.Y, yIndex); i++)
                    {
                        _playerTiles[xIndex][i].Status = 'S';
                        string message = ((int)PacketAction.PLACESHIP).ToString() + " " + IndexToMnemonic(xIndex, i);
                        SendPacket(message);
                    }
                }

                _scratchPoint = new Point(999, 999);
                CurStage += 1;

                if (CurStage == Stage.WAITOPPONENTSHIPS)
                    SendPacket(((int)PacketAction.GAMEREADY).ToString());
            }
            else
            {
                _scratchPoint = new Point(xIndex, yIndex);
                statusBox.Text = mnemonic + " selected as first location.";
            }
        }

        // player grid button handler
        private void playerButtonClick(object sender, EventArgs e)
        {
            Button theButton = (Button)sender;
            string[] nameSplit = theButton.Name.Split('_');
            int xIndex = int.Parse(nameSplit[1]);
            int yIndex = int.Parse(nameSplit[2]);

            if (CurStage < Stage.PLACEBATTLESHIP || CurStage > Stage.PLACEDESTROYER)
            {
                statusBox.Text = "Error: Can't place ships yet.";
                return;
            }

            if (_playerTiles[xIndex][yIndex].Status != 'N')
            {
                statusBox.Text = "Error: Can't place ship here due to overlap.";
                _scratchPoint = new Point(999, 999);
                return;
            }

            switch (CurStage)
            {
                case Stage.PLACEBATTLESHIP: PlaceShip(4, xIndex, yIndex); break;
                case Stage.PLACECRUISER: PlaceShip(3, xIndex, yIndex); break;
                case Stage.PLACESUBMARINE: PlaceShip(2, xIndex, yIndex); break;
                case Stage.PLACEDESTROYER: PlaceShip(1, xIndex, yIndex); break;
            }
        }

        // opponent grid button handler
        private void opponentButtonClick(object sender, EventArgs e)
        {
            Button theButton = (Button)sender;
            string[] nameSplit = theButton.Name.Split('_');
            int xIndex = int.Parse(nameSplit[1]);
            int yIndex = int.Parse(nameSplit[2]);

            // prevent attack spamming/race condition
            lock (_syncButtonObj)
            {
                if (CurStage == Stage.OPPONENTTURN)
                {
                    statusBox.Text = "Error: Opponent's move.";
                    return;
                }
                else if (CurStage == Stage.PLAYERTURN)
                {
                    if (_opponentTiles[xIndex][yIndex].Status != 'N')
                    {
                        statusBox.Text = "Error: Tile has already been discovered.";
                        return;
                    }

                    CurStage = Stage.OPPONENTTURN;
                    SendPacket(((int)PacketAction.PLAYERATTACK).ToString() + " " + IndexToMnemonic(xIndex, yIndex));
                }
            }
        }

        private void DisconnectClient()
        {
            if (_sock != null)
            {
                _sock.Close();
                _sock = null;
            }

            CurStage = Stage.CONNECTWAIT;
            _scratchPoint = new Point(999, 999);

            for (int i = 0; i < Constants.GRID_SIZE; i++)
            {
                for (int j = 0; j < Constants.GRID_SIZE; j++)
                {
                    _playerTiles[i][j].Status = 'N';
                    _opponentTiles[i][j].Status = 'N';
                }
            }

            hostTextBox.Invoke(new MethodInvoker(delegate() { hostTextBox.ReadOnly = false; }));
            connectButton.Invoke(new MethodInvoker(delegate() { connectButton.Enabled = true; }));
            disconnectButton.Invoke(new MethodInvoker(delegate() { disconnectButton.Enabled = false; }));
            statusBox.Invoke(new MethodInvoker(delegate() { statusBox.Text = "You have been disconnected from the server!"; }));
        }

        public void SendPacket(string packet)
        {
            try
            {
                NetworkStream stream = _sock.GetStream();
                byte[] data = System.Text.Encoding.ASCII.GetBytes(packet + "\0");
                stream.Write(data, 0, data.Length);
            }
            catch
            {
                if (_sock != null && _sock.Connected)
                    DisconnectClient();
            }
        }

        private void ReceivePacketLoop()
        {
            try
            {
                while (_sock.Connected)
                {
                    NetworkStream stream = _sock.GetStream();
                    if (stream.DataAvailable)
                    {
                        byte[] data = new byte[128];
                        stream.Read(data, 0, data.Length);
                        ProcessPacket(Encoding.ASCII.GetString(data));
                    }

                    Thread.Sleep(500);
                }

                throw new Exception();
            }
            catch
            {
                if (_sock != null && _sock.Connected)
                    DisconnectClient();
            }
        }

        private void KeepAlive()
        {
            try
            {
                while (_sock.Connected)
                {
                    if (_sock.Client.Poll(Constants.KEEP_ALIVE_TIME, SelectMode.SelectRead))
                    {
                        byte[] buff = new byte[1];
                        if (_sock.Client.Receive(buff, SocketFlags.Peek) == 0)
                            throw new Exception(); // client disconnected
                    }

                    Thread.Sleep(500);
                }

                throw new Exception();
            }
            catch
            {
                if (_sock != null)
                    DisconnectClient();
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                CurStage++;
                hostTextBox.Invoke(new MethodInvoker(delegate() { hostTextBox.ReadOnly = true; }));
                connectButton.Invoke(new MethodInvoker(delegate() { connectButton.Enabled = false; }));
                disconnectButton.Invoke(new MethodInvoker(delegate() { disconnectButton.Enabled = true; }));
                statusBox.Invoke(new MethodInvoker(delegate() { statusBox.Text = string.Format("Connected to {0}:{1}!", hostTextBox.Text, Constants.PORT); }));

                Thread keepAliveThread = new Thread(KeepAlive);
                keepAliveThread.Start();

                Thread receiveLoopThread = new Thread(ReceivePacketLoop);
                receiveLoopThread.Start();
            }
            catch
            {
                if (_sock != null && _sock.Connected)
                    DisconnectClient();
                statusBox.Invoke(new MethodInvoker(delegate() { statusBox.Text = "Error: Connection failed!"; }));
                connectButton.Invoke(new MethodInvoker(delegate() { connectButton.Enabled = true; }));
            }
        }

        private void connectButton_Click(object sender, EventArgs e)
        {
            string host = this.hostTextBox.Text;
            if (host == string.Empty)
            {
                statusBox.Text = "Error: Please enter the server you wish to connect to.";
                return;
            }

            _sock = new TcpClient();
            _sock.BeginConnect(host, Constants.PORT, ConnectCallback, _sock);

            connectButton.Enabled = false;
            statusBox.Text = "Connecting to the server. Please wait...";
        }

        private void disconnectButton_Click(object sender, EventArgs e) { DisconnectClient(); }
    }


    public static class Constants
    {
        public static string TILE_PATH = Environment.CurrentDirectory + @"\images\";
        public const int GRID_SIZE = 10;
        public const int TILE_SIZE = 24;
        public const int PLAYER_GRID_X = 60;
        public const int PLAYER_GRID_Y = 70;
        public const int OPPONENT_GRID_X = TILE_SIZE * GRID_SIZE + PLAYER_GRID_X + 80;
        public const int OPPONENT_GRID_Y = PLAYER_GRID_Y;
        public const int PLAYER_LABEL_X = 35;
        public const int PLAYER_LABEL_Y = 47;
        public const int OPPONENT_LABEL_X = TILE_SIZE * GRID_SIZE + PLAYER_LABEL_X + 80;
        public const int OPPONENT_LABEL_Y = PLAYER_LABEL_Y;

        public const int PORT = 13337;
        public const int KEEP_ALIVE_TIME = 3000000; // 3 seconds
    }
}
