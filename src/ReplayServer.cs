using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace OpenPrestige
{
    public class FlowResponse
    {
        public int Status { get; set; }
        public List<KeyValuePair<string, string>> Headers { get; set; }
        public byte[] Body { get; set; }

        public FlowResponse()
        {
            Headers = new List<KeyValuePair<string, string>>();
            Body = new byte[0];
            Status = 200;
        }

        public FlowResponse(int status, List<KeyValuePair<string, string>> headers, byte[] body)
        {
            Status = status;
            Headers = headers ?? new List<KeyValuePair<string, string>>();
            Body = body ?? new byte[0];
        }
    }

    public class ReplayServer
    {
        private TcpListener _listener;
        private Thread _listenThread;
        private volatile bool _isRunning;
        private readonly int _port;
        private readonly Action<string> _logger;
        private X509Certificate2 _certificate;

        private readonly Dictionary<string, FlowResponse> _table = new Dictionary<string, FlowResponse>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, FlowResponse> _cfgDataVerbatim = new Dictionary<int, FlowResponse>();
        private FlowResponse _cfgDataFallback = null;

        private Dictionary<string, object> _listTemplate = new Dictionary<string, object>
        {
            { "data", "{}" },
            { "maxPages", "1" },
            { "challenge", "" },
            { "id", "" }
        };
        private string _dataChallenge = "";
        private Dictionary<string, object> _seedIndex = new Dictionary<string, object>();
        private Dictionary<string, object> _seedData = new Dictionary<string, object>();

        private Dictionary<string, object> _index = new Dictionary<string, object>();
        private Dictionary<string, object> _data = new Dictionary<string, object>();

        private string _cfgDir;
        private string _indexF;
        private string _dataF;

        private int _ownerUid = 15355;
        private string _ownerUser = "vm3q";
        private int _tier = 2;

        private static readonly HashSet<string> DropHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "transfer-encoding", "connection", "content-length", "date", "server",
            "cf-ray", "cf-cache-status", "nel", "report-to", "alt-svc"
        };

        private static readonly HashSet<string> ConfigEndpoints = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "/modGetConfigListNew", "/modGetConfigData", "/modCreateConfig",
            "/modEditConfig", "/modDeleteConfig"
        };

        public bool IsRunning { get { return _isRunning; } }
        public int Port { get { return _port; } }

        public ReplayServer(int port, Action<string> logger)
        {
            _port = port;
            _logger = logger;
            InitializeStoragePaths();
            LoadCertificate();
            LoadFlows();
            InitializeConfigStore();
        }

        private void InitializeStoragePaths()
        {
            _cfgDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config_store");
            try
            {
                if (!Directory.Exists(_cfgDir)) Directory.CreateDirectory(_cfgDir);
            }
            catch
            {
                _cfgDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenPrestige", "config_store");
                if (!Directory.Exists(_cfgDir)) Directory.CreateDirectory(_cfgDir);
            }
            _indexF = Path.Combine(_cfgDir, "index.json");
            _dataF = Path.Combine(_cfgDir, "data.json");
        }

        private void LoadCertificate()
        {
            try
            {
                byte[] pfxBytes = null;
                string pfxPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crl", "server.pfx");
                if (File.Exists(pfxPath))
                {
                    pfxBytes = File.ReadAllBytes(pfxPath);
                }
                else
                {
                    pfxBytes = ResourceHelper.GetResourceBytes("server.pfx");
                }

                if (pfxBytes != null && pfxBytes.Length > 0)
                {
                    _certificate = new X509Certificate2(pfxBytes, "", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
                    _logger(string.Format(Variables.LogCertLoaded, _certificate.Subject));
                }
                else
                {
                    _logger(Variables.LogCertLoadMissing);
                }
            }
            catch (Exception ex)
            {
                _logger(string.Format(Variables.LogCertLoadError, ex.Message));
            }
        }

        private void LoadFlows()
        {
            string localFlows = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "flows");
            if (Directory.Exists(localFlows))
            {
                LoadFlowsFromDirectory(localFlows);
            }
            else
            {
                LoadFlowsFromZip();
            }
            _logger(string.Format(Variables.LogFlowsLoaded, _table.Count));
        }

        private void LoadFlowsFromDirectory(string flowsDir)
        {
            string[] subDirs = Directory.GetDirectories(flowsDir);
            Array.Sort(subDirs);

            foreach (string dir in subDirs)
            {
                string name = Path.GetFileName(dir);
                string reqTxtF = Path.Combine(dir, "request.txt");
                string resTxtF = Path.Combine(dir, "response.txt");
                string resBinF = Path.Combine(dir, "response.body.bin");
                string reqBinF = Path.Combine(dir, "request.body.bin");

                if (!File.Exists(reqTxtF) || !File.Exists(resTxtF)) continue;

                string reqTxt = File.ReadAllText(reqTxtF, Encoding.UTF8);
                string resTxt = File.ReadAllText(resTxtF, Encoding.UTF8);
                byte[] resBody = File.Exists(resBinF) ? File.ReadAllBytes(resBinF) : new byte[0];
                byte[] reqBody = File.Exists(reqBinF) ? File.ReadAllBytes(reqBinF) : new byte[0];

                ProcessFlowEntry(name, reqTxt, resTxt, reqBody, resBody);
            }
        }

        private void LoadFlowsFromZip()
        {
            byte[] zipBytes = ResourceHelper.GetResourceBytes("flows.zip");
            if (zipBytes == null || zipBytes.Length == 0)
            {
                string localZip = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "flows.zip");
                if (File.Exists(localZip)) zipBytes = File.ReadAllBytes(localZip);
            }

            if (zipBytes == null || zipBytes.Length == 0)
            {
                _logger(Variables.LogFlowsZipMissing);
                return;
            }

            try
            {
                using (MemoryStream ms = new MemoryStream(zipBytes))
                using (ZipArchive archive = new ZipArchive(ms, ZipArchiveMode.Read))
                {
                    // Group entries by directory name
                    Dictionary<string, Dictionary<string, byte[]>> dirs = new Dictionary<string, Dictionary<string, byte[]>>();

                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string fullName = entry.FullName.Replace('\\', '/');
                        int slashIdx = fullName.IndexOf('/');
                        if (slashIdx < 0) continue;

                        string dirName = fullName.Substring(0, slashIdx);
                        string fileName = fullName.Substring(slashIdx + 1);

                        if (!dirs.ContainsKey(dirName))
                        {
                            dirs[dirName] = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
                        }

                        using (Stream s = entry.Open())
                        using (MemoryStream ems = new MemoryStream())
                        {
                            s.CopyTo(ems);
                            dirs[dirName][fileName] = ems.ToArray();
                        }
                    }

                    var sortedDirs = new List<string>(dirs.Keys);
                    sortedDirs.Sort();

                    foreach (string dirName in sortedDirs)
                    {
                        var files = dirs[dirName];
                        if (!files.ContainsKey("request.txt") || !files.ContainsKey("response.txt")) continue;

                        string reqTxt = Encoding.UTF8.GetString(files["request.txt"]);
                        string resTxt = Encoding.UTF8.GetString(files["response.txt"]);
                        byte[] reqBody = files.ContainsKey("request.body.bin") ? files["request.body.bin"] : new byte[0];
                        byte[] resBody = files.ContainsKey("response.body.bin") ? files["response.body.bin"] : new byte[0];

                        ProcessFlowEntry(dirName, reqTxt, resTxt, reqBody, resBody);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger(string.Format(Variables.LogFlowsZipError, ex.Message));
            }
        }

        private void ProcessFlowEntry(string name, string reqTxt, string resTxt, byte[] reqBody, byte[] resBody)
        {
            string path = ExtractPath(reqTxt);
            int status;
            var headers = ExtractHeaders(resTxt, out status);
            FlowResponse response = new FlowResponse(status, headers, resBody);

            if (name.IndexOf("modGetConfigData", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                string reqJson = Encoding.UTF8.GetString(reqBody);
                int? cid = SimpleJson.ExtractId(reqJson);
                if (cid.HasValue)
                {
                    _cfgDataVerbatim[cid.Value] = response;
                    try
                    {
                        string bodyJson = Encoding.UTF8.GetString(resBody);
                        var obj = SimpleJson.DeserializeObject(bodyJson);
                        if (obj != null)
                        {
                            if (obj.ContainsKey("data") && obj["data"] != null)
                            {
                                _seedData[cid.Value.ToString()] = obj["data"];
                            }
                            if (obj.ContainsKey("challenge") && obj["challenge"] != null)
                            {
                                _dataChallenge = obj["challenge"].ToString();
                            }
                        }
                    }
                    catch { }
                }
                if (_cfgDataFallback == null) _cfgDataFallback = response;
            }
            else if (path.Equals("/modGetConfigListNew", StringComparison.OrdinalIgnoreCase))
            {
                _table[path] = response;
                try
                {
                    string bodyJson = Encoding.UTF8.GetString(resBody);
                    var outer = SimpleJson.DeserializeObject(bodyJson);
                    if (outer != null)
                    {
                        _listTemplate = new Dictionary<string, object>(outer);
                        if (outer.ContainsKey("data") && outer["data"] != null)
                        {
                            var parsedIndex = SimpleJson.DeserializeObject(outer["data"].ToString());
                            if (parsedIndex != null) _seedIndex = parsedIndex;
                        }
                    }
                }
                catch { }
            }
            else
            {
                _table[path] = response;
            }
        }

        private string ExtractPath(string requestText)
        {
            using (StringReader reader = new StringReader(requestText))
            {
                string firstLine = reader.ReadLine() ?? "";
                string[] parts = firstLine.Split(' ');
                string url = parts.Length > 1 ? parts[1].Trim() : firstLine.Trim();

                string path = url;
                if (url.Contains("://"))
                {
                    int afterScheme = url.IndexOf("://") + 3;
                    int slash = url.IndexOf('/', afterScheme);
                    path = slash >= 0 ? url.Substring(slash) : "/";
                }

                int q = path.IndexOf('?');
                if (q >= 0) path = path.Substring(0, q);
                return path;
            }
        }

        private List<KeyValuePair<string, string>> ExtractHeaders(string responseText, out int statusCode)
        {
            statusCode = 200;
            var headers = new List<KeyValuePair<string, string>>();
            using (StringReader reader = new StringReader(responseText))
            {
                string statusLine = reader.ReadLine() ?? "";
                string[] sParts = statusLine.Split(' ');
                if (sParts.Length > 1) int.TryParse(sParts[1], out statusCode);

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    int colon = line.IndexOf(':');
                    if (colon > 0)
                    {
                        string k = line.Substring(0, colon).Trim();
                        string v = line.Substring(colon + 1).Trim();
                        if (!DropHeaders.Contains(k))
                        {
                            headers.Add(new KeyValuePair<string, string>(k, v));
                        }
                    }
                }
            }
            return headers;
        }

        private void InitializeConfigStore()
        {
            if (File.Exists(_indexF) && File.Exists(_dataF))
            {
                try
                {
                    string indexJson = File.ReadAllText(_indexF, Encoding.UTF8);
                    string dataJson = File.ReadAllText(_dataF, Encoding.UTF8);
                    _index = SimpleJson.DeserializeObject(indexJson) ?? new Dictionary<string, object>(_seedIndex);
                    _data = SimpleJson.DeserializeObject(dataJson) ?? new Dictionary<string, object>(_seedData);
                }
                catch
                {
                    _index = new Dictionary<string, object>(_seedIndex);
                    _data = new Dictionary<string, object>(_seedData);
                    SaveStore();
                }
            }
            else
            {
                // Fallback check embedded index/data.json
                string resIndex = ResourceHelper.GetResourceString("index.json");
                string resData = ResourceHelper.GetResourceString("data.json");

                if (!string.IsNullOrEmpty(resIndex))
                {
                    _index = SimpleJson.DeserializeObject(resIndex) ?? new Dictionary<string, object>(_seedIndex);
                }
                else
                {
                    _index = new Dictionary<string, object>(_seedIndex);
                }

                if (!string.IsNullOrEmpty(resData))
                {
                    _data = SimpleJson.DeserializeObject(resData) ?? new Dictionary<string, object>(_seedData);
                }
                else
                {
                    _data = new Dictionary<string, object>(_seedData);
                }

                SaveStore();
            }

            // Extract owner info
            foreach (var kvp in _index)
            {
                if (kvp.Value is List<object>)
                {
                    List<object> list = (List<object>)kvp.Value;
                    if (list.Count > 7)
                    {
                        int.TryParse(list[2].ToString(), out _ownerUid);
                        _ownerUser = list[3].ToString();
                        int.TryParse(list[7].ToString(), out _tier);
                        break;
                    }
                }
            }
        }

        private void SaveStore()
        {
            try
            {
                File.WriteAllText(_indexF, SimpleJson.Serialize(_index), Encoding.UTF8);
                File.WriteAllText(_dataF, SimpleJson.Serialize(_data), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger(string.Format(Variables.LogConfigSaveError, ex.Message));
            }
        }

        private string GenerateNewId()
        {
            Random rand = new Random();
            while (true)
            {
                string id = rand.Next(1000, 99999).ToString();
                if (!_index.ContainsKey(id)) return id;
            }
        }

        private FlowResponse HandleConfig(string path, byte[] reqBody)
        {
            string now = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            string reqJson = Encoding.UTF8.GetString(reqBody);

            if (path.Equals("/modGetConfigListNew", StringComparison.OrdinalIgnoreCase))
            {
                var resp = new Dictionary<string, object>(_listTemplate);
                resp["data"] = SimpleJson.Serialize(_index);
                byte[] body = Encoding.UTF8.GetBytes(SimpleJson.Serialize(resp));
                var headers = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("Content-Type", "application/json")
                };
                return new FlowResponse(200, headers, body);
            }

            if (path.Equals("/modGetConfigData", StringComparison.OrdinalIgnoreCase))
            {
                int? cid = SimpleJson.ExtractId(reqJson);
                if (cid.HasValue)
                {
                    string s = cid.Value.ToString();
                    bool edited = _data.ContainsKey(s) &&
                        (!_cfgDataVerbatim.ContainsKey(cid.Value) ||
                         !_seedData.ContainsKey(s) ||
                         _data[s].ToString() != _seedData[s].ToString());

                    if (edited)
                    {
                        var respObj = new Dictionary<string, object>
                        {
                            { "data", _data[s] },
                            { "challenge", _dataChallenge }
                        };
                        byte[] body = Encoding.UTF8.GetBytes(SimpleJson.Serialize(respObj));
                        var headers = new List<KeyValuePair<string, string>>
                        {
                            new KeyValuePair<string, string>("Content-Type", "application/json")
                        };
                        return new FlowResponse(200, headers, body);
                    }

                    if (_cfgDataVerbatim.ContainsKey(cid.Value))
                    {
                        return _cfgDataVerbatim[cid.Value];
                    }
                }
                return _cfgDataFallback;
            }

            if (path.Equals("/modCreateConfig", StringComparison.OrdinalIgnoreCase))
            {
                var o = SimpleJson.DeserializeObject(reqJson) ?? new Dictionary<string, object>();
                string cid = GenerateNewId();
                string cfgName = o.ContainsKey("name") ? o["name"].ToString() : "config";
                string cfgDesc = o.ContainsKey("desc") ? o["desc"].ToString() : "";
                string cfgTags = o.ContainsKey("tags") ? o["tags"].ToString() : "[]";
                string cfgData = o.ContainsKey("data") ? o["data"].ToString() : "{}";

                _index[cid] = new List<object> { cfgName, cfgDesc, _ownerUid, _ownerUser, 0, now, cfgTags, _tier };
                _data[cid] = cfgData;
                SaveStore();
                _logger(string.Format(Variables.LogConfigCreated, cid, cfgName, _index.Count));

                FlowResponse tableResp;
                return _table.TryGetValue(path, out tableResp) ? tableResp : new FlowResponse(200, null, new byte[0]);
            }

            if (path.Equals("/modEditConfig", StringComparison.OrdinalIgnoreCase))
            {
                var o = SimpleJson.DeserializeObject(reqJson) ?? new Dictionary<string, object>();
                if (o.ContainsKey("id") && o["id"] != null)
                {
                    string cid = o["id"].ToString();
                    if (_index.ContainsKey(cid))
                    {
                        if (o.ContainsKey("data")) _data[cid] = o["data"];
                        if (_index[cid] is List<object>)
                        {
                            List<object> row = (List<object>)_index[cid];
                            if (o.ContainsKey("name") && !string.IsNullOrEmpty(o["name"].ToString())) row[0] = o["name"];
                            if (o.ContainsKey("desc") && !string.IsNullOrEmpty(o["desc"].ToString())) row[1] = o["desc"];
                            if (row.Count > 5) row[5] = now;
                        }
                        SaveStore();
                        _logger(string.Format(Variables.LogConfigEdited, cid));
                    }
                }
                FlowResponse tableResp;
                return _table.TryGetValue(path, out tableResp) ? tableResp : new FlowResponse(200, null, new byte[0]);
            }

            if (path.Equals("/modDeleteConfig", StringComparison.OrdinalIgnoreCase))
            {
                int? cid = SimpleJson.ExtractId(reqJson);
                if (cid.HasValue)
                {
                    string s = cid.Value.ToString();
                    _index.Remove(s);
                    _data.Remove(s);
                    SaveStore();
                    _logger(string.Format(Variables.LogConfigDeleted, s, _index.Count));
                }
                FlowResponse tableResp;
                return _table.TryGetValue(path, out tableResp) ? tableResp : new FlowResponse(200, null, new byte[0]);
            }

            return null;
        }

        public void Start()
        {
            if (_isRunning) return;
            if (_certificate == null)
            {
                _logger(Variables.LogReplayNoCert);
                return;
            }

            try
            {
                _listener = new TcpListener(IPAddress.Loopback, _port);
                _listener.Start();
                _isRunning = true;
                _listenThread = new Thread(ListenLoop)
                {
                    IsBackground = true,
                    Name = "ReplayServerWorker"
                };
                _listenThread.Start();
                _logger(string.Format(Variables.LogReplayListening, _port));
            }
            catch (Exception ex)
            {
                _isRunning = false;
                _logger(string.Format(Variables.LogReplayStartFailed, _port, ex.Message));
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
            _logger(Variables.LogReplayStopped);
        }

        private void ListenLoop()
        {
            while (_isRunning)
            {
                try
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(HandleSslClient, client);
                }
                catch (SocketException)
                {
                    if (!_isRunning) break;
                }
                catch (Exception ex)
                {
                    if (!_isRunning) break;
                    _logger(string.Format(Variables.LogReplayAcceptError, ex.Message));
                }
            }
        }

        private void HandleSslClient(object state)
        {
            TcpClient client = (TcpClient)state;
            using (client)
            {
                SslStream ssl = null;
                try
                {
                    ssl = new SslStream(client.GetStream(), false);
                    ssl.ReadTimeout = 15000;
                    ssl.WriteTimeout = 15000;
                    ssl.AuthenticateAsServer(_certificate, false, SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls, false);

                    // HTTP Keep-alive loop
                    while (_isRunning && client.Connected)
                    {
                        var req = ReadHttpRequest(ssl);
                        if (req == null) break; // Client closed connection or invalid request

                        Stopwatch sw = Stopwatch.StartNew();
                        FlowResponse response = null;

                        if (req.Path.Equals("/__shutdown", StringComparison.OrdinalIgnoreCase))
                        {
                            _logger(Variables.LogShutdownSignal);
                            SendResponse(ssl, new FlowResponse(200, null, Encoding.ASCII.GetBytes("Shutting down...")));
                            ThreadPool.QueueUserWorkItem(s => {
                                Thread.Sleep(300);
                                Environment.Exit(0);
                            });
                            continue;
                        }

                        if (ConfigEndpoints.Contains(req.Path))
                        {
                            try
                            {
                                response = HandleConfig(req.Path, req.Body);
                            }
                            catch (Exception ex)
                            {
                                _logger(string.Format(Variables.LogConfigError, req.Path, ex.Message));
                                _table.TryGetValue(req.Path, out response);
                            }
                        }
                        else
                        {
                            _table.TryGetValue(req.Path, out response);
                        }

                        if (response == null)
                        {
                            _logger(string.Format(Variables.LogRouteMiss, req.Method, req.Path, req.Body.Length));
                            SendNotFound(ssl);
                            continue;
                        }

                        SendResponse(ssl, response);
                        sw.Stop();

                        string tag = ConfigEndpoints.Contains(req.Path) ? "[CFG]" : "[SERVE]";
                        _logger(string.Format(Variables.LogRouteServed,
                            tag, req.Method, req.Path, response.Status, response.Body.Length, sw.ElapsedMilliseconds));
                    }
                }
                catch (Exception)
                {
                    // Socket closed or TLS handshake failed
                }
                finally
                {
                    if (ssl != null)
                    {
                        try { ssl.Close(); } catch { }
                    }
                }
            }
        }

        private class HttpRequest
        {
            public string Method { get; set; }
            public string Path { get; set; }
            public Dictionary<string, string> Headers { get; set; }
            public byte[] Body { get; set; }
        }

        private HttpRequest ReadHttpRequest(Stream stream)
        {
            string firstLine = ReadLine(stream);
            if (string.IsNullOrEmpty(firstLine)) return null;

            string[] parts = firstLine.Split(' ');
            if (parts.Length < 2) return null;

            string method = parts[0];
            string path = parts[1];
            int q = path.IndexOf('?');
            if (q >= 0) path = path.Substring(0, q);

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            int contentLength = 0;

            string line;
            while (!string.IsNullOrEmpty(line = ReadLine(stream)))
            {
                int colon = line.IndexOf(':');
                if (colon > 0)
                {
                    string k = line.Substring(0, colon).Trim();
                    string v = line.Substring(colon + 1).Trim();
                    headers[k] = v;
                    if (k.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                    {
                        int.TryParse(v, out contentLength);
                    }
                }
            }

            byte[] body = new byte[contentLength];
            if (contentLength > 0)
            {
                int totalRead = 0;
                while (totalRead < contentLength)
                {
                    int read = stream.Read(body, totalRead, contentLength - totalRead);
                    if (read <= 0) break;
                    totalRead += read;
                }
            }

            return new HttpRequest
            {
                Method = method,
                Path = path,
                Headers = headers,
                Body = body
            };
        }

        private string ReadLine(Stream stream)
        {
            StringBuilder sb = new StringBuilder();
            int b;
            while ((b = stream.ReadByte()) != -1)
            {
                if (b == '\r')
                {
                    int next = stream.ReadByte();
                    if (next == '\n') break;
                    if (next != -1) sb.Append((char)next);
                }
                else if (b == '\n')
                {
                    break;
                }
                else
                {
                    sb.Append((char)b);
                }
            }

            if (sb.Length == 0 && b == -1) return null;
            return sb.ToString();
        }

        private void SendResponse(Stream stream, FlowResponse response)
        {
            StringBuilder headerSb = new StringBuilder();
            headerSb.Append(string.Format("HTTP/1.1 {0} OK\r\n", response.Status));
            headerSb.Append(string.Format("Content-Length: {0}\r\n", response.Body.Length));
            headerSb.Append("Connection: keep-alive\r\n");

            bool hasContentType = false;
            foreach (var h in response.Headers)
            {
                if (h.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)) hasContentType = true;
                headerSb.Append(string.Format("{0}: {1}\r\n", h.Key, h.Value));
            }
            if (!hasContentType)
            {
                headerSb.Append("Content-Type: application/json\r\n");
            }
            headerSb.Append("\r\n");

            byte[] headerBytes = Encoding.ASCII.GetBytes(headerSb.ToString());
            stream.Write(headerBytes, 0, headerBytes.Length);
            if (response.Body.Length > 0)
            {
                stream.Write(response.Body, 0, response.Body.Length);
            }
            stream.Flush();
        }

        private void SendNotFound(Stream stream)
        {
            byte[] notFound = Encoding.ASCII.GetBytes("HTTP/1.1 404 Not Found\r\nContent-Length: 0\r\nConnection: keep-alive\r\n\r\n");
            stream.Write(notFound, 0, notFound.Length);
            stream.Flush();
        }
    }
}
