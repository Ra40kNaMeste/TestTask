using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WildberriesAPI
{
    class Color
    {
        public string name { get; set; }
        public int id { get; set; }
    }

    class Data
    {
        public List<Product> products { get; set; }
    }

    class Price
    {
        public int basic { get; set; }
        public int product { get; set; }
        public int total { get; set; }
        public int logistics { get; set; }
        public int @return { get; set; }
    }

    class Product
    {
        public int id { get; set; }
        public int root { get; set; }
        public int kindId { get; set; }
        public string brand { get; set; }
        public int brandId { get; set; }
        public int siteBrandId { get; set; }
        public List<Color> colors { get; set; }
        public int subjectId { get; set; }
        public int subjectParentId { get; set; }
        public string name { get; set; }
        public string supplier { get; set; }
        public int supplierId { get; set; }
        public double supplierRating { get; set; }
        public int supplierFlags { get; set; }
        public int pics { get; set; }
        public int rating { get; set; }
        public double reviewRating { get; set; }
        public int feedbacks { get; set; }
        public int volume { get; set; }
        public int viewFlags { get; set; }
        public List<int> promotions { get; set; }
        public List<Size> sizes { get; set; }
        public int time1 { get; set; }
        public int time2 { get; set; }
        public int wh { get; set; }
        public int dtype { get; set; }
    }

    class CardsResponseData
    {
        public int state { get; set; }
        public int payloadVersion { get; set; }
        public Data data { get; set; }
    }

    class Size
    {
        public string name { get; set; }
        public string origName { get; set; }
        public int rank { get; set; }
        public int optionId { get; set; }
        public List<Stock> stocks { get; set; }
        public int time1 { get; set; }
        public int time2 { get; set; }
        public int wh { get; set; }
        public int dtype { get; set; }
        public Price price { get; set; }
        public int saleConditions { get; set; }
        public string payload { get; set; }
    }

    class Stock
    {
        public int wh { get; set; }
        public int dtype { get; set; }
        public int qty { get; set; }
        public int priority { get; set; }
        public int time1 { get; set; }
        public int time2 { get; set; }
    }
}
