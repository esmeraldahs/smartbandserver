using System;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SmartBandServer
{
    //additional changes
    class SocketServer
    {
        public static string Data;

        public static void StartServer()
        {
            var ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            var ipAddress = ipHostInfo.AddressList[0];
            var localEndPoint = new IPEndPoint(ipAddress, 8000);

            var socketConnection = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                socketConnection.Bind(localEndPoint);
                socketConnection.Listen(20);
                Console.WriteLine("Waiting for a connection...");

                while (true)
                {
                    var acceptData = socketConnection.Accept();
                    Data = null;

                    var bytes = new byte[1024];
                    var bytesRec = acceptData.Receive(bytes);
                    Data += Encoding.ASCII.GetString(bytes, 0, bytesRec);

                    Console.WriteLine("HeartBeat per minute : {0}", Data);
                    InsertToFile(Data);
                    InsertToDatabase(Data);
                    var dataReceived = "Data received and stored in file and database successfully.";
                    var message = Encoding.ASCII.GetBytes(dataReceived);

                    acceptData.Send(message);
                    acceptData.Shutdown(SocketShutdown.Both);
                    acceptData.Close();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
            Console.Read();
        }

        public static void InsertToFile(string heartBeat)
        {
            try
            {
                using (var file = new StreamWriter(@"heartbeatdata.txt", true))
                {
                    file.WriteLine(Data + "\t" + DateTime.Now + "\t" + 1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to insert data to file because : {0}", ex.Message);
            }
        }

        public static void InsertToDatabase(string heartBeat)
        {
            string speed;
            if (int.Parse(heartBeat) < 60)
            {
                speed = "Too slow";
            }
            else if(int.Parse(heartBeat) > 100)
            {
                speed = "Too fast";
            }
            else
            {
                speed = "Normal";
            }
            try
            {
                const string connectionString = "Data Source=DESKTOP-72N7MJR\\ESME;Initial Catalog=SocketConn;Integrated Security=True";
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("INSERT INTO HeartBeat(HeartBeat, Name, LastName, Timestamp, SmartBand_Id, Speed, " +
                                                    "MeasuringUnit) VALUES (@HeartBeat, @Name, @LastName, @Timestamp, @SmartBand_Id," +
                                                            "@Speed, @MeasuringUnit)", conn))
                    {
                        cmd.Parameters.AddWithValue("@HeartBeat", heartBeat);
                        cmd.Parameters.AddWithValue("@Name", "John");
                        cmd.Parameters.AddWithValue("@LastName", "Leusden");
                        cmd.Parameters.AddWithValue("@Timestamp", DateTime.Now);
                        cmd.Parameters.AddWithValue("@SmartBand_Id", 1);
                        cmd.Parameters.AddWithValue("@Speed", speed);
                        cmd.Parameters.AddWithValue("@MeasuringUnit", "BPM");
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine("Failed to insert data to database because : {0}", ex.Message);
            }
        }

        public static int Main(string[] args)
        {
            StartServer();
            return 0;
        }
    }
}
