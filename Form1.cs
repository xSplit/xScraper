using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace XScraper
{
    public partial class Form1 : Form
    {
        Timer t;
        public Form1()
        {
            ServicePointManager.DefaultConnectionLimit = 10000;
            InitializeComponent();
            if (File.Exists("urls.txt")) listBox1.Items.AddRange(File.ReadAllLines("urls.txt"));
            this.FormClosing += (o, e) =>{
                File.WriteAllLines("urls.txt", listBox1.Items.Cast<string>());
                Environment.Exit(0);
            };
            t = new Timer();
            t.Tick += (o, e) => button1_Click(null, null);
            var z = new Timer();
            z.Tick += (o,e) => { if(todo < 1){ todo--; button2_Click(null,null);}};
            z.Interval = 1000;
            z.Start();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            var url = Microsoft.VisualBasic.Interaction.InputBox("Insert XML url for products scraping\nRemember to add http://", "Add Url", "http://website.com/data.xml");
            listBox1.Items.Add(url);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            listBox1.Items.RemoveAt(listBox1.SelectedIndex);
        }

        int todo = 1;
        private void button1_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
            button1.Enabled = false;
            button1.Text = "WORKING";
            button2.Enabled = true;
            var keys = textBox1.Text.Split(',');
            Parallel.For(0, todo = listBox1.Items.Count, new ParallelOptions() { MaxDegreeOfParallelism = 5 }, (int index) =>
            {
                bool completed = false;
                new System.Threading.Thread(() =>
                {
                    if (button2.Enabled)
                    {
                        try
                        {
                            var xml = new XmlDocument();
                            var web = new WebClient();
                            web.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/55.0.2883.87 Safari/537.36";
                            xml.LoadXml(web.DownloadString((string)listBox1.Items[index]));
                            foreach (XmlNode product in xml.GetElementsByTagName("url"))
                            {
                                if (!button2.Enabled) return;
                                string link = product.FirstChild.InnerText;
                                DateTime date; DateTime.TryParse(product.ChildNodes[1].InnerText, out date);
                                string key = "";
                                if ((date.Day >=  dateTimePicker1.Value.Day && date.Month >= dateTimePicker1.Value.Month && date.Year >= dateTimePicker1.Value.Year)
                                    && (textBox1.Text.Length < 1 || (key = MatchKeys(keys, xml.GetElementsByTagName("image:title"), link)).Length > 0))
                                {
                                    this.Invoke((MethodInvoker)delegate()
                                    {
                                        listView1.Items.Add(new ListViewItem(new[] { link, date.Day.ToString() + '-' + date.Month.ToString() + '-' + date.Year.ToString(), key }));
                                        listView1.Items[listView1.Items.Count - 1].EnsureVisible();
                                        Application.DoEvents();
                                    });
                                }
                            }
                        }
                        catch { }
                        todo--;
                        completed = true;
                    }
                }).Start();
                while (!completed) Application.DoEvents();
            });
        }

        string MatchKeys(string[] keys, XmlNodeList image, string link)
        {
            link = link.ToLower();
            if (image.Count > 0)
            {
                var title = image[0].InnerText.ToLower();
                foreach (var key in keys)
                    if (title.Contains(key.ToLower()))
                        return key;
            }
            foreach (var key in keys)
                if (link.Contains(key.ToLower()))
                    return key;
            return "";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            button1.Enabled = true;
            button1.Text = "START";
            button2.Enabled = false;
        }

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Process.Start(listView1.GetItemAt(e.X, e.Y).Text);
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            try
            {
                t.Stop();
                t.Interval = (int.Parse(textBox2.Text) * 60) * 1000;
                t.Start();
            }
            catch { }
        }
    }
}
