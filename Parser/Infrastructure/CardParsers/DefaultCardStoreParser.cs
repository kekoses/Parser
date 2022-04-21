using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Parser.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using System.IO;
using CsvHelper.Configuration;
using System.Threading;

namespace Parser.Infrastructure.CardParsers
{
    public class DefaultCardStoreParser<T> : ICardStoreParser<StoreCard>
    {
        #region Constants
        private const string pictureNameClass = "card-slider-nav";
        private const string regionNameClass = "select-city-link";
        private const string cardNameClass = "detail-name";
        private const string actualPrice = "price";
        private const string previousPrice = "old-price";
        private const string breadscrumbClass = "breadcrumb-item";
        private const string outInStock = "net-v-nalichii";
        private const string inStock = "ok";
        private const string productDetailClass = "detail-block";
        private const string cardClasses = "d-block p-1 product-name gtm-click";
        private const string pageClass = "page-item";
        private const string cityLinksClass = "region-links";
        #endregion
        private readonly HtmlParser _parser;
        public DefaultCardStoreParser()
        {
            _parser = new HtmlParser();
        }
        public int Page { get; set; }
        public double CurrentCardCount { get; set; }
        public double CardCount { get; set; }
        public string Host { get; set; }
        public async Task<StoreCard> ParseCard(string cardUrl)
        {
            HttpClient client = CustomHttpClientFactory.CreateClient();
            if (string.IsNullOrEmpty(cardUrl)) 
            {
                throw new ArgumentNullException(nameof(cardUrl));
            }
            var response = await client.GetAsync(cardUrl);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(content)) 
                {
                    throw new ArgumentNullException(nameof(content));
                }
                var cardElements = await _parser.ParseDocumentAsync(content);
                var parsedCard = new StoreCard();
                parsedCard.Name = await ParseCardName(cardElements.GetElementsByClassName(cardNameClass)[0]);
                parsedCard.Region = await ParseRegion(cardElements.GetElementsByClassName(regionNameClass)[0]);
                parsedCard.ActualPrice = await ParsePrice(cardElements.GetElementsByClassName(actualPrice)[0]);
                var previuosPriceSection = cardElements.GetElementsByClassName(previousPrice);
                if(previuosPriceSection.Length > 0)
                {
                    parsedCard.PreviousPrice = await ParsePrice(cardElements.GetElementsByClassName(previousPrice)[0]);
                }
                parsedCard.PictureLinks = await ParsePictures(cardElements.GetElementsByClassName(pictureNameClass)[0].Children);
                parsedCard.Path = await ParsePathToCard(cardElements.GetElementsByClassName(breadscrumbClass));
                parsedCard.IsInStock = await ParseIsInStock(cardElements.GetElementsByClassName(productDetailClass)[0]);
                parsedCard.CardLink = cardUrl;
                CurrentCardCount += 1;
                Console.WriteLine($"Current procces {CurrentCardCount/CardCount:p2}");
                Console.SetCursorPosition(0, 2);
                return parsedCard;
            }
            else 
            {
                throw new InvalidOperationException($"Bad request to {cardUrl} from ParseCard method");
            }
        }
        public async Task ParseCardBlockOnPage(string pageUrl, string host, string outputFilePath)
        {
            if(string.IsNullOrEmpty(pageUrl)) 
            {
                throw new ArgumentNullException(nameof(pageUrl));
            }
            var client = CustomHttpClientFactory.CreateClient();
            var response = await client.GetAsync(pageUrl);
            if (response.IsSuccessStatusCode)
            {
                using var sw = new StreamWriter(outputFilePath);
                using CsvWriter writer = new CsvWriter(sw, new CsvConfiguration(Thread.CurrentThread.CurrentCulture));
                var content = await response.Content.ReadAsStringAsync();
                var document = await _parser.ParseDocumentAsync(content);
                var cardUrls = document.GetElementsByClassName(cardClasses).Cast<IHtmlAnchorElement>().Select(a => a.Href.Replace("about://", host));
                await writer.WriteRecordsAsync(ParseCards(cardUrls));
                return;
            }
            throw new InvalidOperationException($"Bad request to {pageUrl} from ParseCardBlockOnPage method");      
        }
        public Task<string> ParseCardName(IElement cardNameSection)
        {
            if(cardNameSection is not null) 
            {
                return Task.FromResult(cardNameSection.TextContent);
            }
            throw new ArgumentNullException(nameof(cardNameSection));
        }
        public async IAsyncEnumerable<StoreCard> ParseCards(IEnumerable<string> cardUrls)
        {
            CardCount = cardUrls.Count();
            foreach (var cardUrl in cardUrls)
            {
                yield return await ParseCard(cardUrl);
            }
        }
        public Task<bool> ParseIsInStock(IElement priceSection)
        {
            if (priceSection.GetElementsByClassName(outInStock).Length>0) 
            {
                return Task.FromResult(false);
            }
            if (priceSection.GetElementsByClassName(inStock).Length > 0) 
            {
                return Task.FromResult(true);
            }
            throw new InvalidOperationException("Cannot parse is it in stock or not!");
        }
        public Task<string> ParsePathToCard(IEnumerable<IElement> pathSections)
        {
            var builder = new StringBuilder();
            foreach (var pathSec in pathSections.Take(pathSections.Count()-1))
            {
                if(!string.IsNullOrEmpty(pathSec.LastChild.TextContent)) 
                {
                    builder.Append(pathSec.LastChild.TextContent.Trim());
                    builder.Append("-");
                }
            }
            builder.Remove(builder.Length - 1, 1);
            return Task.FromResult(builder.ToString());
        }
        public Task<IEnumerable<string>> ParsePictures(IEnumerable<IElement> pictures)
        {
            if (pictures.Count() == 0) 
            {
                throw new InvalidOperationException("Pictures for parsing weren't found!");
            }
            var listLinkPictures = new List<string>();
            foreach (var picture in pictures)
            {
                IHtmlImageElement img=picture.LastElementChild as IHtmlImageElement;
                if(img is null) 
                {
                    throw new InvalidOperationException($"Cannot find a picture for parsing in {nameof(picture)} !");
                }
                else 
                {
                    listLinkPictures.Add(img.Source);
                }
            }
            return Task.FromResult(listLinkPictures.AsEnumerable());
        }
        public Task<decimal> ParsePrice(IElement priceSection)
        {
            if(priceSection is null) 
            {
                throw new ArgumentNullException(nameof(priceSection));
            }
            var builder = new StringBuilder(priceSection.TextContent);
            builder.Replace(" ", "");
            builder.Replace("руб.", "");
            if (decimal.TryParse(builder.ToString(), out decimal previousPriceValue)) 
            {
                return Task.FromResult(previousPriceValue);
            }
            return Task.FromResult(0m);
        }
        public Task<string> ParseRegion(IElement regionSection)
        {
            if (regionSection.ChildElementCount == 0) 
            {
                throw new InvalidOperationException($"Cannot parse region info!");
            }
            string regionName = regionSection.GetElementsByTagName("a")[0].TextContent.Trim(' ', '\n','\t');
            if (string.IsNullOrEmpty(regionName)) 
            {
                throw new InvalidOperationException($"Parse proccess has failed!");
            }
            return Task.FromResult(regionName);
        }

        public async Task ParseSite(string siteUrl, string host,string city, string outputFilePath) 
        {
            if (string.IsNullOrEmpty(host))
            {
                throw new ArgumentNullException(nameof(host));
            }
            if (string.IsNullOrEmpty(siteUrl))
            {
                throw new ArgumentNullException(nameof(siteUrl));
            }
            var client = CustomHttpClientFactory.CreateClient();
            var response = await client.GetAsync(siteUrl);
            if (response.IsSuccessStatusCode) 
            {
                var content = await response.Content.ReadAsStringAsync();
                var document = await _parser.ParseDocumentAsync(content);
                var pageUrls = document.GetElementsByClassName(pageClass).Skip(2).SkipLast(1)
                                                                         .Select(e => (e.Children[0] as IHtmlAnchorElement).Href.Replace("about://", host));
                foreach (var pageUrl in pageUrls)
                {
                    Page += 1;
                    CurrentCardCount = 0;
                    Console.WriteLine($"Parsing {Page} page");
                    await ParseCardBlockOnPage(pageUrl, host, outputFilePath);
                    Console.SetCursorPosition(0, 1);
                }
                Console.SetCursorPosition(0, 3);
                return;
            }
            throw new InvalidOperationException($"Bad request to {siteUrl} from ParseSite method");
        }
    }
}
