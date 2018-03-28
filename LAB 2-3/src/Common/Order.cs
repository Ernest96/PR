using Csv;
using System;
using System.Collections.Generic;
using System.Text;

namespace PR
{
    class Order
    {
        public int parent_id;
        public string id;
        public decimal total;
        public DateTime created;

        public Order(ICsvLine line)
        {
            this.id = line["id"];
            this.total = Convert.ToDecimal(line["total"], System.Globalization.CultureInfo.InvariantCulture);
            this.parent_id = Int32.Parse(line["category_id"]);
            this.created = DateTime.Parse(line["created"]);
        }
    }
}
