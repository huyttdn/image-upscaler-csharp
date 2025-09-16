class Program
{
    static async Task Main(string[] args)
    {
        var processor = new ImageProcessor();
        await processor.ProcessImages("client_ids.txt");
    }
}