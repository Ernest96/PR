using Csv;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PR
{
    class Report
    {
        public RequestResult ordersRequest;
        public RequestResult categoriesRequest;
        private Categories categories;
        private Orders orders;
        private bool parseError = false;
        private Categories parents;
        private Semaphore sem = new Semaphore(Environment.ProcessorCount, Environment.ProcessorCount);
        private CountdownEvent countEventObject = new CountdownEvent(1);
        private string ordersUrl = Settings.OrdresUrl;
        private string categoriesUrl = Settings.CategoriesUrl;

        public Report()
        {
            categories = new Categories();
            orders = new Orders();
            parents = new Categories();
        }

        public void Print()
        {
            if (parents.list == null || parents.list.Count == 0)
            {
                Logger.Writeln("No registrations", ConsoleColor.White);
                return;
            }

            Logger.Writeln($"\nList from {parents.startDateString} to {parents.endDateString}", ConsoleColor.White);

            foreach (var e in parents.list)
            {
                Print(e.Value);
            }
        }

        private void Print(Category c)
        {
            if (c.childNr == 0)
            {
                Console.WriteLine();
            }

            for (int i = 0; i < c.childNr; ++i)
            {
                Logger.Write("\t", ConsoleColor.White);
            }
            Logger.Write($"-{c.name}", ConsoleColor.White);
            Logger.Writeln($"  {c.total}", ConsoleColor.White);

            foreach (var subCat in c.children.Values)
            {
                Print(subCat);
            }
        }

        public void Fetch()
        {
            var date = EnterAndValidateDate();

            this.GetOrdesAndCategoriesAsync(date.Item1, date.Item2);
            if (categories == null || orders == null)
            {
                Logger.Writeln("Could not generate report. Categories or orders are null", ConsoleColor.Red);
                return;
            }

            this.FindParents();

            if (parents == null || parents.list.Count == 0)
            {
                Logger.Writeln("There are no parents. Check your csv format. ", ConsoleColor.Red);
                return;
            }

            foreach (var p in parents.list)
            {
                BeginBuild(p.Value);
            }
            countEventObject.Signal();
            countEventObject.Wait();

            countEventObject = new CountdownEvent(1);

            foreach (var p in parents.list)
            {
                BeginValidate(p.Value);
            }
            countEventObject.Signal();
            countEventObject.Wait();

            Task.Run(() => Cache());
        }

        private (string, string) EnterAndValidateDate()
        {
            bool inputError;
            DateTime startDate = DateTime.MinValue;
            DateTime endDate = DateTime.MinValue;
            string startDateString;
            string endDateString;

            do
            {
                inputError = false;

                Logger.Write("Enter Start Date for orders (yyyy-mm-dd) :", ConsoleColor.White);
                startDateString = Console.ReadLine();
                try
                {
                    if (DateTime.TryParseExact(startDateString, "yyyy-mm-dd", null, DateTimeStyles.None, out startDate) == false)
                        throw new Exception("");

                    var split = startDateString.Split('-');
                    if (Int32.Parse(split[1]) > 12)
                        throw new Exception("");

                }
                catch (Exception e)
                {
                    inputError = true;
                    Logger.Writeln("End Date is in invalid format. Please try again.", ConsoleColor.Red);
                    continue;
                }

            }
            while (inputError);

            inputError = false;
            do
            {
                inputError = false;

                Logger.Write("Enter End Date for orders (yyyy-mm-dd) :", ConsoleColor.White);
                endDateString = Console.ReadLine();

                try
                {

                    if (DateTime.TryParseExact(endDateString, "yyyy-mm-dd", null, DateTimeStyles.None, out endDate) == false)
                        throw new Exception("");

                    var split = endDateString.Split('-');
                    if (Int32.Parse(split[1]) > 12)
                        throw new Exception("");

                }
                catch (Exception e)
                {
                    inputError = true;
                    Logger.Writeln("End Date is in invalid format. Please try again.", ConsoleColor.Red);
                    continue;
                }

            }
            while (inputError);

            if (startDate > endDate)
            {
                Logger.Writeln("End Date must be bigger than Start Date", ConsoleColor.Red);
                return EnterAndValidateDate();
            }

            parents.startDateString = startDateString;
            parents.endDateString = endDateString;

            return (startDateString, endDateString);
        }

        private void GetOrdesAndCategoriesAsync(string startDate, string endDate)
        {
            ordersUrl = Settings.OrdresUrl + $@"?start={startDate}&end={endDate}";

            Thread ordersThread = new Thread(GetOrders);
            ordersThread.Start();

            Thread categoriesThread = new Thread(GetCategories);
            categoriesThread.Start();

            ordersThread.Join();
            categoriesThread.Join();

            if (ordersRequest == null
                || categoriesRequest == null
                || ordersRequest.responseCode != HttpStatusCode.OK
                || categoriesRequest.responseCode != HttpStatusCode.OK
                || parseError == true)
            {
                throw new Exception("Could not fetch and parse data from servers");
            }

            Logger.Writeln("Data fetch and parse completed", ConsoleColor.Green);
        }

        private void GetOrders()
        {
            Logger.Writeln("Performing GetOrders", ConsoleColor.White);
            ordersRequest = Request.DoGetRequest(ordersUrl);
            Logger.Writeln("GetOrders completed", ConsoleColor.White);

            if (ordersRequest.responseCode != HttpStatusCode.OK)
                return;

            Logger.Writeln("Performing Orders Parse", ConsoleColor.White);
            try
            {
                foreach (ICsvLine line in CsvReader.ReadFromText(ordersRequest.data))
                {
                    orders.AddValidElement(line);
                }
            }
            catch (Exception e)
            {
                Logger.Writeln(e.Message, ConsoleColor.Red);
                parseError = true;
                return;
            }
            Logger.Writeln("Orders Parse Completed", ConsoleColor.White);
        }

        private void GetCategories()
        {
            Logger.Writeln("Performing GetCategories", ConsoleColor.White);
            categoriesRequest = Request.DoGetRequest(categoriesUrl);
            Logger.Writeln("GetCategories completed", ConsoleColor.White);

            if (categoriesRequest.responseCode != HttpStatusCode.OK)
                return;

            Logger.Writeln("Performing Categories Parse", ConsoleColor.White);
            try
            {
                foreach (ICsvLine line in CsvReader.ReadFromText(categoriesRequest.data))
                {
                    categories.AddValidElement(line);
                }
            }
            catch (Exception e)
            {
                Logger.Writeln(e.Message, ConsoleColor.Red);
                parseError = true;
                return;
            }

            Logger.Writeln("Categories Parse Completed", ConsoleColor.White);
        }

        private void FindParents()
        {
            foreach (var x in categories.list)
            {
                if (x.Value.parent_id == 0)
                {
                    x.Value.marked = true;
                    if (orders.list.ContainsKey(x.Value.id))
                    {
                        x.Value.total = orders.list[(x.Value.id)].total;
                    }
                    parents.list[x.Key] = x.Value;
                }
            }
        }

        private void BeginBuild(Category p)
        {
            sem.WaitOne();
            countEventObject.AddCount();
            Thread t = new Thread(new ParameterizedThreadStart(Build));
            t.Start(p);
        }

        private void Build(object obj)
        {
            Category p = (Category)obj;
            foreach (var el in categories.list)
            {
                if (!el.Value.marked && el.Value.parent_id == p.id)
                {
                    el.Value.marked = true;
                    if (orders.list.ContainsKey(el.Value.id))
                    {
                        el.Value.total = orders.list[(el.Value.id)].total;
                    }
                    el.Value.childNr = p.childNr + 1;
                    p.children.TryAdd(el.Key, el.Value);
                    Build(el.Value);
                }
            }

            if (p.parent_id == 0)
            {
                countEventObject.Signal();
                sem.Release();
            }
        }

        private void BeginValidate(Category p)
        {
            sem.WaitOne();
            countEventObject.AddCount();
            Task.Run(() => Validate(p));
        }

        private decimal Validate(Category p)
        {
            decimal s = 0.00m;

            if (p.children == null || p.children.IsEmpty)
                s = p.total;
            else
            {
                foreach (var el in p.children)
                {
                    el.Value.total = Validate(el.Value);
                    s += el.Value.total;
                }

            }

            // is parent
            if (p.childNr == 0)
            {
                p.total = s;
                countEventObject.Signal();
                sem.Release();
            }
            return s;

        }

        public void LoadCache()
        {
            try
            {
                using (StreamReader file = File.OpenText(Settings.CacheFile))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    parents = (Categories)serializer.Deserialize(file, typeof(Categories));
                }
            }
            catch (Exception e)
            {
                Logger.Writeln("Can't load cached data", ConsoleColor.Red);
                Logger.Writeln(e.Message, ConsoleColor.Red);
            }
        }

        private void Cache()
        {
            string data = JsonConvert.SerializeObject(parents);
            string path = Settings.CacheFolder;

            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                using (StreamWriter writetext = new StreamWriter(Settings.CacheFile))
                {
                    writetext.Write(data);
                }
            }
            catch(Exception e)
            {
                Logger.Writeln("Can't cache data", ConsoleColor.Red);
                Logger.Writeln(e.Message, ConsoleColor.Red);

            }

        }        
    }
}
