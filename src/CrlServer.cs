using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace OpenPrestige
{
    public class CrlServer
    {
        private TcpListener _listener;
        private Thread _listenThread;
        private volatile bool _isRunning;
        private readonly int _port;
        private readonly Action<string> _logger;
        private byte[] _crlData;

        public bool IsRunning { get { return _isRunning; } }
        public int Port { get { return _port; } }

        public CrlServer(int port, Action<string> logger)
        {
            _port = port;
            _logger = logger;
            LoadCrlData();
        }

        private void LoadCrlData()
        {
            string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crl", "mitm.crl");
            if (!File.Exists(localPath))
            {
                localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mitm.crl");
            }

            if (File.Exists(localPath))
            {
                _crlData = File.ReadAllBytes(localPath);
            }
            else
            {
                _crlData = ResourceHelper.GetResourceBytes("mitm.crl");
            }

            if (_crlData == null || _crlData.Length == 0)
            {
                _logger(Variables.LogCrlMissing);
                _crlData = new byte[0];
            }
        }

        public void Start()
        {
            if (_isRunning) return;

            try
            {
                _listener = new TcpListener(IPAddress.Loopback, _port);
                _listener.Start();
                _isRunning = true;
                _listenThread = new Thread(ListenLoop)
                {
                    IsBackground = true,
                    Name = "CrlServerWorker"
                };
                _listenThread.Start();
                _logger(string.Format(Variables.LogCrlListening, _port));
            }
            catch (Exception ex)
            {
                _isRunning = false;
                _logger(string.Format(Variables.LogCrlStartFailed, _port, ex.Message));
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;
            _isRunning = false;
            try
            {
                if (_listener != null)
                {
                    _listener.Stop();
                }
            }
            catch { }
            _logger(Variables.LogCrlStopped);
        }

        private void ListenLoop()
        {
            while (_isRunning)
            {
                try
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(HandleClient, client);
                }
                catch (SocketException)
                {
                    if (!_isRunning) break;
                }
                catch (Exception ex)
                {
                    if (!_isRunning) break;
                    _logger(string.Format(Variables.LogCrlAcceptError, ex.Message));
                }
            }
        }

        private void HandleClient(object state)
        {
            TcpClient client = (TcpClient)state;
            using (client)
            {
                try
                {
                    using (NetworkStream stream = client.GetStream())
                    {
                        stream.ReadTimeout = 5000;
                        stream.WriteTimeout = 5000;

                        StreamReader reader = new StreamReader(stream, Encoding.ASCII);
                        string line = reader.ReadLine();
                        string path = "/";
                        if (!string.IsNullOrEmpty(line))
                        {
                            string[] parts = line.Split(' ');
                            if (parts.Length > 1) path = parts[1];
                        }

                        while (!string.IsNullOrEmpty(line))
                        {
                            line = reader.ReadLine();
                        }

                        StringBuilder headerSb = new StringBuilder();
                        headerSb.Append("HTTP/1.1 200 OK\r\n");
                        headerSb.Append("Content-Type: application/pkix-crl\r\n");
                        headerSb.Append("Content-Length: " + _crlData.Length + "\r\n");
                        headerSb.Append("Cache-Control: max-age=86400\r\n");
                        headerSb.Append("Connection: close\r\n");
                        headerSb.Append("\r\n");

                        byte[] headerBytes = Encoding.ASCII.GetBytes(headerSb.ToString());
                        stream.Write(headerBytes, 0, headerBytes.Length);
                        if (_crlData.Length > 0)
                        {
                            stream.Write(_crlData, 0, _crlData.Length);
                        }
                        stream.Flush();

                        _logger(string.Format(Variables.LogCrlServed, path, _crlData.Length));
                    }
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
