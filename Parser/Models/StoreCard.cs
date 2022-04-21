using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parser.Models
{
    public record StoreCard
    {
        public string Region { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public decimal ActualPrice { get; set; }
        public decimal PreviousPrice { get; set; }
        public bool IsInStock { get; set; }
        public IEnumerable<string> PictureLinks { get; set; }
        public string CardLink { get; set; }
    }
}
