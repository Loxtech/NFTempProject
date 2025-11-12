csharp NFTempProject\WebControlPanel.cs
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NFTempProject
{
    /// <summary>
    /// Lightweight HTTP control panel running on its own thread.
    /// Start it from Program.Main and provide delegates to read state and apply actions.
    /// </summary>
    public static class WebControlPanel
    {
        static Thread? _thread;
        static bool _running;

        public static void Start(Func<double> getActual, Func<double> getPreferred, Action<string> performAction, int port = 80)
        {
            if (_running) return;
            _running = true;
            _thread = new Thread(() => Run(getActual, getPreferred, performAction, port))
            {
                IsBackground = true
            };
            _thread.Start();
        }

        public static void Stop()
        {
            _running = false;
            try
            {
                _thread?.Join(500);
            }
            catch { }
        }

        static void Run(Func<double> getActual, Func<double> getPreferred, Action<string> performAction, int port)
        {
            try
            {
                var listener = new TcpListener(IPAddress.Any, port);
                listener.Start();

                while (_running)
                {
                    if (!listener.Pending())
                    {
                        Thread.Sleep(50);
                        continue;
                    }

                    using (var client = listener.AcceptTcpClient())
                    {
                        try
                        {
                            var stream = client.GetStream();
                            var buffer = new byte[1024];
                            int read = stream.Read(buffer, 0, buffer.Length);
                            if (read <= 0) continue;

                            var request = Encoding.UTF8.GetString(buffer, 0, read);
                            var requestLineEnd = request.IndexOf("\r\n", StringComparison.Ordinal);
                            if (requestLineEnd < 0) requestLineEnd = request.IndexOf("\n", StringComparison.Ordinal);
                            var requestLine = requestLineEnd > 0 ? request.Substring(0, requestLineEnd) : request;
                            var path = "/";
                            var parts = requestLine.Split(' ');
                            if (parts.Length >= 2) path = parts[1];

                            var action = ParseActionFromPath(path);
                            if (!string.IsNullOrEmpty(action))
                            {
                                try { performAction(action); } catch { }
                            }

                            string body = BuildHtml(getActual(), getPreferred(), action);
                            string header = "HTTP/1.1 200 OK\r\n" +
                                            "Content-Type: text/html; charset=utf-8\r\n" +
                                            "Connection: close\r\n" +
                                            "Content-Length: " + Encoding.UTF8.GetByteCount(body) + "\r\n\r\n";

                            var response = Encoding.UTF8.GetBytes(header + body);
                            stream.Write(response, 0, response.Length);
                            stream.Flush();
                        }
                        catch { }
                        finally
                        {
                            client.Close();
                        }
                    }
                }

                listener.Stop();
            }
            catch
            {
                // swallow - keep webserver optional
            }
            finally
            {
                _running = false;
            }
        }

        static string ParseActionFromPath(string path)
        {
            try
            {
                var qIdx = path.IndexOf('?');
                if (qIdx < 0) return null;
                var qs = path.Substring(qIdx + 1);
                var parts = qs.Split('&');
                foreach (var p in parts)
                {
                    var kv = p.Split('=');
                    if (kv.Length == 2 && kv[0].ToLowerInvariant() == "action")
                    {
                        return WebUtility.UrlDecode(kv[1]);
                    }
                }
            }
            catch { }
            return null;
        }

        static string BuildHtml(double actual, double preferred, string? lastAction)
        {
            string status = Math.Abs(actual - preferred) <= 0.25 ? "Equal" :
                (actual < preferred - 0.25 ? "Cold" : "Warm");

            var sb = new StringBuilder();
            sb.Append("<!doctype html><html><head><meta charset='utf-8'><title>Temp Control</title>");
            sb.Append("<meta name='viewport' content='width=device-width, initial-scale=1'/>");
            sb.Append("<style>body{font-family:Arial,Helvetica,sans-serif;padding:10px;} .btn{display:inline-block;padding:10px 16px;margin:4px;background:#0078D7;color:#fff;text-decoration:none;border-radius:4px;} .btn-reset{background:#888;}</style>");
            sb.Append("</head><body>");
            sb.Append($"<h2>Preferred Temperature: {preferred:F1} °C</h2>");
            sb.Append($"<h2>Actual Temperature: {actual:F1} °C</h2>");
            sb.Append($"<p>Status: <strong>{status}</strong></p>");
            if (!string.IsNullOrEmpty(lastAction))
            {
                sb.Append($"<p>Last action: {WebUtility.HtmlEncode(lastAction)}</p>");
            }
            sb.Append("<p>");
            sb.Append("<a class='btn' href='/?action=up'>? Up +0.5</a>");
            sb.Append("<a class='btn' href='/?action=down'>? Down -0.5</a>");
            sb.Append("<a class='btn btn-reset' href='/?action=reset'>? Reset to sensor</a>");
            sb.Append("</p>");
            sb.Append("<p>Refresh the page to see updated values.</p>");
            sb.Append("</body></html>");
            return sb.ToString();
        }
    }
}