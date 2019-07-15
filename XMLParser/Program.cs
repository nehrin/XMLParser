namespace XMLParser
{
    class Program
    {
        static void Main(string[] args)
        {
            string text = args[0];

            ExtractionHelper extractionHelper = new ExtractionHelper(text);
            extractionHelper.extractAsync().GetAwaiter().GetResult();
        }

    }
}
