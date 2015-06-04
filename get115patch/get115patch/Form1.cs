using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace get115patch
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Redown_Click(object sender, EventArgs e)
        {
            string[] ri = getRangeInfo(textBox1.Text.Split(new char[]{'\n'})[2]);
            UInt64 startb = Convert.ToUInt64(ri[1]);
            UInt64 endb = Convert.ToUInt64(ri[2]);
            string[] infos = textBox1.Text.Split(new char[] { '\n' });
            UInt64 blksize = 512 * 1024;
            for(UInt64 n=0; n<((endb-startb)/blksize + (UInt64)(((endb-startb)%blksize==0)?0:1)); n++)
            {
                UInt64 nsb = startb + n * blksize;
                UInt64 neb = Math.Min((startb + (n + 1) * blksize), endb);
                string tfn = "f:\\" + nsb.ToString() + ".dat";
                if (File.Exists(tfn)) continue;
                infos[2] = "Range: bytes=" + nsb.ToString() + "-" + neb.ToString();
                string req = "";
                foreach (string rs in infos) req += rs.Trim() + "\r\n";
                downloadfile(req + "\r\n");
            }
        }

        //这是一个测试，如果正确应该可以提交了。
		//Imodify by Notepad++
        public void downloadfile(string req)
        {
            byte[] tbuf = new byte[512*1024*8];

            try
            {
                Socket tsocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                tsocket.ReceiveTimeout = 1000 * 30;
                tsocket.SendTimeout = 1000 * 30;
                Uri tloction = new Uri("http://" + req.Split(new char[]{'\n'})[1].Split(new char[]{':'})[1].Trim());
                tsocket.Connect(tloction.Host, 80);
                if (tsocket.Connected)
                {

                    tsocket.Send(ASCIIEncoding.ASCII.GetBytes(req));

                    //bool needrsend = false;
                    byte[] rbuf = new byte[0x80000];
                    int revc = 0, recvcd = 0;
                    do
                    {
                        try
                        {
                            revc = tsocket.Receive(rbuf);
                            Buffer.BlockCopy(rbuf, 0, tbuf, recvcd, revc);
                        }
                        catch (Exception e)
                        {
                            //log(string.Format("In {0} sectionNo: {2} \tDownLoadFileByts.tsocket.Receive(rbuf) catch exception {1}\r\n {3}", ts.filename, e.ToString(), sectionNo, req));
                            //needrsend = true;
                            break;
                        }
                        recvcd += revc;
                    } while (revc > 0);

                    tsocket.Close();

                    if (DealRecvBuf(req, tbuf, recvcd))   //recv success
                    {
                        //System.Threading.Monitor.Enter(ts.sectionflag);
                        //System.Collections.BitArray ba = new System.Collections.BitArray(ts.sectionflag);
                        //ba[sectionNo] = true;
                        //for (int n1 = 0; n1 < ba.Length; n1++)
                        //    if (ba[n1]) ts.sectionflag[n1 / 8] |= (byte)(1 << (n1 % 8));
                        //System.Threading.Monitor.Exit(ts.sectionflag);

                        //ReportTaskStatus(ts);
                    }
                }
                else tsocket.Close();
            }
            catch (Exception e)
            {
                //ts.errorcount++;
                //log(string.Format("In {0} sectionNo: {2} \tDownLoadFileByts catch exception {1}", ts.filename, e.ToString(), sectionNo));
                //ReportTaskStatus(ts);
            }

        }

        public bool DealRecvBuf(string req, byte[] rbuf, long recvbytes)
        {
            int headlen = 0;
            for (int n = 0; n < recvbytes; n++)
            {
                if (rbuf[n] == 0x0d && rbuf[n + 1] == 0x0a && rbuf[n + 2] == 0x0d && rbuf[n + 3] == 0x0a)
                {
                    headlen = n + 4;
                    break;
                }
            }

            if (headlen > 0)
            {
                System.IO.StreamReader sr = new StreamReader(new MemoryStream(rbuf, 0, headlen));
                string heads = sr.ReadToEnd();
                long tcl = 0, tsb = 0;
                GetBytesFromHeadInfo(heads, ref tcl, ref tsb);
                if (tcl > 0 && (headlen + tcl) == recvbytes)
                    return saveBytesToFile(req, rbuf, headlen, tsb, tcl);
                else
                {
                    //ts.errorcount++;
                    //log(string.Format("In {0} DealRecvBuf RecvErrorInfo {1}", ts.filename, heads));
                }
            }

            return false;
        }

        public bool saveBytesToFile(string req, byte[] tfb, long offset, long tsb, long tcl)
        {
            string tfn = "f:\\" + tsb.ToString() + ".dat";
            FileStream fs = File.Open(tfn, FileMode.CreateNew);
            fs.Write(tfb, (int)offset, (int)tcl);
            fs.Close();

            return true;
        }

        public static void GetBytesFromHeadInfo(string heads, ref long tcl, ref long tsb)
        {
            //string theads = "HTTP/1.1 206 Partial Content\r\nServer: nginx\r\nDate: Tue, 20 Jan 2015 03:56:29 GMT\r\nContent-Type: application/octet-stream\r\nContent-Length: 1048560\r\nConnection: keep-alive\r\nLast-Modified: Sun, 06 Oct 2013 07:29:02 GMT\r\nETag: \"5251113e-18c1de6\"\r\nContent-Disposition: attachment\r\nContent-Range: bytes 1-1048560/25959910\r\n\r\n";

            //HTTP/1.1 206 Partial Content\r\n
            //Server: nginx\r\n
            //Date: Tue, 20 Jan 2015 03:56:29 GMT\r\n
            //Content-Type: application/octet-stream\r\n
            //Content-Length: 1048560\r\n
            //Connection: keep-alive\r\n
            //Last-Modified: Sun, 06 Oct 2013 07:29:02 GMT\r\n
            //ETag: \"5251113e-18c1de6\"\r\n
            //Content-Disposition: attachment\r\n
            //Content-Range: bytes 1-1048560/25959910\r\n\r\n

            StringReader sr = new StringReader(heads);
            while (true)
            {
                string tl = sr.ReadLine();
                if (tl == null) break;
                if (tl.IndexOf("Content-Length:") >= 0)
                    tcl = System.Convert.ToInt64(tl.Split(new char[] { ':' })[1]);
                if (tl.IndexOf("Content-Range: bytes") >= 0)
                {
                    string[] tsa = tl.Split(new char[] { ' ', '-', '/' });
                    tsb = System.Convert.ToInt64(tsa[3]);
                }
            }
        }

        public string[] getRangeInfo(string infos)
        {
            return infos.Split(new char[]{'=', '-', '\r'});
        }

        private void MDFile_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.OpenFileDialog ofdlg = new OpenFileDialog();
            if(ofdlg.ShowDialog() == DialogResult.OK)
            {
                string destfilename = ofdlg.FileName;
                foreach(string sfn in Directory.EnumerateFiles("f:\\", "*.dat"))
                {

                    FileStream sfs = File.OpenRead(sfn);
                    byte[] tbuf = new byte[sfs.Length];
                    if (sfs.Read(tbuf, 0, tbuf.Length) != tbuf.Length) return;
                    sfs.Close();

                    long tfof = System.Convert.ToInt64(sfn.Split(new char[] { '\\', '.' })[1]);
                    FileStream fs = File.OpenWrite(destfilename);
                    fs.Seek(tfof, SeekOrigin.Begin);
                    fs.Write(tbuf, 0, tbuf.Length);
                    fs.Close();
                }

                File.Move(destfilename, destfilename + ".good");
            }
        }
    }
}
