/* Authors: Evan Juneau and Chet Ransonet
 * CLID: eaj8153 and cxr4680
 * Class: CMPS 358
 * Assignment: Project #2
 * Due Date: 11:55 PM, November 15, 2015
 * Description: This is a server that connects to two clients and
 *              runs a simple game of Salvo between them.
 *              
 * Certification of Authenticity:
 *      We certify that the solution code in this assignment is entirely our own work.
 */
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SalvoServer
{
    // types of packets
    public enum PacketAction
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

    //Client class, wraparound for TcpClient
    public class Client
    {
        public const int GRID_SIZE = 10;
        private const int KEEP_ALIVE_TIME = 3000000; // 3 seconds

        private char[][] _grid;
        private TcpClient _client;

        public bool GameReady { get; set; }
        public string LocalEndPoint { get { return _client.Client.LocalEndPoint.ToString(); } }

        // constructor
        public Client(TcpClient client)
        {
            _grid = new char[GRID_SIZE][];
            for (int i = 0; i < GRID_SIZE; i++)
            {
                _grid[i] = new char[GRID_SIZE];
                for (int j = 0; j < GRID_SIZE; j++)
                    _grid[i][j] = 'N';
            }

            _client = client;
            GameReady = false;

            Thread keepAliveThread = new Thread(KeepAlive);
            keepAliveThread.Start();

            Thread receiveThread = new Thread(ReceivePacketLoop);
            receiveThread.Start();
        }

        // send a packet to the client
        public void SendPacket(string packet)
        {
            try
            {
                NetworkStream stream = _client.GetStream();
                byte[] data = System.Text.Encoding.ASCII.GetBytes(packet + "\0");
                stream.Write(data, 0, data.Length);
                Console.WriteLine("Sent packet: " + packet);
            }
            catch { Program.DisconnectClient(this); }
        }

        // continuously receive packets from the client
        private void ReceivePacketLoop()
        {
            try
            {
                while (_client.Connected)
                {
                    NetworkStream stream = _client.GetStream();
                    if (stream.DataAvailable)
                    {
                        byte[] data = new byte[128];
                        stream.Read(data, 0, data.Length);

                        string packet = System.Text.Encoding.ASCII.GetString(data);
                        Console.WriteLine("Received packet: " + packet);

                        Program.ProcessPacket(this, packet);
                    }

                    Thread.Sleep(500);
                }

                throw new Exception();
            }
            catch { Program.DisconnectClient(this); }
        }

        // checks the grid status of the given coordinates
        public char CheckGridStatus(int x, int y) { return _grid[x][y]; }

        //change status of the given coordinates
        public void ChangeGrid(int x, int y, char s)
        {
            _grid[x][y] = s;
        }

        // receive an attack at the given coordinates
        public bool GetAttack(int x, int y)
        {
            if (_grid[x][y] == 'S')
            {
                _grid[x][y] = 'H';
                return true;
            }
            else
            {
                _grid[x][y] = 'M';
                return false;
            }
        }

        // check if any ships are left
        public bool CheckIfLose()
        {
            for (int x = 0; x < GRID_SIZE; x++)
            {
                for (int y = 0; y < GRID_SIZE; y++)
                {
                    if (_grid[x][y] == 'S')
                        return false;
                }
            }
            return true;

        }

        // keep client/server connection on
        private void KeepAlive()
        {
            try
            {
                while (_client.Connected)
                {
                    if (_client.Client.Poll(KEEP_ALIVE_TIME, SelectMode.SelectRead))
                    {
                        byte[] buff = new byte[1];
                        if (_client.Client.Receive(buff, SocketFlags.Peek) == 0)
                            throw new Exception(); // client disconnected
                    }

                    Thread.Sleep(500);
                }

                throw new Exception();
            }
            catch { Program.DisconnectClient(this); }
        }

        public bool IsConnected() { return _client != null && _client.Connected; }

        public void Disconnect()
        {
            if (_client != null)
            {
                Console.WriteLine("Client {0} disconnected!", LocalEndPoint);
                _client.Close();
                _client = null;
            }
        }
    }
    

    class Program
    {
        private const int PORT = 13337;

        private static string _externalIP;
        private static TcpListener _listener;
        private static Client _client1, _client2;
       
        static void AcceptCallback(IAsyncResult ar)
        {
            TcpListener listener = (TcpListener)ar.AsyncState;

            // client instance
            Client clientInst = new Client(listener.EndAcceptTcpClient(ar));

            listener.Start();

            // if client 1 is not connected,
            if (_client1 == null || !_client1.IsConnected())
            {
                // connect current client as client 1
                _client1 = clientInst;
                Console.WriteLine("Client 1 connected from {0}!", _client1.LocalEndPoint);

                // if client 2 is connected,
                if (_client2 != null && _client2.IsConnected()) 
                {
                    // tell each client that the other is connected
                    _client1.SendPacket(((int)PacketAction.OPPONENTREADY).ToString());
                    _client2.SendPacket(((int)PacketAction.OPPONENTREADY).ToString());
                }
            }
            // else if client 2 is not cconnected,
            else if (_client2 == null || !_client2.IsConnected())
            {
                // connect current client as client 2
                _client2 = clientInst;
                Console.WriteLine("Client 2 connected from {0}!", _client2.LocalEndPoint);

                // if client 1 is connected,
                if (_client1 != null && _client1.IsConnected())
                {
                    // tell each client that the other is connected
                    _client1.SendPacket(((int)PacketAction.OPPONENTREADY).ToString());
                    _client2.SendPacket(((int)PacketAction.OPPONENTREADY).ToString());
                }
            }
            else
            {
                // if two clients are already connected, refuse this client
                Console.WriteLine("Connection refused from {0}! Too many connections.", clientInst.LocalEndPoint);
                clientInst.SendPacket(((int)PacketAction.TOOMANYCONNECTIONS).ToString());
                clientInst.Disconnect();
            }

            listener.BeginAcceptTcpClient(AcceptCallback, listener);
        }

        // changes two ints x and y into a string "xy"
        public static string IndexToMnemonic(int i1, int i2)
        {
            return Convert.ToChar((byte)'A' + i1).ToString() + (i2 + 1).ToString();
        }

        // changes a string "xy" into a Tuple <x,y>
        public static Tuple<int, int> MnemonicToIndex(string s)
        {
            string x = ((byte)s[0] - (byte)'A').ToString();
            string y = s.Substring(1);
            return Tuple.Create(int.Parse(x), int.Parse(y) - 1);
        }

        // packet handler
        public static void ProcessPacket(Client client, string allPacketString)
        {
            string[] allPackets = allPacketString.Split('\0');
            for (int i = 0; i < allPackets.Length; i++)
            {
                string[] packet = allPackets[i].Split(' ');

                if (string.IsNullOrEmpty(packet[0]))
                    break;

                // handle packet based on type
                PacketAction action = (PacketAction)int.Parse(packet[0]);
                switch (action)
                {
                    // place ship
                    case PacketAction.PLACESHIP:
                        {
                            Tuple<int, int> location = MnemonicToIndex(packet[1]);
                            client.ChangeGrid(location.Item1, location.Item2, 'S');
                            break;
                        }

                    // both clients connected, start game
                    case PacketAction.GAMEREADY:
                        {
                            client.GameReady = true;
                            if (_client1 != null && _client1.IsConnected() && _client1.GameReady
                                && _client2 != null && _client2.IsConnected() && _client2.GameReady)
                            {
                                Random r = new Random();
                                int select = r.Next() % 2; // coin flip to decide who goes first
                                if (select == 0)
                                {
                                    // client 1 goes first
                                    _client1.SendPacket(((int)PacketAction.PLAYERTURN).ToString());
                                    _client2.SendPacket(((int)PacketAction.OPPONENTTURN).ToString());
                                }
                                else
                                {
                                    // client 2 goes first
                                    _client2.SendPacket(((int)PacketAction.PLAYERTURN).ToString());
                                    _client1.SendPacket(((int)PacketAction.OPPONENTTURN).ToString());
                                }
                            }
                            break;
                        }

                    // player attack success
                    case PacketAction.PLAYERATTACK:
                        {
                            // if client 1 is attacking,
                            if (client == _client1)
                            {
                                // get attack coordinates
                                Tuple<int, int> location = MnemonicToIndex(packet[1]);

                                // if there is an enemy ship at the coordinates,
                                if (_client2.GetAttack(location.Item1, location.Item2))
                                {
                                    // check if client 2 has any ships left,
                                    if (!_client2.CheckIfLose())
                                    {
                                        // send attack to client 2
                                        string message = ((int)PacketAction.OPPONENTATTACK).ToString() + " " + IndexToMnemonic(location.Item1, location.Item2);
                                        _client2.SendPacket(message);

                                        // send feedback to client 1
                                        message = ((int)PacketAction.PLAYERHIT).ToString() + " " + IndexToMnemonic(location.Item1, location.Item2);
                                        _client1.SendPacket(message);
                                    }
                                    // if client 2 has no ships left
                                    else
                                    {
                                        // client 1 wins
                                        string message = ((int)PacketAction.PLAYERWIN).ToString() + " " + IndexToMnemonic(location.Item1, location.Item2);
                                        _client1.SendPacket(message);

                                        // client 2 loses
                                        // send packet to display all the opponent's ships
                                        for (int x = 0; x < Client.GRID_SIZE; x++)
                                        {
                                            for (int y = 0; y < Client.GRID_SIZE; y++)
                                            {
                                                if (_client1.CheckGridStatus(x, y) == 'S')
                                                {
                                                    message = ((int)PacketAction.DISPLAYSHIP).ToString() + " " + IndexToMnemonic(x, y);
                                                    _client2.SendPacket(message);
                                                }
                                            }
                                        }

                                        message = ((int)PacketAction.PLAYERLOSS).ToString() + " " + IndexToMnemonic(location.Item1, location.Item2);
                                        _client2.SendPacket(message);
                                    }
                                }
                                // if there is no enemy ship at the coordinates,
                                else
                                {
                                    // send attack to client 2
                                    string message = ((int)PacketAction.OPPONENTATTACK).ToString() + " " + IndexToMnemonic(location.Item1, location.Item2);
                                    _client2.SendPacket(message);

                                    //send feedback to client 1
                                    message = ((int)PacketAction.PLAYERMISS).ToString() + " " + IndexToMnemonic(location.Item1, location.Item2);
                                    _client1.SendPacket(message);
                                }
                            }

                            // or, if client 2 is attacking,
                            else
                            {
                                // get attack coordinates
                                Tuple<int, int> location = MnemonicToIndex(packet[1]);

                                // if there is an enemy ship at the coordinates,
                                if (_client1.GetAttack(location.Item1, location.Item2))
                                {
                                    // check if client 1 has any ships left,
                                    if (!_client1.CheckIfLose())
                                    {
                                        // send attack to client 1
                                        string message = ((int)PacketAction.OPPONENTATTACK).ToString() + " " + IndexToMnemonic(location.Item1, location.Item2);
                                        _client1.SendPacket(message);

                                        // send feedback to client 2
                                        message = ((int)PacketAction.PLAYERHIT).ToString() + " " + IndexToMnemonic(location.Item1, location.Item2);
                                        _client2.SendPacket(message);
                                    }
                                    // if client 1 has no ships left
                                    else
                                    {
                                        // client 2 wins
                                        string message = ((int)PacketAction.PLAYERWIN).ToString() + " " + IndexToMnemonic(location.Item1, location.Item2);
                                        _client2.SendPacket(message);

                                        // client 1 loses
                                        // send packet to display all the opponent's ships
                                        for (int x = 0; x < Client.GRID_SIZE; x++)
                                        {
                                            for (int y = 0; y < Client.GRID_SIZE; y++)
                                            {
                                                if (_client2.CheckGridStatus(x, y) == 'S')
                                                {
                                                    message = ((int)PacketAction.DISPLAYSHIP).ToString() + " " + IndexToMnemonic(x, y);
                                                    _client1.SendPacket(message);
                                                }
                                            }
                                        }

                                        message = ((int)PacketAction.PLAYERLOSS).ToString() + " " + IndexToMnemonic(location.Item1, location.Item2);
                                        _client1.SendPacket(message);
                                    }
                                }
                                // if there is no enemy ship at the coordinates,
                                else
                                {
                                    // send attack to client 1
                                    string message = ((int)PacketAction.OPPONENTATTACK).ToString() + " " + IndexToMnemonic(location.Item1, location.Item2);
                                    _client1.SendPacket(message);

                                    //send feedback to client 2
                                    message = ((int)PacketAction.PLAYERMISS).ToString() + " " + IndexToMnemonic(location.Item1, location.Item2);
                                    _client2.SendPacket(message);
                                }
                            }
                            break;
                        }
                    default:
                        Program.DisconnectClient(client);
                        break;
                }
            }
        }

        public static void DisconnectClient(Client client)
        {
            if (client == _client1)
            {
                if (_client2 != null && _client2.IsConnected())
                {
                    // send packet to display all the opponent's ships
                    for (int x = 0; x < Client.GRID_SIZE; x++)
                    {
                        for (int y = 0; y < Client.GRID_SIZE; y++)
                        {
                            if (_client1.CheckGridStatus(x, y) == 'S')
                            {
                                string message = ((int)PacketAction.DISPLAYSHIP).ToString() + " " + IndexToMnemonic(x, y);
                                _client2.SendPacket(message);
                            }
                        }
                    }

                    _client2.SendPacket(((int)PacketAction.OPPONENTFORFEIT).ToString());
                }

                _client1.Disconnect();
            }
            else
            {
                if (_client1 != null && _client1.IsConnected())
                {
                    // send packet to display all the opponent's ships
                    for (int x = 0; x < Client.GRID_SIZE; x++)
                    {
                        for (int y = 0; y < Client.GRID_SIZE; y++)
                        {
                            if (_client2.CheckGridStatus(x, y) == 'S')
                            {
                                string message = ((int)PacketAction.DISPLAYSHIP).ToString() + " " + IndexToMnemonic(x, y);
                                _client1.SendPacket(message);
                            }
                        }
                    }
                    _client1.SendPacket(((int)PacketAction.OPPONENTFORFEIT).ToString());
                }

                _client2.Disconnect();
            }
        }

        static void Main(string[] args)
        {
            _listener = new TcpListener(IPAddress.Any, PORT);
            _listener.Start();
            _listener.BeginAcceptTcpClient(new AsyncCallback(AcceptCallback), _listener);

            _externalIP = new WebClient().DownloadString("http://icanhazip.com").Trim();
            Console.WriteLine("Listening on {0}:{1}...", _externalIP, PORT);

            while (true)
                Thread.Sleep(100);
        }
    }
}
