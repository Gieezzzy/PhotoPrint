using System.Drawing;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace PhotoPrintApp.Services;

public sealed class MobileSignatureServer : IDisposable
{
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private readonly string _token;

    public int Port { get; private set; }
    public string Token => _token;
    public bool IsRunning => _listener != null;

    public event Action<Bitmap>? SignatureReceived;
    public event Action<string>? ServerLog;

    public MobileSignatureServer()
    {
        _token = Guid.NewGuid().ToString("N")[..12];
    }

    public void Start(int preferredPort = 5088)
    {
        if (_listener != null) return;

        try
        {
            _listener = new TcpListener(IPAddress.Any, preferredPort);
            _listener.Start();
        }
        catch (SocketException)
        {
            // Fallback to OS-assigned free port
            _listener = new TcpListener(IPAddress.Any, 0);
            _listener.Start();
        }

        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => AcceptLoopAsync(_cts.Token));
        ServerLog?.Invoke($"Listening on port {Port}");
    }

    public string BuildUrl(string ipAddress) => $"http://{ipAddress}:{Port}/sign?token={_token}";

    public static List<string> GetLocalIPv4Addresses()
    {
        var results = new List<string>();
        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                    ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;

                var props = ni.GetIPProperties();
                foreach (var ua in props.UnicastAddresses)
                {
                    if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                    string ip = ua.Address.ToString();
                    if (ip.StartsWith("169.254.")) continue; // skip link-local
                    if (!results.Contains(ip))
                        results.Add(ip);
                }
            }
        }
        catch
        {
            // Ignore enumeration errors
        }

        // Prioritize common LAN subnets (192.168.*, 10.*, 172.16-31.*)
        results = results
            .OrderByDescending(ip => ip.StartsWith("192.168."))
            .ThenByDescending(ip => ip.StartsWith("10."))
            .ToList();

        if (results.Count == 0)
            results.Add("127.0.0.1");

        return results;
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener != null)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(ct);
                _ = Task.Run(() => HandleClientAsync(client, ct), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                ServerLog?.Invoke($"Accept error: {ex.Message}");
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        using (client)
        {
            try
            {
                client.ReceiveTimeout = 15000;
                client.SendTimeout = 15000;
                using var stream = client.GetStream();

                var request = await ReadHttpRequestAsync(stream, ct);
                if (request == null) return;

                string path = request.Path;
                string queryToken = ExtractQueryParam(request.Query, "token");

                if (path == "/sign" && queryToken == _token)
                {
                    string html = BuildSignaturePageHtml(_token);
                    await SendResponseAsync(stream, "200 OK", "text/html; charset=utf-8", Encoding.UTF8.GetBytes(html), ct);
                }
                else if (path == "/submit" && request.Method == "POST" && queryToken == _token)
                {
                    var bmp = ParseSignatureBitmap(request.Body);
                    if (bmp != null)
                    {
                        SignatureReceived?.Invoke(bmp);
                        var okJson = Encoding.UTF8.GetBytes("{\"ok\":true}");
                        await SendResponseAsync(stream, "200 OK", "application/json", okJson, ct);
                    }
                    else
                    {
                        var errJson = Encoding.UTF8.GetBytes("{\"ok\":false,\"error\":\"Invalid image data\"}");
                        await SendResponseAsync(stream, "400 Bad Request", "application/json", errJson, ct);
                    }
                }
                else if (path == "/favicon.ico")
                {
                    await SendResponseAsync(stream, "204 No Content", "text/plain", Array.Empty<byte>(), ct);
                }
                else
                {
                    var forbidden = Encoding.UTF8.GetBytes("<h3>Invalid or expired signature session.</h3>");
                    await SendResponseAsync(stream, "403 Forbidden", "text/html; charset=utf-8", forbidden, ct);
                }
            }
            catch (Exception ex)
            {
                ServerLog?.Invoke($"Client error: {ex.Message}");
            }
        }
    }

    private static Bitmap? ParseSignatureBitmap(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("image", out var imgProp)) return null;
            string? dataUrl = imgProp.GetString();
            if (string.IsNullOrWhiteSpace(dataUrl)) return null;

            int commaIdx = dataUrl.IndexOf(',');
            string base64 = commaIdx >= 0 ? dataUrl[(commaIdx + 1)..] : dataUrl;
            byte[] bytes = Convert.FromBase64String(base64);

            using var ms = new MemoryStream(bytes);
            using var temp = Image.FromStream(ms);
            return new Bitmap(temp);
        }
        catch
        {
            return null;
        }
    }

    private static string ExtractQueryParam(string query, string key)
    {
        if (string.IsNullOrEmpty(query)) return "";
        foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 && string.Equals(kv[0], key, StringComparison.OrdinalIgnoreCase))
                return Uri.UnescapeDataString(kv[1]);
        }
        return "";
    }

    private static async Task<HttpRequestData?> ReadHttpRequestAsync(NetworkStream stream, CancellationToken ct)
    {
        var buffer = new byte[8192];
        using var headerMs = new MemoryStream();

        int headerEndIndex = -1;
        while (headerEndIndex < 0)
        {
            int read = await stream.ReadAsync(buffer, 0, buffer.Length, ct);
            if (read <= 0) return null;
            headerMs.Write(buffer, 0, read);

            byte[] current = headerMs.ToArray();
            headerEndIndex = FindHeaderEnd(current);
            if (current.Length > 65536 && headerEndIndex < 0) return null;
        }

        byte[] allBytes = headerMs.ToArray();
        string headerText = Encoding.ASCII.GetString(allBytes, 0, headerEndIndex);
        string[] lines = headerText.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0) return null;

        string[] requestLine = lines[0].Split(' ');
        if (requestLine.Length < 2) return null;

        string method = requestLine[0].ToUpperInvariant();
        string fullTarget = requestLine[1];
        int qIdx = fullTarget.IndexOf('?');
        string path = qIdx >= 0 ? fullTarget[..qIdx] : fullTarget;
        string query = qIdx >= 0 ? fullTarget[qIdx..] : "";

        int contentLength = 0;
        foreach (var line in lines.Skip(1))
        {
            int colon = line.IndexOf(':');
            if (colon <= 0) continue;
            string name = line[..colon].Trim();
            string val = line[(colon + 1)..].Trim();
            if (name.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                int.TryParse(val, out contentLength);
        }

        int bodyStart = headerEndIndex + 4;
        int alreadyReadBody = allBytes.Length - bodyStart;

        using var bodyMs = new MemoryStream();
        if (alreadyReadBody > 0)
            bodyMs.Write(allBytes, bodyStart, Math.Min(alreadyReadBody, contentLength));

        while (bodyMs.Length < contentLength)
        {
            int toRead = (int)Math.Min(buffer.Length, contentLength - bodyMs.Length);
            int read = await stream.ReadAsync(buffer, 0, toRead, ct);
            if (read <= 0) break;
            bodyMs.Write(buffer, 0, read);
        }

        string body = Encoding.UTF8.GetString(bodyMs.ToArray());
        return new HttpRequestData(method, path, query, body);
    }

    private static int FindHeaderEnd(byte[] data)
    {
        for (int i = 0; i <= data.Length - 4; i++)
        {
            if (data[i] == '\r' && data[i + 1] == '\n' && data[i + 2] == '\r' && data[i + 3] == '\n')
                return i;
        }
        return -1;
    }

    private static async Task SendResponseAsync(
        NetworkStream stream,
        string status,
        string contentType,
        byte[] bodyBytes,
        CancellationToken ct)
    {
        string headers =
            $"HTTP/1.1 {status}\r\n" +
            $"Content-Type: {contentType}\r\n" +
            $"Content-Length: {bodyBytes.Length}\r\n" +
            "Cache-Control: no-store\r\n" +
            "Connection: close\r\n\r\n";

        byte[] headerBytes = Encoding.ASCII.GetBytes(headers);
        await stream.WriteAsync(headerBytes, 0, headerBytes.Length, ct);
        if (bodyBytes.Length > 0)
            await stream.WriteAsync(bodyBytes, 0, bodyBytes.Length, ct);
        await stream.FlushAsync(ct);
    }

    private static string BuildSignaturePageHtml(string token)
    {
        const string html = @"
<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""utf-8"" />
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no"" />
<title>Photo Print Studio - Sign Here</title>
<style>
  * { box-sizing: border-box; -webkit-tap-highlight-color: transparent; }
  body {
    margin: 0; padding: 16px;
    font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, sans-serif;
    background: #1e2430; color: #f3f4f6;
    display: flex; flex-direction: column; min-height: 100vh;
  }
  .header { text-align: center; margin-bottom: 12px; }
  .header h1 { font-size: 1.2rem; margin: 0 0 4px; }
  .header p { font-size: 0.85rem; color: #9ca3af; margin: 0; }
  .pad-wrap {
    flex: 1; background: #ffffff; border-radius: 12px;
    border: 2px dashed #94a3b8; position: relative;
    min-height: 260px; overflow: hidden;
    box-shadow: 0 10px 25px rgba(0,0,0,0.35);
  }
  canvas {
    width: 100%; height: 100%; display: block;
    touch-action: none; cursor: crosshair;
  }
  .hint {
    position: absolute; bottom: 10px; left: 0; right: 0;
    text-align: center; color: #cbd5e1; font-size: 0.85rem;
    pointer-events: none;
  }
  .controls {
    display: flex; gap: 10px; align-items: center; justify-content: space-between;
    margin-top: 12px; flex-wrap: wrap;
  }
  .pen-opts { display: flex; gap: 8px; align-items: center; }
  .swatch {
    width: 28px; height: 28px; border-radius: 50%; border: 2px solid #475569;
    cursor: pointer;
  }
  .swatch.active { border-color: #38bdf8; transform: scale(1.1); }
  select, button {
    font-size: 0.95rem; padding: 10px 14px; border-radius: 8px; border: none;
  }
  select { background: #334155; color: #fff; }
  .actions { display: flex; gap: 10px; width: 100%; margin-top: 10px; }
  .btn-clear { flex: 1; background: #475569; color: #fff; font-weight: 600; }
  .btn-submit { flex: 2; background: #16a34a; color: #fff; font-weight: 700; }
  .btn-submit:disabled { opacity: 0.5; }
  .status {
    margin-top: 10px; text-align: center; font-size: 0.9rem;
    padding: 8px; border-radius: 8px; display: none;
  }
  .status.ok { display: block; background: #14532d; color: #bbf7d0; }
  .status.err { display: block; background: #7f1d1d; color: #fecaca; }
</style>
</head>
<body>
  <div class=""header"">
    <h1>Draw Your Signature</h1>
    <p>Sign inside the white box below, then tap Submit</p>
  </div>

  <div class=""pad-wrap"" id=""wrap"">
    <canvas id=""pad""></canvas>
    <div class=""hint"" id=""hint"">Sign here with your finger or stylus</div>
  </div>

  <div class=""controls"">
    <div class=""pen-opts"">
      <div class=""swatch active"" style=""background:#000000"" data-color=""#000000""></div>
      <div class=""swatch"" style=""background:#0f2c7a"" data-color=""#0f2c7a""></div>
      <select id=""thickness"">
        <option value=""3"">Thin</option>
        <option value=""5"" selected>Medium</option>
        <option value=""8"">Thick</option>
      </select>
    </div>
  </div>

  <div class=""actions"">
    <button class=""btn-clear"" id=""btnClear"" type=""button"">Clear</button>
    <button class=""btn-submit"" id=""btnSubmit"" type=""button"">Submit Signature</button>
  </div>

  <div class=""status"" id=""status""></div>

<script>
(() => {
  const canvas = document.getElementById('pad');
  const wrap = document.getElementById('wrap');
  const hint = document.getElementById('hint');
  const statusEl = document.getElementById('status');
  const thicknessEl = document.getElementById('thickness');
  const ctx = canvas.getContext('2d');

  let drawing = false;
  let hasDrawn = false;
  let penColor = '#000000';
  let lastPt = null;

  function resize() {
    const rect = wrap.getBoundingClientRect();
    const dpr = Math.min(window.devicePixelRatio || 1, 2);
    canvas.width = Math.max(300, Math.floor(rect.width * dpr));
    canvas.height = Math.max(220, Math.floor(rect.height * dpr));
    ctx.scale(dpr, dpr);
    ctx.lineCap = 'round';
    ctx.lineJoin = 'round';
  }
  resize();
  window.addEventListener('resize', () => { if (!hasDrawn) resize(); });

  function getPoint(e) {
    const rect = canvas.getBoundingClientRect();
    return { x: e.clientX - rect.left, y: e.clientY - rect.top };
  }

  canvas.addEventListener('pointerdown', e => {
    e.preventDefault();
    canvas.setPointerCapture(e.pointerId);
    drawing = true;
    hasDrawn = true;
    hint.style.display = 'none';
    lastPt = getPoint(e);
    ctx.fillStyle = penColor;
    ctx.beginPath();
    ctx.arc(lastPt.x, lastPt.y, parseFloat(thicknessEl.value) / 2, 0, Math.PI * 2);
    ctx.fill();
  });

  canvas.addEventListener('pointermove', e => {
    if (!drawing) return;
    e.preventDefault();
    const pt = getPoint(e);
    ctx.strokeStyle = penColor;
    ctx.lineWidth = parseFloat(thicknessEl.value);
    ctx.beginPath();
    ctx.moveTo(lastPt.x, lastPt.y);
    ctx.lineTo(pt.x, pt.y);
    ctx.stroke();
    lastPt = pt;
  });

  function stopDraw() { drawing = false; lastPt = null; }
  canvas.addEventListener('pointerup', stopDraw);
  canvas.addEventListener('pointercancel', stopDraw);

  document.querySelectorAll('.swatch').forEach(sw => {
    sw.addEventListener('click', () => {
      document.querySelectorAll('.swatch').forEach(s => s.classList.remove('active'));
      sw.classList.add('active');
      penColor = sw.dataset.color;
    });
  });

  document.getElementById('btnClear').addEventListener('click', () => {
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    hasDrawn = false;
    hint.style.display = 'block';
    statusEl.className = 'status';
    statusEl.textContent = '';
  });

  document.getElementById('btnSubmit').addEventListener('click', async () => {
    if (!hasDrawn) {
      statusEl.className = 'status err';
      statusEl.textContent = 'Please draw your signature first.';
      return;
    }
    const btn = document.getElementById('btnSubmit');
    btn.disabled = true;
    btn.textContent = 'Sending...';
    try {
      const dataUrl = canvas.toDataURL('image/png');
      const res = await fetch('/submit?token={{TOKEN}}', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ image: dataUrl })
      });
      if (res.ok) {
        statusEl.className = 'status ok';
        statusEl.textContent = 'Signature sent to Photo Print Studio! You can sign again or close this page.';
      } else {
        throw new Error('Server rejected signature');
      }
    } catch (err) {
      statusEl.className = 'status err';
      statusEl.textContent = 'Failed to send: ' + err.message;
    } finally {
      btn.disabled = false;
      btn.textContent = 'Submit Signature';
    }
  });
})();
</script>
</body>
</html>";
        return html.Replace("{{TOKEN}}", token);
    }

    public void Dispose()
    {
        try
        {
            _cts?.Cancel();
            _listener?.Stop();
        }
        catch
        {
            // Ignore shutdown errors
        }
        finally
        {
            _cts?.Dispose();
            _listener = null;
        }
    }

    private sealed record HttpRequestData(string Method, string Path, string Query, string Body);
}
