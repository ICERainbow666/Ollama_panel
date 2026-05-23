using System.Diagnostics;
using System.Net.Http;

namespace Ollama_panel;

public class OllamaService
{
    private Process? _process;
    private readonly string _ollamaPath;
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(3) };

    public event Action<bool>? StatusChanged;
    public event Action<string>? LogReceived;

    public bool IsRunning => _process is { HasExited: false };

    public OllamaService(string ollamaPath = "ollama")
    {
        _ollamaPath = ollamaPath;
        DetectExistingProcess();
    }

    private void DetectExistingProcess()
    {
        var existing = Process.GetProcessesByName("ollama");
        if (existing.Length > 0)
        {
            _process = existing[0];
            StatusChanged?.Invoke(true);
        }
    }

    public void Start()
    {
        if (IsRunning) return;

        var psi = new ProcessStartInfo
        {
            FileName = _ollamaPath,
            Arguments = "serve",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        try
        {
            _process = Process.Start(psi);
            if (_process == null) return;

            _process.EnableRaisingEvents = true;
            _process.Exited += (_, _) =>
            {
                _process = null;
                StatusChanged?.Invoke(false);
            };

            _process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    LogReceived?.Invoke(e.Data);
            };
            _process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    LogReceived?.Invoke("[ERR] " + e.Data);
            };

            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            StatusChanged?.Invoke(true);
        }
        catch (Exception ex)
        {
            LogReceived?.Invoke($"启动失败: {ex.Message}");
            _process = null;
        }
    }

    public void Stop()
    {
        if (!IsRunning) return;

        try
        {
            // 优先杀掉所有 ollama 进程（包括子进程 ollama runner）
            foreach (var proc in Process.GetProcessesByName("ollama"))
            {
                try { proc.Kill(entireProcessTree: true); } catch { }
            }
            _process = null;
            StatusChanged?.Invoke(false);
        }
        catch (Exception ex)
        {
            LogReceived?.Invoke($"停止失败: {ex.Message}");
        }
    }

    public async Task<bool> IsApiReachableAsync()
    {
        try
        {
            var resp = await _httpClient.GetAsync("http://127.0.0.1:11434");
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> GetVersionAsync()
    {
        try
        {
            var resp = await _httpClient.GetAsync("http://127.0.0.1:11434/api/version");
            return await resp.Content.ReadAsStringAsync();
        }
        catch
        {
            return "";
        }
    }
}
