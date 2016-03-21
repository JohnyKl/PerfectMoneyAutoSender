using System;
using System.Collections;
using System.IO;
using System.Xml;
using System.Reflection;
using Excel = Microsoft.Office.Interop.Excel;

namespace PerfectMoneyAutoSender
{
    class XMLDataReader
    {
        public static ArrayList paymentRequisites = new ArrayList();

        public static void CreateDocument(ArrayList values)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<item></item>");

            for(int i = 0; i < 3; i++)
            {
                AddNewElement(doc, "account", "U11336184");
                AddNewElement(doc, "money", "0.01");
                AddNewElement(doc, "comment", "test sending " + i.ToString());
            }
            
            doc.PreserveWhitespace = true;
            doc.Save("data.xml");

        }

        private static void AddNewElement(XmlDocument doc, string name, string value)
        {
            XmlElement newElem = doc.CreateElement(name);
            newElem.InnerText = value;
            doc.DocumentElement.AppendChild(newElem);
        }

        public static void ReadExcelDocument()
        {
            Excel.Application xlApp;
            Excel.Workbook xlWorkBook;
            Excel.Worksheet xlWorkSheet;
            Excel.Range range;

            string str;

            string pathToApp = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase);
            pathToApp = pathToApp.Replace("file:\\", "");

            xlApp = new Excel.Application();
            xlWorkBook = xlApp.Workbooks.Open(pathToApp + "\\payments.xlsx"); //Open(@"payments.xls", 0, true, 5, "", "", true, Excel.XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0);
            xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(1);

            range = xlWorkSheet.UsedRange;

            for (int rCnt = 2; rCnt <= range.Rows.Count; rCnt++)
            {
                string account = (string)(range.Cells[rCnt, 1] as Excel.Range).Value2;
                string money = (string)(range.Cells[rCnt, 2] as Excel.Range).Value2;
                string comment = (string)(range.Cells[rCnt, 3] as Excel.Range).Value2;

                paymentRequisites.Add(new string[] { account, money, comment });
            }

            xlWorkBook.Close(true, null, null);
            xlApp.Quit();

            releaseObject(xlWorkSheet);
            releaseObject(xlWorkBook);
            releaseObject(xlApp);
        }

        private static void releaseObject(object obj)
        {
            try
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);
                obj = null;
            }
            catch (Exception ex)
            {
                obj = null;
            }
            finally
            {
                GC.Collect();
            }
        }

        public static void ReadDocument()
        {
            paymentRequisites.Clear();

            XmlDocument doc = new XmlDocument();
            doc.Load("data.xml");
            
            using (XmlReader reader = XmlReader.Create(new StringReader(doc.InnerXml)))
            {
                reader.ReadToFollowing("account");

                for( int i = 0; i < doc.DocumentElement.ChildNodes.Count / 3; i++)
                {                    
                    string account = reader.ReadElementContentAsString();
                    string money = reader.ReadElementContentAsString();
                    string comment = reader.ReadElementContentAsString();

                    paymentRequisites.Add(new string[] { account, money, comment } );

                    //Console.WriteLine("Account " + i.ToString() + " - " + account + ", " + money + "USD, " + comment);
                }                
            }
        }
    }
}
