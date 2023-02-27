using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using nanoFramework.Networking;
using nf_ssl;

var debuggerWaitPeriod = TimeSpan.FromMilliseconds(300);
while (!Debugger.IsAttached)
{
    Thread.Sleep(debuggerWaitPeriod);
}

// either configure WiFi here or using "Edit Network Configuration"
string wifiSsid = null;
string wifiPassword = null;

var cs = new CancellationTokenSource(TimeSpan.FromSeconds(30));
// ReSharper disable twice ExpressionIsAlwaysNull
ConnectWiFi(cs, wifiSsid, wifiPassword);

// check if we configured a storageAccountToken in resources,
// if not, prepare variable token with "string.empty", otherwise ?{token}:
var storageAccountToken = Resource.GetString(Resource.StringResources.storageAccountToken);
var token = string.IsNullOrEmpty(storageAccountToken)
    ? string.Empty
    : $"?{storageAccountToken}";

// prepare fileNames with array of file names:
var storageAccountFileNames = Resource.GetString(Resource.StringResources.storageAccountFileNames);
var fileNames = storageAccountFileNames.Split(';');

// fetch baseUri:
var baseUri = Resource.GetString(Resource.StringResources.storageAccountBaseUri);

var digiCertGlobalRootG2 = new X509Certificate(Resource.GetBytes(Resource.BinaryResources.DigiCertGlobalRootG2));

// HTTP client is meant to have a single instance throughout the app life cycle 
var httpClient = new HttpClient
{
    SslProtocols = SslProtocols.Tls12,
    HttpsAuthentCert = digiCertGlobalRootG2,
};

foreach (var fileName in fileNames)
{
    nanoFramework.Runtime.Native.GC.EnableGCMessages(true);
    Debug.WriteLine($"Free memory = {nanoFramework.Runtime.Native.GC.Run(true)}");

    Trace($"Trying to fetch file with name: {fileName}");

    try
    {
        var address = $"{baseUri}{fileName}{token}";
        var result = httpClient.Get(address);
        result.EnsureSuccessStatusCode();

        using var fs = new FileStream($"I:\\{fileName}", FileMode.Create, FileAccess.ReadWrite);
        result.Content.ReadAsStream().CopyTo(fs);
        fs.Flush();

        Trace($"Successfully fetched and saved file with name: {fileName}");
    }
    catch (SocketException sex)
    {
        Trace($"Could not fetch file with name: {fileName}");
        Trace($"{sex.ErrorCode}");
        Trace(sex.Message);
    }
    catch (HttpRequestException httpEx)
    {
        Trace($"Could not fetch file with name: {fileName}");
        Trace($"{httpEx.InnerException}");
        Trace(httpEx.Message);

        var wex = httpEx.InnerException as WebException;
        if (wex != null)
        {
            Trace($"{wex.Status}");
            Trace($"{wex.Message}");
            Trace($"{wex.InnerException}");

            var sex = wex.InnerException as SocketException;
            if (sex != null)
            {
                Trace($"{sex.ErrorCode}");
            }
        }
    }
    catch (Exception ex)
    {
        Trace($"Could not fetch file with name: {fileName}");
        Trace(ex.Message);
        Trace(ex.ToString());
    }
}

Thread.Sleep(Timeout.Infinite);

// if ssid & password are set, this method tries to do a connect with these values
// otherwise it tries to "re-connect" (therefore the device needs to be configured
// for this to work: "Edit Network Configuration")
void ConnectWiFi(CancellationTokenSource cancellationTokenSource, string ssid, string password)
{
    var success = !string.IsNullOrEmpty(ssid) && !string.IsNullOrEmpty(password)
        ? WifiNetworkHelper.ConnectDhcp(ssid, password, requiresDateTime: true,
            token: cancellationTokenSource.Token)
        : WifiNetworkHelper.Reconnect(true, token: cancellationTokenSource.Token);

    if (success) return;

    Trace($"Can't connect to wifi: {WifiNetworkHelper.Status}, Exception: {WifiNetworkHelper.HelperException}");
}

void Trace(string message)
{
    Debug.WriteLine(message);
}