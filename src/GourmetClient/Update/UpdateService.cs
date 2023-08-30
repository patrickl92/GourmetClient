using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GourmetClient.Update.Response;
using Semver;

namespace GourmetClient.Update
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Reflection;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;

    public class UpdateService
    {
        private const string ReleaseListUri = "https://api.github.com/repos/patrickl92/GourmetClient/releases";

        public UpdateService()
        {
            CurrentVersion = SemVersion.Parse(Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion, SemVersionStyles.Strict);
        }

        public SemVersion CurrentVersion { get; }

        public async Task<ReleaseDescription> CheckForUpdate(bool acceptPreReleases)
        {
            try
            {
                return await GetLatestRelease(acceptPreReleases);
            }
            catch (GourmetUpdateException)
            {
                return null;
            }
        }

        public async Task<string> DownloadUpdatePackage(ReleaseDescription updateRelease, IProgress<int> progress, CancellationToken cancellationToken)
        {
            updateRelease = updateRelease ?? throw new ArgumentNullException(nameof(updateRelease));

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
                using var client = CreateHttpClient();

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
            var targetLocation = Path.Combine(GetTempFolderPath(), "UpdatePackage");

            try
            {
                await Task.Run(() =>
                {
                    if (Directory.Exists(targetLocation))
                    {
                        Directory.Delete(targetLocation, true);
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

                if (Directory.Exists(backupLocation))
                {
                    await Task.Run(() => Directory.Delete(backupLocation, true), cancellationToken);
                }

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
                await Task.Run(() => Directory.Delete(GetBackupPath(), true), cancellationToken);
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
            using var client = CreateHttpClient();
            ReleaseEntry[] releases;

            try
            {
                releases = await client.GetFromJsonAsync<ReleaseEntry[]>(ReleaseListUri);
            }
            catch (Exception exception)
            {
                throw new GourmetUpdateException("Error while trying to receive the list of releases", exception);
            }

            var releaseDescriptions = ReleaseEntriesToDescriptions(releases);

            if (!acceptPreReleases)
            {
                releaseDescriptions = releaseDescriptions.Where(description => !description.Version.IsPrerelease);
            }

            foreach (var releaseDescription in releaseDescriptions)
            {
                if (releaseDescription.Version.CompareSortOrderTo(CurrentVersion) > 0)
                {
                    return releaseDescription;
                }
            }

            return null;
        }

        private HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GourmetClient", CurrentVersion.ToString()));

            return client;
        }

        private static IEnumerable<ReleaseDescription> ReleaseEntriesToDescriptions(IEnumerable<ReleaseEntry> entries)
        {
            foreach (var entry in entries.Where(entry => !entry.IsDraft))
            {
                if (SemVersion.TryParse(entry.Name, SemVersionStyles.AllowLowerV, out var semVersion))
                {
                    var updatePackageAsset = entry.Assets.FirstOrDefault(asset => asset.Name == "GourmetClient.zip");
                    var checksumPackageAsset = entry.Assets.FirstOrDefault(asset => asset.Name == "checksum.txt");

                    if (updatePackageAsset != null && checksumPackageAsset != null)
                    {
                        yield return new ReleaseDescription(semVersion, updatePackageAsset.DownloadUrl, updatePackageAsset.Size, checksumPackageAsset.DownloadUrl, checksumPackageAsset.Size);
                    }
                }
            }
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
