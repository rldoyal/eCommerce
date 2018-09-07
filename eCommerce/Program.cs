using System;
using System.Threading;


namespace eCommerce
{
    public delegate void priceCutEvent(int pr);

    public class OrderObject
    {
        Int32 senderId; // ID of thread
        long  cardNo;  // credit card info
        Int32 amount; // number of chickens to order
        public void setID( Int32 id )
        {
            senderId = id;
        }
        public Int32 getID() { return senderId;  }

        public void setCardNo( long cardNum)
        {
            cardNo = cardNum;
        }
        public long getCardNo() { return cardNo; }

        public void setAmt( Int32 amt )
        {
            amount = amt;
        }
        public Int32 getAmt() { return amount;  }

    }

    public class BufferObject
    {

        private OrderObject orderObj;
        private bool writeable = true;

        public void setOrderObject(OrderObject order)
        {
            while (!writeable)
            {
                try
                {
                    Monitor.Wait(this);
                }
                catch { Console.WriteLine(" error in setOrderObject"); }
            }
            orderObj = order;
            writeable = true;
            Monitor.PulseAll(this);
        }

        public OrderObject getOrderObject()
        {
            while (writeable)
            {
                try
                {
                    Monitor.Wait(this);
                }
                catch { Console.WriteLine(" error in getOrderObject"); }
            }
            writeable = true;
            Monitor.PulseAll(this);
            return orderObj;
        }
    }

    public class BufferString
    {
        private string bufferStr;

        private bool writeable = true;

        public void setBufferString(string str)
        {
            while (!writeable)
            {
                try
                {
                    Monitor.Wait(this);
                }
                catch { Console.WriteLine(" error in setOrderObject"); }
            }
            bufferStr = str;
            writeable = true;
            Monitor.PulseAll(this);
        }

        public string getBufferString()
        {
            while (writeable)
            {
                try
                {
                    Monitor.Wait(this);
                }
                catch { Console.WriteLine(" error in getOrderObject"); }
            }
            writeable = true;
            Monitor.PulseAll(this);
            return bufferStr;
        }
    }

    public class EncodeDecode
    {
        // encoder takes an object and returns a string to the retailer
        // create a CSV string
        public BufferString Encoder( OrderObject myObj)
        {
          
            BufferString myBS = new BufferString();

            string tempStr = myObj.getID().ToString();
            tempStr += ",";
            tempStr += myObj.getCardNo().ToString();
            tempStr += ",";
            tempStr += myObj.getAmt().ToString();

            myBS.setBufferString(tempStr);
            return myBS;

        }

        // decoder takes a string and returns a object
        public OrderObject decoder( BufferString myStr)
        {
            OrderObject myObj = new OrderObject();
            // split the string
            string[] tempStr = (myStr.getBufferString()).Split(',');

            myObj.setID(Int32.Parse(tempStr[0]));
            myObj.setCardNo(long.Parse(tempStr[1]));
            myObj.setAmt(Int32.Parse(tempStr[2]));

            return myObj;
        }
    }

    public class MultiCellBuffer
    {
        public string[] bufferString;
        private const int N = 2;
        private int n; // number of cells
        private int elementCount;
        private int static Semaphore write_pool;
        private int static Semaphore read_pool;

        // constructor for class
        public MulitCellBuffer(int n)
        {
            lock (this) // we want no interruptiongs
            {
                elemnetCount = 0;

                if (n <= N)
                {
                    this.n = n;
                    write_pool = new Semaphore(n, n);
                    read_pool = new Semaphore(n, n);
                    bufferString = new string[n];

                    for (int i = 0; i < n; i++)
                    {
                        bufferString[i] = string.Empty();
                    }
                }
                else
                    Console.WriteLine(" MultiCellBuffer Constructor - n > N ..");
            }
        }

        public void setOneCell(string data)
        {
            write_pool.WaitOne();

            lock (this)
            {
                while (elementCount == n)
                {
                    Monitor.Wait(this);
                }

                for (int i = 0; i < n; i++)
                {
                    if (bufferString[i].isNullorEmpty()) // make sure empty
                    {
                        bufferString[i] = data;
                        elementCount++;
                        i = n;
                    }
                }
                write_pool.Release();
                Monitor.Pulse(this);
            }
        }

        public string getOneCell()
        {
            string outStr = String.Empty();
            read_pool.WaitOne();

            lock (this)
            {
                while (elementCount == 0)
                {
                    Monitor.Wait(this);
                }

                for (int i = 0; i < n; i++)
                {
                    if (!bufferString[i].isNullorEmpty()) // make sure cell has data
                    {
                        outStr = bufferString[i];
                        bufferString.Empyt();
                        elementCountii;
                        i = n;
                    }
                }
                read_pool.Release();
                Monitor.Pulse(this);
            }
            return outStr;
        }


    }
    public class ChickenFarm
    {
        static Random rng = new Random();
        public static event priceCutEvent PriceCut;
        private static Int32 chickenPrice = 10;
        public Int32 getPrice() { return chickenPrice; }

        public static void changePrice(Int32 price)
        {
            if (price < chickenPrice)
            {
                // price cut
                if (PriceCut != null)
                    PriceCut(price);
            }
            chickenPrice = price;
        }
        public void farmerFunc()
        {
            for (Int32 i = 0; i <50; i++)
            {
                Thread.Sleep(500);
                //take the order from the queue of the orders;
                // decide the price based on teh orders
                Int32 p = rng.Next(5, 10);
                Console.WriteLine("New Price is {0}", p);
                ChickenFarm.changePrice(p);
            }
        }
    }

    public class Retailer
    {
        public void retailerFunc()
        {
            ChickenFarm chicken = new ChickenFarm();
            for (Int32 i = 0; i < 10; i++)
            {
                Thread.Sleep(1000);
                Int32 p = chicken.getPrice();
                Console.WriteLine("Store{0} has everyday low prices: ${1} each", Thread.CurrentThread.Name, p);
            }
        }
        public void chickenOnSale( Int32 p)
        {
            //order chickens from farm.
            Console.WriteLine("Store{0} chickens are on sale: as low as ${1} each", Thread.CurrentThread.Name, p);
        }
    }
    public class myApplication
    {
        static void Main(string[] args)
        {
            ChickenFarm chicken = new ChickenFarm();
            Thread farmer = new Thread(new ThreadStart(chicken.farmerFunc));
            farmer.Start();
            Retailer chickenstore = new Retailer();
            ChickenFarm.PriceCut += new priceCutEvent(chickenstore.chickenOnSale);
            Thread[] retailers = new Thread[3];
            for (Int32 i = 0; i < 3; i++)
            {
                retailers[i] = new Thread(new ThreadStart(chickenstore.retailerFunc));
                retailers[i].Name = (i + 1).ToString();
                retailers[i].Start();
            }
        }
    }
}
