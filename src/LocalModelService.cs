using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace ComfortUI
{
    // Optional portable sidecar, downloaded only by the explicit Prepare action.
    // CPU baseline works without Python, Ollama, CUDA or a system-wide installation.
    internal sealed class LocalModelService : IDisposable
    {
        internal const string ModelName = "Qwen2.5-0.5B-Instruct-Q4_K_M";
        internal const string ModelUrl = "https://huggingface.co/Qwen/Qwen2.5-0.5B-Instruct-GGUF/resolve/9217f5db79a29953eb74d5343926648285ec7e67/qwen2.5-0.5b-instruct-q4_k_m.gguf";
        internal const string ModelHash = "74A4DA8C9FDBCD15BD1F6D01D621410D31C6FC00986F5EB687824E7B93D7A9DB";
        private const string Release = "https://github.com/ggml-org/llama.cpp/releases/download/b11718/";
        internal const string CpuArchive = "llama-b11718-bin-win-cpu-x64.zip", GpuArchive = "llama-b11718-bin-win-vulkan-x64.zip";
        internal const string CpuHash = "92CC529B29A43F9A30E21D490CB2E95062ACD0B59AF6FEE8B3B52B28074F838C", GpuHash = "237E5736031A9CDCF9EFF54E1F4E9BD871945DE1D56EC542ECB9EBAF8D45BDB6";
        private readonly string root;
        private readonly Action<string, int> report;
        private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private readonly object sync = new object();
        private readonly Timer idle;
        private Process server;
        private SafeFileHandle serverJob;
        private string endpoint, secret, runningMode;
        private bool verified, gpuVerified, disposed;
        private int active;
        [StructLayout(LayoutKind.Sequential)] private struct BasicLimits { internal long ProcessTime, JobTime; internal uint Flags; internal UIntPtr MinWorkingSet, MaxWorkingSet; internal uint Processes; internal UIntPtr Affinity; internal uint Priority, Scheduling; }
        [StructLayout(LayoutKind.Sequential)] private struct IoCounters { internal ulong ReadOps, WriteOps, OtherOps, ReadBytes, WriteBytes, OtherBytes; }
        [StructLayout(LayoutKind.Sequential)] private struct ExtendedLimits { internal BasicLimits Basic; internal IoCounters Io; internal UIntPtr ProcessMemory, JobMemory, PeakProcessMemory, PeakJobMemory; }
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern SafeFileHandle CreateJobObject(IntPtr attributes, string name);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetInformationJobObject(SafeFileHandle job, int informationClass, ref ExtendedLimits information, uint length);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool AssignProcessToJobObject(SafeFileHandle job, IntPtr process);
        private DateTime lastUse = DateTime.UtcNow;
        internal sealed class Connection : IDisposable
        {
            internal string Endpoint, Key;
            internal Action Release;
            public void Dispose() { Interlocked.Exchange(ref Release, null)?.Invoke(); }
        }
        internal LocalModelService(string directory, Action<string, int> progress)
        {
            root = Path.GetFullPath(directory); report = progress;
            idle = new Timer(_ => { lock (sync) if (active == 0 && DateTime.UtcNow - lastUse > TimeSpan.FromSeconds(90)) Stop(); }, null, 5000, 5000);
        }
        private string ModelPath => Path.Combine(root, "model.gguf");
        internal static string Hash(string path) { using (var stream = File.OpenRead(path)) using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", ""); }
        private static bool Matches(string path, string hash) { return File.Exists(path) && Hash(path) == hash; }
        internal async Task Prepare(ContentTranslator.Options options, CancellationToken token)
        {
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(token, lifetime.Token))
            {
                await gate.WaitAsync(linked.Token).ConfigureAwait(false);
                try
                {
                    if (!Environment.Is64BitProcess && !Environment.Is64BitOperatingSystem) throw new ContentTranslator.Failure("localPlatform");
                    Directory.CreateDirectory(root);
                    var handler = new HttpClientHandler { UseProxy = options.ProxyMode != "direct", AllowAutoRedirect = true };
                    if (options.ProxyMode == "manual") { if (!ContentTranslator.ValidProxy(options.ProxyAddress)) throw new ContentTranslator.Failure("proxy"); handler.Proxy = new WebProxy(options.ProxyAddress); }
                    else if (options.ProxyMode == "system") { var proxy = ContentTranslator.SystemProxyAddress(); handler.Proxy = proxy.Length > 0 ? new WebProxy(proxy) : WebRequest.GetSystemWebProxy(); }
                    using (var client = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(15) })
                    {
                        client.DefaultRequestHeaders.UserAgent.ParseAdd("ComfortUI/0.5.1");
                        await Fetch(client, Release + CpuArchive, Path.Combine(root, CpuArchive), CpuHash, "localRuntimeDownload", 24000000, linked.Token).ConfigureAwait(false);
                        await Fetch(client, ModelUrl, ModelPath, ModelHash, "localModelDownload", 492000000, linked.Token).ConfigureAwait(false);
                        if (options.LocalAcceleration == "auto")
                        {
                            try { await Fetch(client, Release + GpuArchive, Path.Combine(root, GpuArchive), GpuHash, "localGpuDownload", 40000000, linked.Token).ConfigureAwait(false); }
                            catch (OperationCanceledException) { throw; }
                            catch { report?.Invoke("localCpuFallback", -1); }
                        }
                    }
                    verified = false;
                    Verify(linked.Token);
                    report?.Invoke("localReady", -1);
                }
                finally { gate.Release(); }
            }
        }
        private async Task Fetch(HttpClient client, string url, string path, string hash, string stage, long maximum, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (Matches(path, hash)) return;
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".part";
            using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
            deadline.CancelAfter(TimeSpan.FromMinutes(15));
            try
            {
                using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, deadline.Token).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode) throw new ContentTranslator.Failure("localDownloadFailed");
                    var expected = response.Content.Headers.ContentLength;
                    if (expected > maximum) throw new ContentTranslator.Failure("localIntegrity");
                    using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (var output = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None, 65536, true))
                    {
                        var buffer = new byte[65536]; long total = 0; int size, last = -5;
                        while ((size = await input.ReadAsync(buffer, 0, buffer.Length, deadline.Token).ConfigureAwait(false)) > 0)
                        {
                            total += size; if (total > maximum) throw new ContentTranslator.Failure("localIntegrity");
                            await output.WriteAsync(buffer, 0, size, deadline.Token).ConfigureAwait(false);
                            int percent = expected > 0 ? (int)(total * 100 / expected.Value) : 0;
                            if (percent >= last + 5) { last = percent; report?.Invoke(stage, percent); }
                        }
                    }
                }
                token.ThrowIfCancellationRequested();
                if (!Matches(temporary, hash)) throw new ContentTranslator.Failure("localIntegrity");
                if (File.Exists(path)) File.Delete(path);
                File.Move(temporary, path);
            }
            catch (OperationCanceledException) { if (token.IsCancellationRequested) throw; throw new ContentTranslator.Failure("localDownloadFailed"); }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
            }
        }
        internal static string SafeEntry(string directory, string entry)
        {
            var basePath = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var path = Path.GetFullPath(Path.Combine(basePath, entry));
            if (!path.StartsWith(basePath, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Archive path escapes local runtime");
            return path;
        }
        private void Extract(string archive, string directory, CancellationToken token)
        {
            using (var zip = ZipFile.OpenRead(Path.Combine(root, archive)))
            {
                foreach (var entry in zip.Entries)
                {
                    token.ThrowIfCancellationRequested(); var path = SafeEntry(directory, entry.FullName);
                    if (string.IsNullOrEmpty(entry.Name)) { Directory.CreateDirectory(path); continue; }
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    // A running server is never replaced, even if Prepare is pressed during a translation.
                    if (File.Exists(path))
                    {
                        using (var expected = entry.Open()) using (var sha = SHA256.Create())
                            if (Hash(path) == BitConverter.ToString(sha.ComputeHash(expected)).Replace("-", "")) continue;
                        lock (sync) { if (active != 0) throw new ContentTranslator.Failure("busy"); Stop(); }
                    }
                    entry.ExtractToFile(path, true);
                }
            }
        }
        private void Verify(CancellationToken token)
        {
            if (verified) return;
            report?.Invoke("localVerifying", -1); token.ThrowIfCancellationRequested();
            if (!Matches(ModelPath, ModelHash) || !Matches(Path.Combine(root, CpuArchive), CpuHash)) throw new ContentTranslator.Failure("localMissing");
            Extract(CpuArchive, Path.Combine(root, "cpu"), token);
            gpuVerified = Matches(Path.Combine(root, GpuArchive), GpuHash);
            if (gpuVerified) Extract(GpuArchive, Path.Combine(root, "gpu"), token);
            token.ThrowIfCancellationRequested(); verified = true;
        }
        internal async Task<Connection> Connect(string acceleration, CancellationToken token)
        {
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(token, lifetime.Token))
            {
                await gate.WaitAsync(linked.Token).ConfigureAwait(false);
                try
                {
                    Verify(linked.Token);
                    lock (sync)
                    {
                        if (server != null && !server.HasExited && runningMode == acceleration) return Lease();
                        if (active != 0) throw new ContentTranslator.Failure("busy");
                        Stop();
                    }
                    if (acceleration == "auto" && gpuVerified)
                    {
                        try { await Start("gpu", linked.Token).ConfigureAwait(false); }
                        catch (OperationCanceledException) { lock (sync) Stop(); throw; }
                        catch { lock (sync) Stop(); report?.Invoke("localCpuFallback", -1); }
                    }
                    bool running; lock (sync) running = server != null && !server.HasExited;
                    if (!running) await Start("cpu", linked.Token).ConfigureAwait(false);
                    lock (sync) { runningMode = acceleration; return Lease(); }
                }
                finally { gate.Release(); }
            }
        }
        private Connection Lease()
        {
            active++; lastUse = DateTime.UtcNow;
            return new Connection { Endpoint = endpoint, Key = secret, Release = () => { lock (sync) { active--; lastUse = DateTime.UtcNow; } } };
        }
        private async Task Start(string backend, CancellationToken token)
        {
            report?.Invoke("localStarting", -1);
            var folder = Path.Combine(root, backend); var executables = Directory.GetFiles(folder, "llama-server.exe", SearchOption.AllDirectories);
            if (executables.Length != 1) throw new ContentTranslator.Failure("localStartFailed");
            int port; var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
            var key = Guid.NewGuid().ToString("N");
            // All text and responses use loopback HTTP, never process arguments or logs.
            var args = "--model \"" + ModelPath + "\" --host 127.0.0.1 --port " + port + " --alias comfort-local --ctx-size 4096 --parallel 1 --batch-size 128 --threads " + Math.Min(4, Math.Max(1, Environment.ProcessorCount / 2)) + " --gpu-layers " + (backend == "gpu" ? "99 --device Vulkan0" : "0 --device none") + " --api-key " + key + " --no-webui --no-warmup --log-disable";
            var info = new ProcessStartInfo(executables[0], args) { WorkingDirectory = Path.GetDirectoryName(executables[0]), UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, RedirectStandardError = true, RedirectStandardOutput = true };
            var process = new Process { StartInfo = info };
            // Drain and discard native logs. Do not persist prompts, response text or local auth tokens.
            process.OutputDataReceived += (s, e) => { }; process.ErrorDataReceived += (s, e) => { };
            lock (sync)
            {
                if (disposed) throw new OperationCanceledException();
                var job = CreateJobObject(IntPtr.Zero, null); var limits = new ExtendedLimits { Basic = new BasicLimits { Flags = 0x2000 } };
                if (job.IsInvalid || !SetInformationJobObject(job, 9, ref limits, (uint)Marshal.SizeOf(typeof(ExtendedLimits)))) { job.Dispose(); process.Dispose(); throw new ContentTranslator.Failure("localStartFailed"); }
                serverJob = job;
                try { if (!process.Start()) throw new ContentTranslator.Failure("localStartFailed"); server = process; if (!AssignProcessToJobObject(job, process.Handle)) throw new ContentTranslator.Failure("localStartFailed"); }
                catch { Stop(); throw; }
                endpoint = "http://127.0.0.1:" + port; secret = key; lastUse = DateTime.UtcNow;
            }
            process.BeginOutputReadLine(); process.BeginErrorReadLine();
            try
            {
                using (var client = new HttpClient(new HttpClientHandler { UseProxy = false }) { Timeout = TimeSpan.FromSeconds(2) })
                {
                    var until = DateTime.UtcNow.AddSeconds(75);
                    while (DateTime.UtcNow < until)
                    {
                        token.ThrowIfCancellationRequested(); if (process.HasExited) throw new ContentTranslator.Failure("localStartFailed");
                        try { using (var response = await client.GetAsync(endpoint + "/health", token).ConfigureAwait(false)) if (response.IsSuccessStatusCode) { report?.Invoke(backend == "gpu" ? "localRunningGpu" : "localRunningCpu", -1); return; } }
                        catch (HttpRequestException) { }
                        catch (OperationCanceledException) { token.ThrowIfCancellationRequested(); }
                        await Task.Delay(300, token).ConfigureAwait(false);
                    }
                }
                throw new ContentTranslator.Failure("localStartFailed");
            }
            catch { lock (sync) Stop(); throw; }
        }
        private void Stop() { if (server != null) { try { if (!server.HasExited) server.Kill(); } catch { } server.Dispose(); server = null; } serverJob?.Dispose(); serverJob = null; endpoint = secret = null; }
        public void Dispose() { lock (sync) { if (disposed) return; disposed = true; lifetime.Cancel(); Stop(); } idle.Dispose(); }
    }
}
