using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

public class QrzLogbookService
{
    private static readonly HttpClient _client;
    private const string QrzApiUrl = "https://logbook.qrz.com/api";

    static QrzLogbookService()
    {
        _client = new HttpClient();

        // CRITICAL: QRZ requires an identifiable User-Agent.
        // Format: ApplicationName/Version (Callsign)
        _client.DefaultRequestHeaders.UserAgent.ParseAdd("MyWpfLogApp/1.0.0 (YOURCALL)");
    }

    /// <summary>
    /// Pushes a single QSO log entry to QRZ.com.
    /// </summary>
    /// <param name="apiKey">Your unique QRZ Logbook API Key</param>
    /// <param name="adifData">The contact data in standard ADIF format</param>
    /// <returns>The raw XML string response from QRZ</returns>
    public async Task<string> PushLogEntryAsync(string apiKey, string adifData)
    {
        // Ensure the ADIF entry ends with the End of Record tag
        if (!adifData.EndsWith("<eor>", StringComparison.OrdinalIgnoreCase))
        {
            adifData += "<eor>";
        }

        // Construct the post parameters requested by QRZ
        var postData = new Dictionary<string, string>
        {
            { "KEY", apiKey },
            { "ACTION", "INSERT" },
            { "ADIF", adifData }
        };

        try
        {
            var content = new FormUrlEncodedContent(postData);

            HttpResponseMessage response = await _client.PostAsync(QrzApiUrl, content);
            response.EnsureSuccessStatusCode();

            // QRZ returns data in an XML payload string
            string xmlResponse = await response.Content.ReadAsStringAsync();
            return xmlResponse;
        }
        catch (Exception ex)
        {
            // Handle or log exceptions appropriately in your WPF app
            return $"Error: {ex.Message}";
        }
    }
}