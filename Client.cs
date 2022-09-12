using System.Collections;
using System.Collections.Generic;
using System;

using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace ClientServer {
    public class Client
    {

        public static int dataBufferSize = 4096;
        public string ip {get; protected set;}
        public int port {get; protected set;}

        public TCP tcp;

        public bool isConnected {get; protected set;}

        protected delegate void PacketHandler(Packet _packet);
        protected Dictionary<int, PacketHandler> packetHandlers;


        public Client(string ip, int port){
            this.ip = ip;
            this.port = port;
            tcp = new TCP(this);
            
        }
        protected virtual void InitializeClientData(){}

        private void OnApplicationQuite(){
            Disconnect();
        }

        public void ConnectToServer(){
            InitializeClientData();
            isConnected = true;
            tcp.Connect();
        }

        private void OnDestroy(){
            Disconnect();
        }

        public class TCP{

            public TcpClient socket;
            private NetworkStream stream;
            private Packet receivedData;
            private Client client;
            private byte[] receiveBuffer;

            public TCP(Client client){
                this.client = client;
            }

            public void Connect(){
                socket = new TcpClient{
                    ReceiveBufferSize = dataBufferSize,
                    SendBufferSize = dataBufferSize,
                };

                receiveBuffer = new byte[dataBufferSize];
                socket.BeginConnect(client.ip, client.port, ConnectCallback, socket);
            }

            private void ConnectCallback(IAsyncResult _result){
                socket.EndConnect(_result);

                if (!socket.Connected){
                    return;
                }

                stream = socket.GetStream();

                receivedData = new Packet();

                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            } 

            public void SendData(Packet  _packet){
                try {
                    if (socket != null){
                        stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null);
                    }
                } catch (Exception _ex){
                    Console.WriteLine($"Error sending data to server via TCP: {_ex}");
                }
            }

            private void ReceiveCallback(IAsyncResult _result){
                try {
                    int _byteLength = stream.EndRead(_result);
                    if (_byteLength <= 0){
                        client.Disconnect();
                        return;
                    }

                    byte[] _data = new byte[_byteLength];
                    Array.Copy(receiveBuffer, _data, _byteLength);

                    receivedData.Reset(HandleData(_data)); 
                    stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                } catch(Exception _ex) {
                    Disconnect();
                    Console.WriteLine(_ex);
                }
            }

            private bool HandleData(byte[] _data){
                int _packetLength = 0;
                receivedData.SetBytes(_data);

                if(receivedData.UnreadLength() >= 4){
                    _packetLength = receivedData.ReadInt();
                    if (_packetLength <= 0){
                        return true;
                    }
                }
                while (_packetLength > 0 && _packetLength <= receivedData.UnreadLength()){
                    byte[] _packetBytes = receivedData.ReadBytes(_packetLength);
                    ThreadManager.ExecuteOnMainThread(() => {
                        using (Packet _packet = new Packet(_packetBytes)){
                            int _packetId = _packet.ReadInt();
                            HandlePacket(_packet);
                        }
                    });    
                    _packetLength = 0;
                    if(receivedData.UnreadLength() >= 4){
                        _packetLength = receivedData.ReadInt();
                        if (_packetLength <= 0){
                            return true;
                        }
                    }
                }
                if (_packetLength <= 1){
                    return true;
                }
                return false;
            }

            int counter = 0;
            private void HandlePacket(Packet _packet){
                int number = _packet.ReadInt();
                string text = _packet.ReadString();
                counter ++;
                Console.WriteLine(counter);
            }

            private void Disconnect(){
                client.Disconnect();
                stream = null;
                receivedData = null;
                receiveBuffer = null;
                socket = null;
            }
        }


        public void Disconnect(){
            if (isConnected){
                Console.WriteLine("Disconnected from server.");
                isConnected = false;
                tcp.socket.Close();
            }
        }

    }
}