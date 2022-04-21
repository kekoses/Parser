using System;
using System.Threading.Tasks;
using Parser.Infrastructure.CardParsers;
using Parser.Models;

namespace Parser
{
    class Program
    {
        static async Task Main(string[] args)
        {
            ICardStoreParser<StoreCard> cardParser = new DefaultCardStoreParser<StoreCard>();
            Console.WriteLine("Start parsing...");
            await cardParser.ParseSite("https://www.toy.ru/catalog/boy_transport/", "https://www.toy.ru","Ростов-на-Дону", @"C:\\Users\KP\Desktop\csv_file.csv");
            Console.WriteLine("Done!");
            Console.ReadLine();

        }
    }
}
