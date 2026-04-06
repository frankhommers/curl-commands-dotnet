using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using CurlCommandParser.Internal;

namespace CurlCommandParser.Tests;

public class CertificateHttpClientFactoryTests
{
  [Fact]
  public void GetClient_WithPfxCertificate_ReturnsHttpClient()
  {
    string pfxPath = CreateTempPfx();
    try
    {
      HttpClient client = CertificateHttpClientFactory.GetClient(pfxPath, password: "testpass");
      Assert.NotNull(client);
    }
    finally
    {
      File.Delete(pfxPath);
    }
  }

  [Fact]
  public void GetClient_WithPemCertAndKey_ReturnsHttpClient()
  {
    (string certPath, string keyPath) = CreateTempPemPair();
    try
    {
      HttpClient client = CertificateHttpClientFactory.GetClient(certPath, keyFile: keyPath);
      Assert.NotNull(client);
    }
    finally
    {
      File.Delete(certPath);
      File.Delete(keyPath);
    }
  }

  [Fact]
  public void GetClient_SameArguments_ReturnsCachedClient()
  {
    string pfxPath = CreateTempPfx();
    try
    {
      HttpClient client1 = CertificateHttpClientFactory.GetClient(pfxPath, password: "testpass");
      HttpClient client2 = CertificateHttpClientFactory.GetClient(pfxPath, password: "testpass");
      Assert.Same(client1, client2);
    }
    finally
    {
      File.Delete(pfxPath);
    }
  }

  [Fact]
  public void GetClient_WithInsecure_ReturnsHttpClient()
  {
    string pfxPath = CreateTempPfx();
    try
    {
      HttpClient client = CertificateHttpClientFactory.GetClient(pfxPath, password: "testpass", insecure: true);
      Assert.NotNull(client);
    }
    finally
    {
      File.Delete(pfxPath);
    }
  }

  [Fact]
  public void GetClient_NonExistentFile_ThrowsFileNotFoundException()
  {
    Assert.Throws<FileNotFoundException>(() =>
      CertificateHttpClientFactory.GetClient("/nonexistent/cert.pfx"));
  }

  private static string CreateTempPfx()
  {
    string pfxPath = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}.pfx");
    using RSA rsa = RSA.Create(2048);
    CertificateRequest req = new("CN=test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    using X509Certificate2 cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(1));
    byte[] pfxBytes = cert.Export(X509ContentType.Pfx, "testpass");
    File.WriteAllBytes(pfxPath, pfxBytes);
    return pfxPath;
  }

  private static (string certPath, string keyPath) CreateTempPemPair()
  {
    string certPath = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}.pem");
    string keyPath = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}.key");
    using RSA rsa = RSA.Create(2048);
    CertificateRequest req = new("CN=test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    using X509Certificate2 cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(1));
    File.WriteAllText(certPath, new string(PemEncoding.Write("CERTIFICATE", cert.RawData)));
    File.WriteAllText(keyPath, new string(PemEncoding.Write("RSA PRIVATE KEY", rsa.ExportRSAPrivateKey())));
    return (certPath, keyPath);
  }
}
