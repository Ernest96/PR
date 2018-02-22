using Csv;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace PR
{
    class Orders
    {
        public ConcurrentDictionary<int, Order> list = new ConcurrentDictionary<int, Order>();

        public void AddValidElement(ICsvLine line)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(line["total"])
                || String.IsNullOrWhiteSpace(line["id"])
                || String.IsNullOrWhiteSpace(line["category_id"]))
                {
                    return;
                }

                Order ord = new Order(line);

                if (list.ContainsKey(Int32.Parse(line["category_id"])))
                {
                    list[Int32.Parse(line["category_id"])].total += ord.total;
                }
                else
                {
                    list[Int32.Parse(line["category_id"])] = ord;
                }
            }
            catch (Exception e)
            {
                //do nothing, element is invalid
                return;
            }
        }
    }
}
