using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace youku
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                txtContent.Text = "";
                var response = http(textBox1.Text);
                var stream = response.GetResponseStream();

                var vl = new List<video>();
                using (var sr = new StreamReader(stream))
                {
                    //var content = sr.ReadToEnd();
                    //txtContent.Text = content;
                    var n = 1;
                    while (sr.Peek() >= 0)
                    {
                        var line = sr.ReadLine();
                        if (line.StartsWith("#EXTINF"))
                        {
                            var timeStr = line.Split(':')[1].Split(',')[0];
                            var url = sr.ReadLine();
                            txtContent.Text += n.ToString() + "                                           " + timeStr + "\r\n";
                            txtContent.Text += url + "\r\n\r\n";
                            var v = new video
                            {
                                index = n,
                                time = Convert.ToDecimal(timeStr),
                                url = url
                            };
                            vl.Add(v);
                            n++;
                        }
                    }
                }
                txtContent.Text = "共【" + vl.Count.ToString() + "】个视频文件\r\n" + txtContent.Text;
                // 文件下载地址
                var dir = new FolderBrowserDialog();
                if (dir.ShowDialog() != DialogResult.OK) return;
                string path = dir.SelectedPath;// "E:/文件下载";
                                               // 如果不存在就创建file文件夹
                if (!Directory.Exists(path))
                {
                    if (path != null) Directory.CreateDirectory(path);
                }
                foreach (var v in vl)
                {
                    var thread = new Thread(new ParameterizedThreadStart(download));
                    thread.Start(new dt
                    {
                        url = v.url,
                        fileName = path + "\\" + v.index + ".flv.ts"
                    });
                }

                MessageBox.Show("Ok");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        class dt
        {
            public string url { get; set; }
            public string fileName { get; set; }
        }
        private void download(object state)
        {
            try
            {
                var obj = state as dt;
                var responseAvi = http(obj.url);
                using (Stream streamAvi = responseAvi.GetResponseStream())
                {
                    using (var sos = new System.IO.FileStream(obj.fileName, System.IO.FileMode.Create))
                    {
                        byte[] img = new byte[1024];
                        int total = streamAvi.Read(img, 0, img.Length);
                        while (total > 0)
                        {
                            //之后再输出内容
                            sos.Write(img, 0, total);
                            total = streamAvi.Read(img, 0, img.Length);
                        }
                    }
                }
                Thread.Sleep(Convert.ToInt32(100));
            }
            catch
            {

            }
        }
        private System.Net.HttpWebResponse http(string url)
        {
            //请求网络路径地址
            var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
            request.Timeout = 5000; // 超时时间
                                    //获得请求结果
            var response = (System.Net.HttpWebResponse)request.GetResponse();
            return response;
        }
        class video
        {
            public int index { get; set; }
            public decimal time { get; set; }
            public string url { get; set; }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                var responseAvi = http(textBox1.Text);
                using (Stream streamAvi = responseAvi.GetResponseStream())
                {
                    var dialog = new SaveFileDialog();
                    dialog.Filter = "*.flv.ts|*.flv.ts";
                    if (dialog.ShowDialog() != DialogResult.OK) return;
                    var path = dialog.FileName;
                    using (var sos = new System.IO.FileStream(path, System.IO.FileMode.Create))
                    {
                        byte[] img = new byte[1024];
                        int total = streamAvi.Read(img, 0, img.Length);
                        while (total > 0)
                        {
                            //之后再输出内容
                            sos.Write(img, 0, total);
                            total = streamAvi.Read(img, 0, img.Length);
                        }
                    }
                }
                MessageBox.Show("Ok");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                var dir = new FolderBrowserDialog();
                if (dir.ShowDialog() != DialogResult.OK) return;

                var fileNames = new StringBuilder();
                foreach (string fileName in Directory.GetFiles(dir.SelectedPath))
                {
                    fileNames.Append(fileName + "?");
                }
                fileNames.Remove(checked(fileNames.Length - 1), 1);
                Thread thread = new Thread(mergeVideo);
                thread.Start(fileNames.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void mergeVideo(object obj)
        {
            mergeVideo((string)obj);
        }

        private void mergeVideo(string fileNames)
        {
            string[] names = fileNames.Split('?');
            FileInfo fileInfo = new FileInfo(names[0]);
            string outName = fileInfo.DirectoryName + "\\merge_" + fileInfo.Name;
            outName = "\"" + outName + "\"";
            string fileStr = "";
            string[] array = names;
            foreach (string name in array)
            {
                fileStr = fileStr + "file '" + name + "'\r\n";
            }
            File.WriteAllText("list.txt", fileStr);
            string arguments = "-f concat -i list.txt -c copy " + outName;
            runProcess("ffmpeg.exe", arguments);
        }

        private void runProcess(string processName, string arguments)
        {
            var p = new Process();
            p.StartInfo.FileName = processName;
            p.StartInfo.Arguments = arguments;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.ErrorDataReceived += ErrorDataReceived;
            p.OutputDataReceived += OutputDataReceived;
            p.StartInfo.UseShellExecute = false;
            p.Start();
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            p.BeginErrorReadLine();
            p.WaitForExit();
            p.Close();
            p.Dispose();
        }

        private void OutputDataReceived(object obj, DataReceivedEventArgs e)
        {
            //_message.AppendLine(e.Data);
            //if (textBoxMessage.InvokeRequired)
            //{
            //    textBoxMessage.Invoke(new EventHandler(showMessage));
            //}
        }

        private void ErrorDataReceived(object obj, DataReceivedEventArgs e)
        {
            //_message.AppendLine(e.Data);
            //if (textBoxMessage.InvokeRequired)
            //{
            //    textBoxMessage.Invoke(new EventHandler(showMessage));
            //}
        }
    }
}
