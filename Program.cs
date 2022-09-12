using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection.Metadata;

// dotnet publish -c release -r ubuntu.20.04-x64 --self-contained

namespace ClientServer {
    class Program {

        static void Main(string[] args){
            
            
            Client client = new Client("chess.ricksprojects.com", 4449); //This does not work well

            // Client client = new Client("localhost", 4449); // This does work well


            client.ConnectToServer();

            while (true){
                ThreadManager.Update();
            }
        }
    }
}