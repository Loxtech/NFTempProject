using System;
using System.Threading;
using System.Net;
using nanoFramework.Networking;
using nanoFramework.WebServer;

namespace NFTempProject
{
    public static class WebControlPanel
    {
        private static Func<double> _getActualTemp;
        private static Func<double> _getPreferredTemp;
        private static Action<string> _handleAction;

        public static void Start(Func<double> getActualTemp, Func<double> getPreferredTemp, Action<string> handleAction)
        {
            _getActualTemp = getActualTemp;
            _getPreferredTemp = getPreferredTemp;
            _handleAction = handleAction;

            // Start the web server in a background thread
            new Thread(RunServer).Start();
        }

        private static void RunServer()
        {
            using (var server = new WebServer(80, HttpProtocol.Http))
            {
                server.CommandReceived += Server_CommandReceived;
                server.Start();
                Thread.Sleep(Timeout.Infinite);
            }
        }

        // Change the event handler signature to match the delegate: no parameters
        private static void Server_CommandReceived(object obj, WebServerEventArgs e)
        {
            // The event args are not passed directly, so we need to get the current context from the server
            var context = e.Context;
            if (context == null) return;

            var request = context.Request;
            var response = context.Response;

            try
            {
                if (request.HttpMethod == "GET")
                {
                    if (request.RawUrl.StartsWith("/control"))
                    {
                        // Handle action requests from buttons
                        var queryString = request.RawUrl.Split('?');
                        if (queryString.Length > 1)
                        {
                            var queryParams = queryString[1].Split('&');
                            foreach (var param in queryParams)
                            {
                                if (param.StartsWith("action="))
                                {
                                    var action = param.Substring("action=".Length);
                                    _handleAction?.Invoke(action);
                                    break;
                                }
                            }
                        }
                        // Redirect back to the home page
                        response.StatusCode = (int)HttpStatusCode.Redirect;
                        response.RedirectLocation = "/";
                    }
                    else
                    {
                        // Serve the main HTML page
                        string html = GenerateHtmlPage();
                        response.ContentType = "text/html";
                        var buffer = System.Text.Encoding.UTF8.GetBytes(html);
                        response.OutputStream.Write(buffer, 0, buffer.Length);
                    }
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling request: {ex.Message}");
                try
                {
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                }
                catch { /* Ignore potential secondary exception */ }
            }
        }

        private const double EqualTolerance = 0.25;

        private static string GenerateHtmlPage()
        {
            double actual = _getActualTemp();
            double preferred = _getPreferredTemp();
            string status = Math.Abs(actual - preferred) <= EqualTolerance ? "Equal" : (actual < preferred ? "Cold" : "Warm");

            return $@"
<!DOCTYPE html>
<html>
<head>
    <title>ESP32 Control Panel</title>
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <style>
        body {{ font-family: Arial, sans-serif; text-align: center; background-color: #f0f0f0; }}
        .container {{ max-width: 400px; margin: auto; padding: 20px; background-color: white; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        h2 {{ color: #333; }}
        p {{ font-size: 1.2em; margin: 10px 0; }}
        .btn {{ display: inline-block; text-decoration: none; background-color: #007bff; color: white; padding: 10px 20px; margin: 5px; border-radius: 5px; font-size: 1em; }}
        .btn-danger {{ background-color: #dc3545; }}
    </style>
</head>
<body>
    <div class=""container"">
        <h2>ESP32 Temp Control</h2>
        <p>Preferred: {preferred:F1} &deg;C</p>
        <p>Current: {actual:F1} &deg;C</p>
        <p>Status: {status}</p>
        <div>
            <a href=""/control?action=up"" class=""btn"">Up (+0.5)</a>
            <a href=""/control?action=down"" class=""btn"">Down (-0.5)</a>
            <a href=""/control?action=reset"" class=""btn btn-danger"">Reset</a>
        </div>
    </div>
</body>
</html>";
        }
    }
}