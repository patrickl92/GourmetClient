using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using GourmetClient.Update.Response;
using GourmetClient.Utils;
using Semver;

namespace GourmetClient.Update
{
    using Serialization;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Reflection;
    using System.Security.Cryptography;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    public class UpdateService
    {
        private const string ProxyTestUri = "https://api.github.com/";
        private const string ReleaseListUri = "https://api.github.com/repos/patrickl92/GourmetClient/releases";

        private readonly string _releaseListQueryResultFilePath;

        public UpdateService()
        {
            CurrentVersion = SemVersion.Parse(Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion, SemVersionStyles.Strict);

            _releaseListQueryResultFilePath = Path.Combine(App.LocalAppDataPath, "ReleaseListQueryResult.json");
        }

        public SemVersion CurrentVersion { get; }

        public async Task<ReleaseDescription> CheckForUpdate(bool acceptPreReleases)
        {
            try
            {
                var latestRelease = await GetLatestRelease(acceptPreReleases);
                if (latestRelease != null && latestRelease.Version.CompareSortOrderTo(CurrentVersion) > 0)
                {
                    // Version of latest release is newer than current version
                    return latestRelease;
                }
            }
            catch (GourmetUpdateException)
            {
            }

            return null;
        }

        public async Task<string> DownloadUpdatePackage(ReleaseDescription updateRelease, IProgress<int> progress, CancellationToken cancellationToken)
        {
            updateRelease = updateRelease ?? throw new ArgumentNullException(nameof(updateRelease));

            var tempFolderPath = GetTempFolderPath();
            var packagePath = Path.Combine(GetTempFolderPath(), "GourmetClient.zip");
            var signedChecksumFilePath = Path.Combine(GetTempFolderPath(), "checksum.txt");

            try
            {
                if (File.Exists(packagePath))
                {
                    File.Delete(packagePath);
                }

                if (File.Exists(signedChecksumFilePath))
                {
                    File.Delete(signedChecksumFilePath);
                }
            }
            catch (IOException exception)
            {
                throw new GourmetUpdateException("Could not delete local update package files", exception);
            }

            try
            {
                if (!Directory.Exists(tempFolderPath))
                {
                    Directory.CreateDirectory(tempFolderPath!);
                }

                using var client = await CreateHttpClient();

                await using var packageFileStream = new FileStream(packagePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await using var checksumFileStream = new FileStream(signedChecksumFilePath, FileMode.Create, FileAccess.Write, FileShare.None);

                var totalBytesCount = updateRelease.UpdatePackageSize + updateRelease.ChecksumSize;
                var totalReadBytes = 0L;

                var packageSourceStream = await client.GetStreamAsync(updateRelease.UpdatePackageDownloadUrl, cancellationToken);
                await DownloadFile(packageSourceStream, packageFileStream, totalBytesCount, totalReadBytes, progress, cancellationToken);
                totalReadBytes += updateRelease.UpdatePackageSize;

                var checksumSourceStream = await client.GetStreamAsync(updateRelease.ChecksumDownloadUrl, cancellationToken);
                await DownloadFile(checksumSourceStream, checksumFileStream, totalBytesCount, totalReadBytes, progress, cancellationToken);
            }
            catch (TaskCanceledException exception)
            {
                if (exception.InnerException is TimeoutException)
                {
                    throw new GourmetUpdateException("Could not download update package files", exception);
                }
            }
            catch (Exception exception)
            {
                throw new GourmetUpdateException("Could not download update package files", exception);
            }

            await VerifyChecksum(packagePath, signedChecksumFilePath, cancellationToken);

            return packagePath;
        }

        public async Task<string> ExtractUpdatePackage(string packagePath, CancellationToken cancellationToken)
        {
            var tempFolderPath = GetTempFolderPath();
            var targetLocation = Path.Combine(tempFolderPath, "UpdatePackage");

            try
            {
                await Task.Run(() =>
                {
                    if (Directory.Exists(targetLocation))
                    {
                        Directory.Delete(targetLocation, true);
                    }

                    if (!Directory.Exists(tempFolderPath))
                    {
                        Directory.CreateDirectory(tempFolderPath);
                    }

                    ZipFile.ExtractToDirectory(packagePath, targetLocation);
                }, cancellationToken);
            }
            catch (Exception exception) when (exception is IOException || exception is InvalidDataException)
            {
                throw new GourmetUpdateException("Could not extract update package", exception);
            }

            return targetLocation;
        }

        public bool StartUpdate(string updateLocation)
        {
            var gourmetClientExePath = Path.Combine(updateLocation, "GourmetClient.exe");

            if (File.Exists(gourmetClientExePath))
            {
                var assemblyLocation = Assembly.GetEntryAssembly()?.Location;
                var assemblyPath = Path.GetDirectoryName(assemblyLocation);

                Process.Start(gourmetClientExePath, $"/update \"{assemblyPath}\"");

                Environment.Exit(0);
                return true;
            }

            return false;
        }

        public async Task CreateBackup(string path, CancellationToken cancellationToken)
        {
            try
            {
                var backupLocation = GetBackupPath();

                await Task.Run(() =>
                {
                    if (Directory.Exists(backupLocation))
                    {
                        Directory.Delete(backupLocation, true);
                    }
                }, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                await CopyDirectory(path, backupLocation, cancellationToken);
            }
            catch (IOException exception)
            {
                throw new GourmetUpdateException("Could not created backup", exception);
            }
        }

        public async Task RemovePreviousVersion(string path, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Run(() =>
                {
                    foreach (var filePath in Directory.GetFiles(path))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        File.Delete(filePath);
                    }

                    foreach (var directoryPath in Directory.GetDirectories(path))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        Directory.Delete(directoryPath, true);
                    }
                }, cancellationToken);
            }
            catch (IOException exception)
            {
                throw new GourmetUpdateException("Could not created backup", exception);
            }
        }

        public async Task CopyCurrentVersion(string targetPath, CancellationToken cancellationToken)
        {
            try
            {
                var assemblyLocation = Assembly.GetEntryAssembly()?.Location;
                var assemblyPath = Path.GetDirectoryName(assemblyLocation);

                cancellationToken.ThrowIfCancellationRequested();

                await CopyDirectory(assemblyPath, targetPath, cancellationToken);
            }
            catch (IOException exception)
            {
                throw new GourmetUpdateException("Could not created backup", exception);
            }
        }

        public async Task RemoveUpdateFiles(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Run(() =>
                {
                    var backupPath = GetBackupPath();

                    if (Directory.Exists(backupPath))
                    {
                        Directory.Delete(GetBackupPath(), true);
                    }
                }, cancellationToken);
            }
            catch (IOException exception)
            {
                throw new GourmetUpdateException("Could not created backup", exception);
            }
        }

        public bool StartNewVersion(string path)
        {
            var gourmetClientExePath = Path.Combine(path, "GourmetClient.exe");

            if (File.Exists(gourmetClientExePath))
            {
                Process.Start(gourmetClientExePath, "/force");

                Environment.Exit(0);
                return true;
            }

            return false;
        }

        private async Task DownloadFile(Stream sourceStream, Stream targetStream, long totalBytesCount, long totalReadBytesOffset, IProgress<int> progress, CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];
            var totalReadBytes = totalReadBytesOffset;

            while (true)
            {
                var readBytes = await sourceStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                if (readBytes == 0)
                {
                    break;
                }

                await targetStream.WriteAsync(buffer, 0, readBytes, cancellationToken);

                totalReadBytes += readBytes;
                progress.Report((int)((totalReadBytes * 100) / totalBytesCount));
            }
        }

        private async Task<ReleaseDescription> GetLatestRelease(bool acceptPreReleases)
        {
            IEnumerable<ReleaseDescription> releaseDescriptions = await GetAvailableReleases();

            if (!acceptPreReleases)
            {
                releaseDescriptions = releaseDescriptions.Where(description => !description.Version.IsPrerelease);
            }

            return releaseDescriptions.MaxBy(description => description.Version, SemVersion.SortOrderComparer);
        }

        private async Task<IReadOnlyList<ReleaseDescription>> GetAvailableReleases()
        {
            using var client = await CreateHttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, ReleaseListUri);

            IReadOnlyList<ReleaseDescription> releaseDescriptions = Array.Empty<ReleaseDescription>();

            var cachedQueryResult = await GetCachedReleaseListQueryResult();
            if (cachedQueryResult != null)
            {
                request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(cachedQueryResult.ETagHeaderValue, cachedQueryResult.IsWeakETag));
                releaseDescriptions = cachedQueryResult.Releases;
            }

            try
            {
                var response = await client.SendAsync(request);
                
                if (response.StatusCode != HttpStatusCode.NotModified)
                {
                    response.EnsureSuccessStatusCode();

                    var contentStream = await response.Content.ReadAsStreamAsync();
                    var releases = await JsonSerializer.DeserializeAsync<ReleaseEntry[]>(contentStream);

                    releaseDescriptions = ReleaseEntriesToDescriptions(releases);

                    if (response.Headers.ETag != null)
                    {
                        var queryResult = new ReleaseListQueryResult(response.Headers.ETag.Tag, response.Headers.ETag.IsWeak, releaseDescriptions);
                        await SaveReleaseListQueryResult(queryResult);
                    }
                }
            }
            catch (Exception exception)
            {
                throw new GourmetUpdateException("Error while trying to receive the list of releases", exception);
            }

            return releaseDescriptions;
        }

        private async Task<HttpClient> CreateHttpClient()
        {
            var (client, _) = await HttpClientHelper.CreateHttpClient(ProxyTestUri, client => client.GetAsync(ProxyTestUri), new CookieContainer());

            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GourmetClient", CurrentVersion.ToString()));

            return client;
        }

        private async Task<ReleaseListQueryResult> GetCachedReleaseListQueryResult()
        {
            if (File.Exists(_releaseListQueryResultFilePath))
            {
                try
                {
                    await using var fileStream = new FileStream(_releaseListQueryResultFilePath, FileMode.Open, FileAccess.Read, FileShare.None);
                    var serializedResult = await JsonSerializer.DeserializeAsync<SerializableReleaseListQueryResult>(fileStream);

                    return serializedResult.ToReleaseListQueryResult();
                }
                catch (Exception exception) when (exception is IOException || exception is JsonException || exception is InvalidOperationException)
                {
                }
            }

            return null;
        }

        private async Task SaveReleaseListQueryResult(ReleaseListQueryResult queryResult)
        {
            queryResult = queryResult ?? throw new ArgumentNullException(nameof(queryResult));

            var serializedQueryResult = new SerializableReleaseListQueryResult(queryResult);

            try
            {
                var parentDirectory = Path.GetDirectoryName(_releaseListQueryResultFilePath);
                if (parentDirectory != null && !Directory.Exists(parentDirectory))
                {
                    Directory.CreateDirectory(parentDirectory);
                }

                await using var fileStream = new FileStream(_releaseListQueryResultFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await JsonSerializer.SerializeAsync(fileStream, serializedQueryResult, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (IOException)
            {
            }
        }

        private static IReadOnlyList<ReleaseDescription> ReleaseEntriesToDescriptions(IEnumerable<ReleaseEntry> entries)
        {
            var releaseDescriptions = new List<ReleaseDescription>();

            foreach (var entry in entries.Where(entry => !entry.IsDraft))
            {
                if (SemVersion.TryParse(entry.Name, SemVersionStyles.AllowLowerV, out var semVersion))
                {
                    var updatePackageAsset = entry.Assets.FirstOrDefault(asset => asset.Name == "GourmetClient.zip");
                    var checksumPackageAsset = entry.Assets.FirstOrDefault(asset => asset.Name == "checksum.txt");

                    if (updatePackageAsset != null && checksumPackageAsset != null)
                    {
                        releaseDescriptions.Add(new ReleaseDescription(semVersion, updatePackageAsset.DownloadUrl, updatePackageAsset.Size, checksumPackageAsset.DownloadUrl, checksumPackageAsset.Size));
                    }
                }
            }

            return releaseDescriptions;
        }

        private async Task VerifyChecksum(string packagePath, string signedChecksumFilePath, CancellationToken cancellationToken)
        {
            byte[] signedChecksum;
            byte[] calculatedChecksum;

            try
            {
                var signedChecksumBase64 = await File.ReadAllTextAsync(signedChecksumFilePath, cancellationToken);
                signedChecksum = Convert.FromBase64String(signedChecksumBase64);

                calculatedChecksum = await CalculateChecksum(packagePath);
            }
            catch (Exception exception) when (exception is IOException || exception is FormatException)
            {
                throw new GourmetUpdateException("Could not read/calculate checksum of update package", exception);
            }

            await using var publicKeyXmlStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GourmetClient.Resources.UpdatePackageSignaturePublicKey.xml");
            if (publicKeyXmlStream == null)
            {
                throw new GourmetUpdateException("Public key for signature of update package could not be found");
            }

            using var publicKeyXmlStreamReader = new StreamReader(publicKeyXmlStream);
            using var rsa = RSA.Create();

            var publicKeyXml = await publicKeyXmlStreamReader.ReadToEndAsync(cancellationToken);
            rsa.FromXmlString(publicKeyXml);

            var rsaDeformatter = new RSAPKCS1SignatureDeformatter(rsa);
            rsaDeformatter.SetHashAlgorithm(nameof(SHA256));

            if (!rsaDeformatter.VerifySignature(calculatedChecksum, signedChecksum))
            {
                throw new GourmetUpdateException("Checksum of update package is invalid");
            }
        }

        private async Task<byte[]> CalculateChecksum(string path)
        {
            using var sha256 = SHA256.Create();
            await using var stream = File.OpenRead(path);

            return await sha256.ComputeHashAsync(stream);
        }

        private async Task CopyDirectory(string sourcePath, string targetPath, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                Directory.CreateDirectory(targetPath);

                foreach (var filePath in Directory.GetFiles(sourcePath))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var fileName = Path.GetFileName(filePath);
                    var targetFilePath = Path.Combine(targetPath, fileName);

                    File.Copy(filePath, targetFilePath);
                }
            }, cancellationToken);

            foreach (var directoryPath in Directory.GetDirectories(sourcePath))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var directoryName = new DirectoryInfo(directoryPath).Name;
                var targetDirectoryPath = Path.Combine(targetPath, directoryName);

                await CopyDirectory(directoryPath, targetDirectoryPath, cancellationToken);
            }
        }

        private string GetBackupPath()
        {
            return Path.Combine(GetTempFolderPath(), "backup");
        }

        private string GetTempFolderPath()
        {
            return Path.Combine(Path.GetTempPath(), "GourmetClientUpdate");
        }
    }
}
