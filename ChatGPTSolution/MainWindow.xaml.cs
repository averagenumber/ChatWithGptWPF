using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace ChatGPTSolution;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private string[] requests = [];
    private string[] answers = [];

    public MainWindow()
    {
        InitializeComponent();

        try
        {
            string[] filePaths = Directory.GetFiles("Log", "*.json", SearchOption.TopDirectoryOnly);
            
            
            foreach (string filePath in filePaths)
            {
                string timestamp = Path.GetFileNameWithoutExtension(filePath);
                string format = "MMddyyyyhhmmsstt";
                if (DateTime.TryParseExact(timestamp, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                {
                    CreateButton($"Chat {Path.GetFileName(parsedDate.ToString())}", Path.GetFileName(filePath));
                }
                else
                {
                    Console.WriteLine("Failed to parse date.");
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }


    private void CreateButton(string content,string datetime)
    {
        Button button = new Button
        {
            Content = content,
            CommandParameter = datetime,
            Foreground = Brushes.White,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            
        };
        
        button.Click += LoadChat_Click;
        
        Border border = new Border
        {
            CornerRadius = new System.Windows.CornerRadius(5), // Rounded corners
            Background = new SolidColorBrush(Color.FromRgb(57, 82, 63)), // Background color
            Padding = new System.Windows.Thickness(5) // Padding inside the border
        };
        
        border.Child = button;
        
        ListBoxContainer.Items.Add(border);
    }
    private void SendRequest_Click(object sender, RoutedEventArgs e)
    {
        string request = RequestBox.Text;
        if (string.IsNullOrEmpty(request)) {MessageBox.Show("Request is empty");}
        string answer = GetChatGptResponse(request);
        
        Array.Resize(ref requests, requests.Length + 1);
        requests[requests.Length - 1] = request;
        Chat.AppendText($"User: {request}\n");
        Array.Resize(ref answers, answers.Length + 1);
        answers[answers.Length - 1] = answer;
        Chat.AppendText($"AI: {answer}\n\n");
        RequestBox.Clear();
    }

    
    private void LoadChat_Click(object sender, RoutedEventArgs e)
    {
        Chat.Clear();
        string text = "";
        
        if (sender is Button button && button.CommandParameter is string parameter)
        {
            text = File.ReadAllText($"Log/{parameter}");
        }
        
        var data = JsonSerializer.Deserialize<Dictionary<string, List<Dictionary<string, object>>>>(text);
        
        List<Dictionary<string, object>> reqList = data["req"];
        List<Dictionary<string, object>> ansList = data["ans"];
        
        string[] requests = new string[reqList.Count];
        string[] answers = new string[ansList.Count];

        for (int i = 0; i < reqList.Count; i++)
        {
            requests[i] = reqList[i]["Key"].ToString();
            answers[i] = ansList[i]["Key"].ToString();
            Chat.AppendText($"User: {requests[i]}\n");
            Chat.AppendText($"AI: {answers[i]}\n\n");
        }
        
        RequestBox.Clear();
    }
    
    private void LoadLast_Click(object sender, RoutedEventArgs e)
    {
        Chat.Clear();
        string text = File.ReadAllText("DataFile.json");
        
        var data = JsonSerializer.Deserialize<Dictionary<string, List<Dictionary<string, object>>>>(text);
        
        List<Dictionary<string, object>> reqList = data["req"];
        List<Dictionary<string, object>> ansList = data["ans"];
        
        string[] requests = new string[reqList.Count];
        string[] answers = new string[ansList.Count];

        for (int i = 0; i < reqList.Count; i++)
        {
            requests[i] = reqList[i]["Key"].ToString();
            answers[i] = ansList[i]["Key"].ToString();
            Chat.AppendText($"User: {requests[i]}\n");
            Chat.AppendText($"AI: {answers[i]}\n\n");
        }
        
        RequestBox.Clear();
    }
    
    private void On_Exit(object? sender, CancelEventArgs e)
    {
        var req = new List<KeyValuePair<string, int>>();
        var ans = new List<KeyValuePair<string, int>>();

        for (int i = 0; i < requests.Length; i++)
        {
            req.Add(new KeyValuePair<string, int>(requests[i], i));
        }
        for (int i = 0; i < answers.Length; i++)
        {
            ans.Add(new KeyValuePair<string, int>(answers[i], i));
        }

        var jsonObject = new { req, ans };
        
        string fileName = "DataFile.json"; 
        string jsonString = JsonSerializer.Serialize(jsonObject);
        File.WriteAllText(fileName, jsonString);
        
        
        string logName = $"Log/{DateTime.Now.ToString("MMddyyyyhhmmsstt")}.json";
        string logDirectory = Path.GetDirectoryName(logName);
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }
        string jsonLog = JsonSerializer.Serialize(jsonObject);
        File.WriteAllText(logName, jsonLog);
    }

    string GetChatGptResponse(string prompt)
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("x-apihub-key", "");
            client.DefaultRequestHeaders.Add("x-apihub-host", "Cheapest-GPT-AI-Summarization.allthingsdev.co");
            client.DefaultRequestHeaders.Add("x-apihub-endpoint", "");
            
            var requestBody = new
            {
                text = prompt,
                length = prompt.Length,
                style = "text"
            };

            // Serialize object to JSON using System.Text.Json
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Make the POST request
            try
            {
                var response = client
                    .PostAsync("https://Cheapest-GPT-AI-Summarization.proxy-production.allthingsdev.co/api/summarize",
                        content).Result;
                var responseContent = response.Content.ReadAsStringAsync().Result;

                // Parse the response content as JSON
                using (JsonDocument doc = JsonDocument.Parse(responseContent))
                {
                    // Access the "result" key value
                    if (doc.RootElement.TryGetProperty("result", out JsonElement result))
                    {
                        return result.GetString();
                    }
                    else
                    {
                        return "ERROR! Result key not found in the response";
                    }
                }
            }
            catch (Exception ex)
            {
                return ("Error: " + ex.Message);
            }
        }
    }
}