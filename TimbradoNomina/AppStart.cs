using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace TimbradoNomina
{
    class AppStart
    {

        static void Main(string[] args)
        {

            /*
                string json = @"{'idEmpresa': '11','idTipo': '1','idUsuario':'1','path':'C:\\Users\\Hp\\Desktop\\Anterior\\TimbradoNomina\\xml\\Origen\\001\\09012017','nombreCarpeta':'09012017'}";
                    ConfigurationManager.AppSettings["json"].ToString();// @"{'idEmpresa': '11','idTipo': '1','idUsuario':'1','path':'C:\\Users\\Hp\\Desktop\\Anterior\\TimbradoNomina\\xml\\Origen\\001\\09012017','nombreCarpeta':'09012017'}";            
                AppBussinessLogic bu = new AppBussinessLogic();
                bu.Data = json;
                bu.StartProcessDirectory();
                Console.Read();
             */


            string ipserver = ConfigurationManager.AppSettings["ipserver"];
            int port = int.Parse(ConfigurationManager.AppSettings["port"].ToString());
            CreateListener(ipserver, port);

        }


        private static void CreateListener(string ipConfig, int port)
        {
            TcpListener tcpListener = null;
            IPAddress ipAddress = Dns.GetHostEntry(ipConfig).AddressList[0];

            try
            {
                tcpListener = new TcpListener(IPAddress.Any, port);
                tcpListener.Start();
                Console.WriteLine("Esperando conexión...");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.ToString());
            }
            while (true)
            {
                Thread.Sleep(1000);

                Console.WriteLine("conexion entrante");
                TcpClient tcpClient = tcpListener.AcceptTcpClient();

                string json = ClientPetition(tcpClient);
                Console.WriteLine(json);
                ClientResponse(tcpClient);


                AppBussinessLogic startProcess = new AppBussinessLogic();
                startProcess.Data = json;
                Thread oThread = new Thread(new ThreadStart(startProcess.StartProcessDirectory));
                oThread.Start();
                //startProcess.StartProcessDirectory();

            }
        }


        private static string ClientPetition(TcpClient tcpClient)
        {
            byte[] bytes = new byte[256];
            NetworkStream stream = tcpClient.GetStream();
            stream.Read(bytes, 0, bytes.Length);

            return Encoding.ASCII.GetString(bytes, 0, bytes.Length);
        }


        private static void ClientResponse(TcpClient tcpClient)
        {
            byte[] bytesToSend = new byte[256];
            NetworkStream stream = tcpClient.GetStream();
            bytesToSend = Encoding.ASCII.GetBytes("1");
            stream.Write(bytesToSend, 0, bytesToSend.Length);
        }

    }
}


