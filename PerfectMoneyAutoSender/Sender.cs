using System;
using System.Reflection;
using OpenQA.Selenium;
using OpenQA.Selenium.PhantomJS;
//using OpenQA.Selenium. Chrome;
using OpenQA.Selenium.Support.UI;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Collections;

namespace PerfectMoneyAutoSender
{
    class Sender
    {
        private string accountName = "";
        private string userID = "";

        public bool send = false;
        public ProgressBar progressBar;
        public Button startStop;
        
        public Sender()
        {
            //var chromeDriverService = ChromeDriverService.CreateDefaultService();
            //chromeDriverService.HideCommandPromptWindow = true;

            //driver = new ChromeDriver(chromeDriverService, new ChromeOptions());  
            var phantomJsService = PhantomJSDriverService.CreateDefaultService();
            phantomJsService.HideCommandPromptWindow = true;

            driver = new PhantomJSDriver(phantomJsService, new PhantomJSOptions());           
        }
        
        public void Start(PictureBox pb)
        {
            WebDriverWait wait = new WebDriverWait(driver, DefaultTimeout);

            driver.Navigate().GoToUrl(loginPageUrl);

            IWebElement imgCaptcha = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("cpt_img")));

            SetCaptchaImage(pb, imgCaptcha);
            
            Console.WriteLine("start");
        }

        public void Stop()
        {
            Console.WriteLine("stop");

            driver.Quit();
        }

        public void Sending()
        {
            progressBar.Invoke(new Action(() => progressBar.Maximum = XMLDataReader.paymentRequisites.Count));
            progressBar.Invoke(new Action(() => progressBar.Minimum = 0));
            progressBar.Invoke(new Action(() => progressBar.Value = 0));
            //progressBar.Maximum = XMLDataReader.paymentRequisites.Count;
            //progressBar.Minimum = 0;
            //progressBar.Value = 0;

            for (int i = 0; i < XMLDataReader.paymentRequisites.Count && send; i++)
            {
                Console.WriteLine("sending - {0}", i);

                string[] payment = (string[])XMLDataReader.paymentRequisites[i];
                            
                var collection = driver.FindElementsByTagName("input");

                ArrayList elementsList = new ArrayList(3);

                int parametersCounter = 0;

                foreach(var element in collection)
                {
                    if(parametersCounter < 2 && element.Size.Height == 20)
                    {
                        elementsList.Add(element);

                        parametersCounter++;
                    }
                }

                elementsList.Add(driver.FindElementByTagName("textarea"));

                ((IWebElement)elementsList[0]).SendKeys(payment[1]);
                ((IWebElement)elementsList[1]).SendKeys(payment[0]);
                ((IWebElement)elementsList[2]).SendKeys(payment[2]);

                driver.FindElementById("sbt").Click();

                WebDriverWait wait = new WebDriverWait(driver, DefaultTimeout);

                wait.Until(ExpectedConditions.UrlMatches(sendPreviewPageUrl));
                                
                driver.FindElementById("submit").Click();

                bool result = wait.Until(ExpectedConditions.UrlMatches(resultPageUrl));

                if(result)
                {
                    using (MemoryStream streamFullScreenshot = new MemoryStream((driver as ITakesScreenshot).GetScreenshot().AsByteArray))
                    {
                        Bitmap bitmap = new Bitmap(streamFullScreenshot);

                        var table = GetResultTable();

                        if(table != null)
                        {                        
                            Rectangle rect = new Rectangle(table.Location, table.Size);

                            bitmap = bitmap.Clone(rect, bitmap.PixelFormat);                        
                        }

                        string path = "snapshot/" + userID + "(" + DateTime.Now.ToString("s") + ").png";
                        path = path.Replace(":", "-");
                        path = path.Replace(" ", "-");

                        bitmap.Save(path);

                        Console.WriteLine("snapshot saved - {0}", path);

                        progressBar.Invoke(new Action(() => progressBar.Value = i + 1));
                        //progressBar.Invoke(new Action(() => progressBar.Refresh()));
                        //progressBar.Value = i + 1;

                        //progressBar.Refresh();

                        WriteInLog(accountName, payment[0], payment[1], payment[2]);
                    }
                }                

                driver.Navigate().GoToUrl(sendMoneyPageUrl + accountName);
            }

            startStop.Invoke(new Action(() => startStop.Text = "Start"));
        }

        private IWebElement GetResultTable()
        {
            var collections = driver.FindElementsByTagName("table");

            foreach (var element in collections)
            {
                if(element.Text.Contains("Завершен\r\n") || element.Text.Contains("Complete\r\n"))
                {
                    return element;
                }
            }

            return null;
        }
        
        public bool LoginUser (string login, string password, string captcha, string accountName)
        {
            this.accountName = accountName;
            this.userID = login;

            driver.FindElementByName("login").SendKeys(login);
            driver.FindElementByName("password").SendKeys(password);
            driver.FindElementByName("turing").SendKeys(captcha);

            driver.FindElementById("sbt").Click();

            bool result = driver.Url.Equals(profilePageUrl);

            if (result)
            {
                driver.Navigate().GoToUrl(sendMoneyPageUrl + accountName);
            }

            Console.WriteLine("login user - {0}", result);

            return result;
        }

        private void SetCaptchaImage(PictureBox pb, IWebElement imgCaptcha)
        {
            if (imgCaptcha != null)
            {
                Image captcha = GetCaptcha(imgCaptcha);

                if (captcha != null)
                {
                    pb.Image = captcha;
                }
            }
        }

        private void WriteInLog(string accountName, string destAccount, string amount, string comment)
        {
            string path = @"snapshot/log.txt";
            string text = accountName + " -> " + destAccount + " - " + amount + " : " + comment;

            // This text is added only once to the file.
            if (!File.Exists(path))
            {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.WriteLine(text);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(path))
                {
                    sw.WriteLine(text);
                }
            }
        }

        private Image GetCaptcha(IWebElement imgCaptcha)
        {
            try
            {
                using (MemoryStream streamFullScreenshot = new MemoryStream((driver as ITakesScreenshot).GetScreenshot().AsByteArray))
                {
                    Bitmap bitmap = new Bitmap(streamFullScreenshot);
                    Rectangle rect = new Rectangle(new Point(imgCaptcha.Location.X + 15, imgCaptcha.Location.Y - 15), imgCaptcha.Size);

                    return bitmap.Clone(rect, bitmap.PixelFormat);                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetCaptcha() failed: {0}", ex);
            }

            return null;
        }
        private PhantomJSDriver driver;
        //private ChromeDriver driver;

        private readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(2.0);
        private readonly TimeSpan DefaultSleep = TimeSpan.FromSeconds(3.0);

        private const string loginPageUrl = @"https://perfectmoney.is/login.html";
        private const string profilePageUrl = @"https://perfectmoney.is/profile.html";
        private const string sendMoneyPageUrl = @"https://perfectmoney.is/send_money.html?f=";
        private const string sendPreviewPageUrl = @"https://perfectmoney.is/send_preview.html";
        private const string resultPageUrl = @"https://perfectmoney.is/result.html";
    }
}
