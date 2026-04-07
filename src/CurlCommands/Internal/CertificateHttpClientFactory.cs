using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;

namespace CurlCommands.Internal;

/// <summary>
/// Provides cached HttpClient instances configured with client certificates.
/// Thread-safe via ConcurrentDictionary.
/// </summary>
internal static class CertificateHttpClientFactory
{
  private static readonly ConcurrentDictionary<string, HttpClient> _clients = new();

  /// <summary>
  /// Gets or creates an HttpClient configured with a client certificate.
  /// </summary>
  /// <param name="certFile">Path to the certificate file (PEM or PFX/PKCS12).</param>
  /// <param name="keyFile">Optional path to the private key file (for PEM certs).</param>
  /// <param name="password">Optional certificate password.</param>
  /// <param name="insecure">Whether to skip server certificate validation.</param>
  public static HttpClient GetClient(
    string certFile,
    string? keyFile = null,
    string? password = null,
    bool insecure = false)
  {
    if (!File.Exists(certFile))
    {
      throw new FileNotFoundException($"Certificate file not found: '{certFile}'.", certFile);
    }

    string cacheKey = $"{certFile}|{keyFile ?? ""}|{password ?? ""}|{insecure}";
    return _clients.GetOrAdd(cacheKey, _ => CreateClient(certFile, keyFile, password, insecure));
  }

  private static HttpClient CreateClient(
    string certFile,
    string? keyFile,
    string? password,
    bool insecure)
  {
    X509Certificate2 certificate = LoadCertificate(certFile, keyFile, password);

    HttpClientHandler handler = new();
    handler.ClientCertificates.Add(certificate);

    if (insecure)
    {
      handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
    }

    return new HttpClient(handler);
  }

  private static X509Certificate2 LoadCertificate(string certFile, string? keyFile, string? password)
  {
#if NET10_0_OR_GREATER
    if (keyFile != null)
    {
      X509Certificate2 cert = X509Certificate2.CreateFromPemFile(certFile, keyFile);
      return X509CertificateLoader.LoadPkcs12(cert.Export(X509ContentType.Pfx), null);
    }

    if (certFile.EndsWith(".pem", StringComparison.OrdinalIgnoreCase)
        || certFile.EndsWith(".crt", StringComparison.OrdinalIgnoreCase))
    {
      X509Certificate2 cert = X509Certificate2.CreateFromPemFile(certFile);
      return X509CertificateLoader.LoadPkcs12(cert.Export(X509ContentType.Pfx), null);
    }

    byte[] pfxBytes = File.ReadAllBytes(certFile);
    return password != null
      ? X509CertificateLoader.LoadPkcs12(pfxBytes, password)
      : X509CertificateLoader.LoadPkcs12(pfxBytes, null);
#else
    // netstandard2.0 fallback
    return password != null
      ? new X509Certificate2(certFile, password)
      : new X509Certificate2(certFile);
#endif
  }
}
