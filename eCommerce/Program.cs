using System;
using System.Threading;


namespace eCommerce
{
    public delegate void priceCutEvent(int pr);
    public delegate void orderProcessedEvent(Int32 SenderID, Int32 amountToChage, Int32 price, Int32 amount);
    public delegate void orderCreatedEvent();

    public class OrderObject
    {
        Int32 senderId; // ID of thread
        long  cardNo;  // credit card info
        Int32 amount; // number of chickens to order

        public OrderObject()
        {

        }
        public OrderObject( Int32 Id, long CNum, Int32 Amt)
        {
            senderId = Id;
            cardNo = CNum;
            amount = Amt;
        }
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

    

    public class BufferString
    {
        private string bufferStr;

        private bool writeable = true;
        public BufferString()
        {
            writeable = true;
            bufferStr = String.Empty;
        }
        public void setBufferString(string str)
        {
           
            bufferStr = str;
            writeable = false;  // has data
          
        }

        public string getBufferString()
        {
         
            writeable = true; // data read
            return bufferStr;
        }

        public bool isWriteable()
        {
            return writeable;
        }

        public void Reset()  // reset the values
        {
            writeable = true;
            bufferStr = String.Empty;
        }
    }
     
    public class EncodeDecode
    {
        // encoder takes an object and returns a string to the retailer
        // create a CSV string
        public static BufferString Encoder( OrderObject myObj)
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
        public static OrderObject Decoder( BufferString myStr)
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
        public BufferString[] buffers;
        private const int N = 5;
        private int n; // number of cells
        private int elementCount;
        private static Semaphore write_pool;
        private static Semaphore read_pool;

        // constructor for class
        public MultiCellBuffer(int n)
        {
            lock (this) // we want no interruptiongs
            {
                elementCount = 0;
                
                if (n <= N)
                {
                    this.n = n;
                    write_pool = new Semaphore(n, n);
                    read_pool = new Semaphore(n, n);
                    buffers = new BufferString[3];
                    

                    for (int i = 0; i < n; i++)
                    {
                        buffers[i] = new BufferString();
                 
                    }
                }
                else
                    Console.WriteLine(" MultiCellBuffer Constructor - n > N ..");
            }
        }
         
        public void setOneCell(BufferString data)
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
                    if (buffers[i].isWriteable()) // make sure empty
                    {
                        buffers[i] = data;
                        elementCount++;
                        i = n;
                    }
                }
                write_pool.Release();
                Monitor.Pulse(this);
            }
        }

        public BufferString getOneCell()
        {
            BufferString outStr = null;
            read_pool.WaitOne();

            lock (this)
            {
                while (elementCount == 0)
                {
                    Monitor.Wait(this);
                }
                
                for (int i = 0; i < n; i++)
                {

                    if (!buffers[i].isWriteable()) // make sure cell has data
                    {
                        outStr = new BufferString();
                        outStr.setBufferString(buffers[i].getBufferString());
                        buffers[i].Reset();
                        elementCount--;
                        i = n;
                    }
                               
                }
                read_pool.Release();
                Monitor.Pulse(this);
            }
            return outStr;
        }

    }

    public class Orderprocessing
    {
        public static event orderProcessedEvent orderProcessed; // event triggered whe a new order has beeen processed
        public static void processOrder( OrderObject orderObj, Int32 unitPrice )
        {
            long cardNo = orderObj.getCardNo();
            if ( (cardNo <= 9999 && cardNo >= 9000))
            {
                Int32 paymentAmount = Convert.ToInt32(1.10 * (unitPrice * orderObj.getAmt()));  // price * quanity + tax(10%)
                orderProcessed(orderObj.getID(), paymentAmount, unitPrice, orderObj.getAmt());
            }
            else
            {
                Console.WriteLine("INVALID CARD.....  {0}", cardNo);
            }
        }
    }


    public class ChickenFarm
    {
        static Random rnd = new Random();
        public static event priceCutEvent PriceCut;

        private static Int32 priceCutCount = 0;
        private static Int32 loc = 0;
        private static Int32 chickenPrice = 35;
        private static Int32 priceUpdateCounter = 0; // when this gets to 10, then up the price and reset

        public Int32 getPrice() { return chickenPrice; }
        
        public static void changePrice(Int32 price)

        {
            //Int32 Len =  myApplication.retailers.Length;
            //if (loc == myApplication.retailers.Length)
            //        loc = 0;

            if (PriceCut != null)
            {
                if (price < chickenPrice)
                {
                    // price cut
                    if (PriceCut != null)
                        if (price < chickenPrice)
                        {
                            PriceCut(price);
                            loc++;
                            priceCutCount++;
                        }
                    if (price != chickenPrice)
                        chickenPrice = price;

                }
            }
            
        }
        public void farmerFunc()
        {
            while (priceCutCount < 10)
            {
                Thread.Sleep(rnd.Next(1000, 2000));
                Int32 price = pricingModel();
                changePrice(price);
            }
            myApplication.chickenThreadRunning = false;
        }

        private  Int32 pricingModel()
        {
            Int32 price = 0;
            priceUpdateCounter++;

            if (priceUpdateCounter >= 10)
            {
                price = 40;// max the price
                priceUpdateCounter = 0;
            }
            else
            {
                price = rnd.Next(10, 60);
            }
            return price;
        }
       
        public void runOrder() // event handler
        {
            BufferString order = myApplication.mcb.getOneCell();
            OrderObject orderObj = EncodeDecode.Decoder(order);
            Thread thread = new Thread(() => Orderprocessing.processOrder(orderObj, getPrice()));
            thread.Start();
        }
    }


    public class Retailer
    {
    
        public static event orderCreatedEvent orderCreated;
        public static Random rnd = new Random();


        public void retailerFunc()
        {
         
            while (myApplication.chickenThreadRunning)
            {
                Thread.Sleep(rnd.Next(1500, 3000));
                createorder(Int32.Parse(Thread.CurrentThread.Name));
            }
        }

        private void createorder(Int32 senderID)
        {
            long cardNo = rnd.Next(9000, 9999);
            Int32 amount = rnd.Next(10, 1000);

            OrderObject orderObj = new OrderObject(senderID, cardNo, amount);
            BufferString orderString = new BufferString();
            orderString = EncodeDecode.Encoder(orderObj);

            Console.WriteLine(" Store {0}'s order was created at {1}.", senderID.ToString(), DateTime.Now.ToString("hh:mm:ss"));

            myApplication.mcb.setOneCell(orderString);
            orderCreated();
           
        }

        public void orderProcessed(Int32 senderID, Int32 amountToCharge, Int32 price, Int32 amount)
        {

            Console.WriteLine(" Store {0}'s order has been processed.  amount to be chaged is ${1} = ${2} (price) * {3} (amount) of chickens",
                 senderID.ToString(),  amountToCharge, price, amount);
            Console.WriteLine("           time : {0}", DateTime.Now.ToString("hh:mm:ss"));

        }

          
        public void chickenOnSale( Int32 p)
        {
            //order chickens from farm.
            Console.WriteLine("Store{0} chickens are on sale: as low as ${1} each", Thread.CurrentThread.Name, p);
        }
    }
    public class myApplication
    {
        public static bool chickenThreadRunning = true;
        public static MultiCellBuffer mcb;
        public static Thread[] retailers;


        static void Main(string[] args)
        {
            ChickenFarm chicken = new ChickenFarm();
            Retailer chickenstore = new Retailer();

            mcb = new MultiCellBuffer(3);

            
            Thread farmer = new Thread(new ThreadStart(chicken.farmerFunc));
            farmer.Start();
           
            ChickenFarm.PriceCut += new priceCutEvent(chickenstore.chickenOnSale);
            Retailer.orderCreated += new orderCreatedEvent(chicken.runOrder);
            Orderprocessing.orderProcessed += new orderProcessedEvent(chickenstore.orderProcessed);

            Thread[] retailers = new Thread[5];
            for (Int32 i = 0; i < 5; i++)
            {
                retailers[i] = new Thread(new ThreadStart(chickenstore.retailerFunc));
                retailers[i].Name = (i + 1).ToString();
                retailers[i].Start();
            }
        }
    }
}
