param
(
    [string]$version = $(Read-Host "Enter version"),
    [string]$outputDirectory = "artifacts"
)

if (-Not ($version -match "^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$")) {
    # Version does not match SemVer Regex
    Write-Error "Version is not a valid SemVer string"
    exit 1
}

$updatePackageSignatureSecurePassword = Read-Host -Prompt "Signature password" -AsSecureString

if (-not [IO.Path]::IsPathRooted($outputDirectory)) {
    $outputDirectory = [IO.Path]::Combine($pwd, $outputDirectory)
}

if (-Not (Test-Path -Path $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

############## Define paths
$updatePackageSignaturePrivateKeyFilePath = [IO.Path]::Combine($PSScriptRoot, "UpdatePackageSignaturePrivateKeyXml.txt")
$gourmetSolutionFilePath = [IO.Path]::Combine($PSScriptRoot, "..", "GourmetClient.sln")
$gourmetClientPublishDirectory = [IO.Path]::Combine($PSScriptRoot, "..", "src", "GourmetClient", "bin", "publish")
$updatePackagePath = [IO.Path]::Combine($outputDirectory, "GourmetClient.zip")
$hashFilePath = [IO.Path]::Combine($outputDirectory, "checksum.txt")

if (Test-Path -Path $updatePackagePath) {
    Remove-Item -Path $updatePackagePath
}


############## Build
dotnet publish $gourmetSolutionFilePath -c Release -p:PublishProfile=PublishRelease -p:Version=$version


############## Create package
$updatePackagePath = [IO.Path]::Combine($outputDirectory, "GourmetClient.zip")
Compress-Archive -Path "$gourmetClientPublishDirectory\*" -DestinationPath $updatePackagePath


############## Create checksum of package
$sha256 = [System.Security.Cryptography.SHA256]::Create()
$updatePackageStream = [IO.File]::OpenRead($updatePackagePath)
$hashResult = $sha256.ComputeHash($updatePackageStream)
$updatePackageStream.Dispose()


############## Decrypt private key for signature
$updatePackageSignatureEncryptionKeySize = 128
$updatePackageSignatureEncryptionDerivationIterations = 1000
$encryptedUpdatePackageSignatureXml = [IO.File]::ReadAllText($updatePackageSignaturePrivateKeyFilePath)
$encryptedXmlBytesWithSaltAndIv = [Convert]::FromBase64String($encryptedUpdatePackageSignatureXml)
$saltStringBytes = $encryptedXmlBytesWithSaltAndIv[0..(($updatePackageSignatureEncryptionKeySize / 8) - 1)]
$ivStringBytes = $encryptedXmlBytesWithSaltAndIv[($updatePackageSignatureEncryptionKeySize / 8)..((($updatePackageSignatureEncryptionKeySize / 8) * 2) -1 )]
$cipherTextBytes = $encryptedXmlBytesWithSaltAndIv[(($updatePackageSignatureEncryptionKeySize / 8) * 2)..($encryptedXmlBytesWithSaltAndIv.Length - 1)]

$aes = [System.Security.Cryptography.AES]::Create()
$aes.BlockSize = 128
$aes.Mode = [System.Security.Cryptography.CipherMode]::CBC
$aes.Padding = [System.Security.Cryptography.PaddingMode]::PKCS7

$updatePackageSignaturePlainPassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($updatePackageSignatureSecurePassword))
$passwordBytes = New-Object -TypeName System.Security.Cryptography.Rfc2898DeriveBytes -ArgumentList ($updatePackageSignaturePlainPassword, $saltStringBytes, $updatePackageSignatureEncryptionDerivationIterations, [System.Security.Cryptography.HashAlgorithmName]::SHA256)
$keyBytes = $passwordBytes.GetBytes($updatePackageSignatureEncryptionKeySize / 8)

$decryptor = $aes.CreateDecryptor($keyBytes, $ivStringBytes)
$cipherTextMemoryStream = New-Object -TypeName System.IO.MemoryStream -ArgumentList (,$cipherTextBytes)
$cryptoStream = New-Object -TypeName System.Security.Cryptography.CryptoStream -ArgumentList ($cipherTextMemoryStream, $decryptor, [System.Security.Cryptography.CryptoStreamMode]::Read)
$plainUpdatePackageSignatureXmlBytes = [System.Byte[]]::CreateInstance([System.Byte], $cipherTextBytes.Length)
$decryptedByteCount = 0

try {
    $decryptedByteCount = $cryptoStream.Read($plainUpdatePackageSignatureXmlBytes, 0, $plainUpdatePackageSignatureXmlBytes.Length)
} catch {
    Write-Host "Error: Could not decrypt signature key"
    exit 1
}

$cryptoStream.Dispose()
$cipherTextMemoryStream.Dispose()
$decryptor.Dispose()
$passwordBytes.Dispose()
$aes.Dispose()

$plainUpdatePackageSignatureXmlString = [System.Text.Encoding]::UTF8.GetString($plainUpdatePackageSignatureXmlBytes, 0, $decryptedByteCount)


############## Create signed checksum
$rsa = [System.Security.Cryptography.RSA]::Create()
$rsa.FromXmlString($plainUpdatePackageSignatureXmlString)

$plainUpdatePackageSignatureXmlString = $null

$rsaFormatter = New-Object -TypeName System.Security.Cryptography.RSAPKCS1SignatureFormatter
$rsaFormatter.SetKey($rsa)
$rsaFormatter.SetHashAlgorithm("SHA256");
$signedHash = $rsaFormatter.CreateSignature($hashResult);
$signedHashBase64 = [Convert]::ToBase64String($signedHash)

[IO.File]::WriteAllText($hashFilePath, $signedHashBase64)