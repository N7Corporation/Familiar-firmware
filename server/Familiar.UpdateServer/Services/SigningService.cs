using System.Security.Cryptography;
using Familiar.UpdateServer.Options;
using Microsoft.Extensions.Options;

namespace Familiar.UpdateServer.Services;

public interface ISigningService
{
    string SignData(byte[] data);
    string SignFile(string filePath);
    bool VerifySignature(byte[] data, string signature);
    bool VerifyFileSignature(string filePath, string signature);
    string GetPublicKey();
    void GenerateKeyPair();
}

public class SigningService : ISigningService
{
    private readonly UpdateServerOptions _options;
    private readonly ILogger<SigningService> _logger;
    private RSA? _privateKey;
    private RSA? _publicKey;

    public SigningService(IOptions<UpdateServerOptions> options, ILogger<SigningService> logger)
    {
        _options = options.Value;
        _logger = logger;
        LoadKeys();
    }

    private void LoadKeys()
    {
        if (File.Exists(_options.PrivateKeyPath))
        {
            try
            {
                var privateKeyPem = File.ReadAllText(_options.PrivateKeyPath);
                _privateKey = RSA.Create();
                _privateKey.ImportFromPem(privateKeyPem);
                _logger.LogInformation("Loaded private key from {Path}", _options.PrivateKeyPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load private key from {Path}", _options.PrivateKeyPath);
            }
        }

        if (File.Exists(_options.PublicKeyPath))
        {
            try
            {
                var publicKeyPem = File.ReadAllText(_options.PublicKeyPath);
                _publicKey = RSA.Create();
                _publicKey.ImportFromPem(publicKeyPem);
                _logger.LogInformation("Loaded public key from {Path}", _options.PublicKeyPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load public key from {Path}", _options.PublicKeyPath);
            }
        }
    }

    public void GenerateKeyPair()
    {
        var rsa = RSA.Create(4096);

        var privateKeyPem = rsa.ExportRSAPrivateKeyPem();
        var publicKeyPem = rsa.ExportRSAPublicKeyPem();

        var keysDir = Path.GetDirectoryName(_options.PrivateKeyPath);
        if (!string.IsNullOrEmpty(keysDir) && !Directory.Exists(keysDir))
        {
            Directory.CreateDirectory(keysDir);
        }

        File.WriteAllText(_options.PrivateKeyPath, privateKeyPem);
        File.WriteAllText(_options.PublicKeyPath, publicKeyPem);

        _privateKey = rsa;
        _publicKey = RSA.Create();
        _publicKey.ImportFromPem(publicKeyPem);

        _logger.LogInformation("Generated new RSA key pair");
    }

    public string SignData(byte[] data)
    {
        if (_privateKey == null)
        {
            throw new InvalidOperationException("Private key not loaded. Generate or load a key pair first.");
        }

        var signature = _privateKey.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(signature);
    }

    public string SignFile(string filePath)
    {
        var data = File.ReadAllBytes(filePath);
        return SignData(data);
    }

    public bool VerifySignature(byte[] data, string signature)
    {
        if (_publicKey == null)
        {
            throw new InvalidOperationException("Public key not loaded.");
        }

        try
        {
            var signatureBytes = Convert.FromBase64String(signature);
            return _publicKey.VerifyData(data, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Signature verification failed");
            return false;
        }
    }

    public bool VerifyFileSignature(string filePath, string signature)
    {
        var data = File.ReadAllBytes(filePath);
        return VerifySignature(data, signature);
    }

    public string GetPublicKey()
    {
        if (_publicKey == null)
        {
            throw new InvalidOperationException("Public key not loaded.");
        }

        return _publicKey.ExportRSAPublicKeyPem();
    }
}
