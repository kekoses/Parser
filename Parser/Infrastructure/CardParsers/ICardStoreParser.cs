using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Parser.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Parser.Infrastructure.CardParsers
{
    public interface ICardStoreParser<T> where T : StoreCard
    {
        public Task<T> ParseCard(string cardUrl);
        public IAsyncEnumerable<StoreCard> ParseCards(IEnumerable<string> cardUrls);
        public Task<string> ParseRegion(IElement regionSection);
        public Task<string> ParseCardName(IElement cardNameSection);
        public Task<IEnumerable<string>> ParsePictures(IEnumerable<IElement> pictures);
        public Task<string> ParsePathToCard(IEnumerable<IElement> pathSections);
        public Task<decimal> ParsePrice(IElement priceSection);
        public Task<bool> ParseIsInStock(IElement priceSection);
        public Task ParseCardBlockOnPage(string pageUrl, string host, string outputFilePath);
        public Task ParseSite(string siteUrl, string host,string city,string outputFilePath);
    }
}
