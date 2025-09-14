using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

public class ImageProcessor
{
    private static readonly HttpClient client = new HttpClient();
    private const string serverUrl = "https://image-upscaling.net";

    public async Task ProcessImages(string clientsFilePath)
    {
        // 1. Hỏi người dùng đường dẫn thư mục chứa ảnh gốc
        Console.WriteLine("Nhập đường dẫn thư mục chứa ảnh gốc:");
        string sourceDirectory = Console.ReadLine();

        if (!Directory.Exists(sourceDirectory))
        {
            Console.WriteLine("Thư mục không tồn tại.");
            return;
        }

        // 2. Tạo thư mục để lưu ảnh đã làm nét
        string enhancedDirectory = Path.Combine(sourceDirectory, "enhanced");
        if (!Directory.Exists(enhancedDirectory))
        {
            Directory.CreateDirectory(enhancedDirectory);
            Console.WriteLine($"Đã tạo thư mục lưu ảnh đã làm nét tại: {enhancedDirectory}");
        }

        // 3. Đọc danh sách client_id từ file text
        var clientIds = File.ReadAllLines(clientsFilePath)
                            .Where(c => !string.IsNullOrWhiteSpace(c))
                            .ToList();

        if (!clientIds.Any())
        {
            Console.WriteLine("File client_id không có dữ liệu.");
            return;
        }

        // 4. Lấy danh sách tất cả các tệp ảnh trong thư mục gốc
        var imageFiles = Directory.GetFiles(sourceDirectory, "*.*", SearchOption.TopDirectoryOnly)
                                  .Where(s => s.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                              s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                              s.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                                  .ToList();

        if (!imageFiles.Any())
        {
            Console.WriteLine("Thư mục không chứa tệp ảnh nào được hỗ trợ (.png, .jpg, .jpeg).");
            return;
        }

        int clientIdIndex = 0;
        int requestCount = 0;

        // 5. Lặp qua từng tệp ảnh trong danh sách
        foreach (var imagePath in imageFiles)
        {
            // Lấy client_id hiện tại dựa trên chỉ số
            string currentClientId = clientIds[clientIdIndex % clientIds.Count];

            Console.WriteLine($"\nĐang xử lý ảnh: {Path.GetFileName(imagePath)} với client_id: {currentClientId}");

            // Thêm cookie client_id vào HttpClient
            var cookieContainer = new System.Net.CookieContainer();
            cookieContainer.Add(new Uri(serverUrl), new System.Net.Cookie("client_id", currentClientId));
            var handler = new HttpClientHandler() { CookieContainer = cookieContainer };
            using (var clientWithCookies = new HttpClient(handler))
            {
                var upscalingUrl = $"{serverUrl}/upscaling_upload";
                var data = new Dictionary<string, string>
                {
                    { "scale", "4" },
                    { "model", "plus" }
                };

                using (var content = new MultipartFormDataContent())
                {
                    foreach (var keyValuePair in data)
                    {
                        content.Add(new StringContent(keyValuePair.Value), $"\"{keyValuePair.Key}\"");
                    }

                    var fileContent = new StreamContent(File.OpenRead(imagePath));
                    content.Add(fileContent, "\"image\"", $"\"{Path.GetFileName(imagePath)}\"");

                    try
                    {
                        var response = await clientWithCookies.PostAsync(upscalingUrl, content);
                        response.EnsureSuccessStatusCode();

                        // Tăng bộ đếm yêu cầu
                        requestCount++;
                        Console.WriteLine($"Đã gửi thành công yêu cầu thứ {requestCount} cho client_id này.");

                        // Chờ quá trình xử lý hoàn tất
                        await WaitForProcessing(clientWithCookies, enhancedDirectory);
                    }
                    catch (HttpRequestException e)
                    {
                        Console.WriteLine($"Lỗi khi gửi yêu cầu: {e.Message}");
                    }
                }
            }

            // Nếu đã đủ 7 yêu cầu, chuyển sang client_id tiếp theo
            if (requestCount >= 7)
            {
                clientIdIndex++;
                requestCount = 0;
                Console.WriteLine($"\nĐã đạt giới hạn 7 yêu cầu. Chuyển sang client_id tiếp theo.");
            }
        }

        Console.WriteLine("\nHoàn tất quá trình xử lý ảnh.");
    }

    private async Task WaitForProcessing(HttpClient client, string enhancedDirectory)
    {
        while (true)
        {
            var statusUrl = $"{serverUrl}/upscaling_get_status";
            try
            {
                var response = await client.GetStringAsync(statusUrl);
                var statusData = JsonConvert.DeserializeObject<dynamic>(response);

                var processedUrls = statusData.processed;
                if (processedUrls != null)
                {
                    foreach (var url in processedUrls)
                    {
                        var imageUrl = (string)url;
                        var fileName = Path.GetFileName(imageUrl).Replace(":", "-");
                        var savePath = Path.Combine(enhancedDirectory, fileName);

                        Console.WriteLine($"Đang tải ảnh đã làm nét: {fileName}");

                        var downloadParams = new Dictionary<string, string> { { "delete_after_download", "" } };
                        var downloadUrl = $"{imageUrl}?{string.Join("&", downloadParams.Select(kv => $"{kv.Key}={kv.Value}"))}";

                        var imageBytes = await client.GetByteArrayAsync(downloadUrl);
                        await File.WriteAllBytesAsync(savePath, imageBytes);
                        Console.WriteLine($"Đã tải và lưu thành công: {fileName}");
                    }
                }

                if (statusData.pending.Count == 0 && statusData.processing.Count == 0 && statusData.processed.Count == 0)
                {
                    break; // Thoát khỏi vòng lặp chờ nếu không còn ảnh nào đang chờ hoặc đang xử lý
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi kiểm tra trạng thái: {ex.Message}");
                break;
            }

            await Task.Delay(1000); // Chờ 1 giây trước khi kiểm tra lại
        }
    }
}