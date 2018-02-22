using Csv;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace PR
{
    class Categories
    {
        public ConcurrentDictionary<int, Category> list = new ConcurrentDictionary<int, Category>();
        public string startDateString;
        public string endDateString;

        public void AddValidElement(ICsvLine line)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(line["name"])
                || String.IsNullOrWhiteSpace(line["id"]))
                {
                    return;
                }

                Category cat = new Category(line);
                list[Int32.Parse(line["id"])] = cat;
            }
            catch (Exception e)
            {
                // do nothing element is invalid
                return;
            }
        }
    }
}
