using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Parsing;
using Directory = Pri.LongPath.Directory;
using File = Pri.LongPath.File;

namespace MakeExamsEasyAgain
{
    public partial class Form1 : Form
    {
        string[] categories = {
            "Handouts",
            "Useful Proofs"
        };
        string[] subjectNames = {
            "Stars",
            "Dynamics",
            "Quantum",
            "Relativity",
            "Topics",
            "Cosmology",
            "Stat Phys",
            "Astrofluids"
        };
        List<DataFile> files = new List<DataFile>();
        List<DataFile> openFiles = new List<DataFile>();
        public DataFile currentFile;
        public TreeNode currentFileNode;
        bool dontUpdateTree = false;
        public static string path;
        public Form1()
        {
            path = Directory.GetCurrentDirectory() + @"\Documents\";
            InitializeComponent();
            InitNodes();
            LoadTreeView();
            tabControl1.Resize += ResizeTab;
            tabControl1.SelectedIndexChanged += UpdateSelected;

        }
        private void InitNodes()
        {
            foreach (string cat in categories)
            {
                foreach (string sub in subjectNames)
                {
                    DoNodesForFolder(cat, sub);
                }
            }
            string textbookPath = path + "Textbooks\\";
            string[] dirs = Directory.GetDirectories(textbookPath);
            foreach (string s in dirs)
            {
                string sub = s.Substring(textbookPath.Length);
                DoNodesForFolder("Textbooks", sub);
            }
            files.Sort();
            //webBrowser1.
        }
        private void DoNodesForFolder(string cat, string sub)
        {
            string currPath = path + cat + "\\" + sub + "\\";
            string[] fl = Directory.GetFiles(currPath, "*.*", SearchOption.AllDirectories)
                .Where(s => s.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)).ToArray();
            foreach (string f in fl)
            {
                string nm = f.Substring(path.Length + 2 + cat.Length + sub.Length);
                nm = nm.Substring(0, nm.Length - 4);
                string nameForFile = f.Substring(0, f.Length - 4);
                string txtFile = nameForFile + ".txt";
                string fullTextFile = nameForFile + " Full Text.txt";
                string[] contents;
                if (File.Exists(txtFile))
                {
                    contents = File.ReadLines(txtFile).ToArray();
                }
                else
                {
                    File.WriteAllText(txtFile, nm);
                    contents = new string[1];
                    contents[0] = nm;

                }
                string ft = "";
                if (File.Exists(fullTextFile))
                {
                    string[] ftLines = File.ReadLines(fullTextFile).ToArray();
                    foreach (string s in ftLines) ft += s + " ";
                    ft = ft.Trim();
                }
                List<string> goodTags = new List<string>();
                for (int i = 1; i < contents.Length; i++)
                {
                    if (contents[i] != "") goodTags.Add(contents[i]);
                }
                files.Add(new DataFile(contents[0], goodTags.ToArray(), cat, sub, nm, ft));
            }

        }
        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            TreeNode selNode = treeView1.SelectedNode;
            try
            {
                string name = selNode.Text;
                string subj = selNode.Parent.Text;
                string cat = selNode.Parent.Parent.Text;
                DataFile dat = GetDataFile(name, subj, cat);
                LoadPage(cat, subj, dat.fileName, name);
                currentFileNode = treeView1.SelectedNode;
                currentFile = GetDataFile(name, subj, cat);
                textBox2.Text = name;
                string[] tags = dat.tags;
                string t = "";
                foreach (string s in tags) t += s + " ";
                t = t.Trim();
                richTextBox1.Text = t;
            }
            catch { }
        }
        public DataFile GetDataFile(string n, string s, string t)
        {
            DataFile checker = new DataFile(n, t, s);
            foreach (DataFile d in files) if (d.Equals(checker)) return d;
            throw new Exception("this is bad");
        }
        public DataFile GetDataFile(DataFile dat)
        {
            foreach (DataFile d in files) if (d.Equals(dat)) return d;
            throw new Exception("this is bad");
        }
        public int GetIndexOfDataFile(string n, string s, string t)
        {
            DataFile checker = new DataFile(n, t, s);
            for (int i = 0; i < files.Count; i++) if (files[i].Equals(checker)) return i;
            throw new Exception("this is bad");
        }
        public int GetIndexOfDataFile(DataFile d)
        {
            for (int i = 0; i < files.Count; i++) if (files[i].Equals(d)) return i;
            throw new Exception("this is bad");
        }
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            LoadTreeView();
            if (textBox1.Text != "")
                treeView1.ExpandAll();
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            //put optional search option in here
        }
        private void LoadTreeView()
        {
            //files.Sort();
            treeView1.Nodes.Clear();
            string[] tags;
            if (checkBox2.Checked)
            {
                if (textBox1.Text != "")
                    tags = textBox1.Text.Split(' ');
                else tags = new string[0];
                for (int i = 0; i < tags.Length; i++) tags[i] = tags[i].Trim();
                foreach (DataFile f in files)
                {
                    if (f.MatchTags(tags, checkBox1.Checked))
                    {
                        string subj = f.subject;
                        string type = f.type;
                        int ind = CheckNodeChildrenForString(type);
                        if (ind == -1)
                        {
                            treeView1.Nodes.Add(type);
                            ind = CheckNodeChildrenForString(type);
                        }
                        TreeNode typeNode = treeView1.Nodes[ind];
                        int ind2 = CheckNodeChildrenForString(typeNode, subj);
                        if (ind2 == -1)
                        {
                            typeNode.Nodes.Add(subj);
                            ind2 = CheckNodeChildrenForString(typeNode, subj);
                        }
                        TreeNode subjNode = typeNode.Nodes[ind2];
                        subjNode.Nodes.Add(f.name);
                    }
                }
            }
            else
            {
                string tag = textBox1.Text.Trim();
                int numSpaces = 0;
                foreach (char c in tag) if (c == ' ') numSpaces++;
                int numIterations = (int)Math.Pow(2, numSpaces);
                string[] words = tag.Split(' ');
                List<DataFile> matchedFiles = new List<DataFile>();
                for (int i = 0; i < numIterations; i++)
                {
                    string trialWord = "";
                    for (int j = 0; j < numSpaces; j++)
                    {
                        trialWord += words[j];
                        trialWord += (i & (int)Math.Pow(2, j)) != 0 ? " " : "";
                    }
                    trialWord += words[words.Length - 1];
                    tags = new string[1];
                    tags[0] = trialWord;
                    foreach (DataFile f in files)
                    {
                        if (f.MatchTags(tags, checkBox1.Checked) && !matchedFiles.Contains(f))
                        {
                            matchedFiles.Add(f);
                            string subj = f.subject;
                            string type = f.type;
                            int ind = CheckNodeChildrenForString(type);
                            if (ind == -1)
                            {
                                treeView1.Nodes.Add(type);
                                ind = CheckNodeChildrenForString(type);
                            }
                            TreeNode typeNode = treeView1.Nodes[ind];
                            int ind2 = CheckNodeChildrenForString(typeNode, subj);
                            if (ind2 == -1)
                            {
                                typeNode.Nodes.Add(subj);
                                ind2 = CheckNodeChildrenForString(typeNode, subj);
                            }
                            TreeNode subjNode = typeNode.Nodes[ind2];
                            subjNode.Nodes.Add(f.name);
                        }
                    }
                }
            }
        }
        private void LoadPage(string type, string subj, string n, string tabName)
        {
            DataFile checkFile = new DataFile(tabName, type, subj);
            if (openFiles.Contains(checkFile))
            {
                int i = openFiles.IndexOf(checkFile);
                tabControl1.SelectedIndex = i;
            }
            else
            {
                TabPage page = new TabPage(tabName);
                WebBrowser browser = new WebBrowser
                {
                    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left,
                    Width = tabControl1.Width - 9,
                    WebBrowserShortcutsEnabled = true
                };
                browser.DocumentCompleted += DoSearch;
                browser.Navigate(path + type + "\\" + subj + "\\" + n + ".pdf");
                page.Controls.Add(browser);
                tabControl1.TabPages.Add(page);
                openFiles.Add(GetDataFile(checkFile));
                tabControl1.SelectedTab = page;

            }
        }
        private void DoSearch(object sender, EventArgs eventArgs)
        {
            if (checkBox3.Checked && textBox1.Text != "")
            {
                //WebBrowser browser = (WebBrowser)tabControl1.SelectedTab.Controls[0];
                //tabControl1.SelectedTab.mouse
                tabControl1.SelectedTab.Select();
                Thread t = new Thread(SearchText);
                t.Start();
            }
        }
        private void SearchText()
        {
            
            Thread.Sleep(1000);
            try
            {
                
                SendKeys.Send("^F");
                if (checkBox2.Checked)
                    SendKeys.Send(textBox1.Text.Split(' ')[0].ToUpper());
                else
                    SendKeys.Send(textBox1.Text.ToUpper());
                SendKeys.Send("{ENTER}");
            }
            catch { }
        }

        private void UpdateSelected(object sender, EventArgs eventArgs)
        {
            dontUpdateTree = true;
            try
            {
                DataFile dat = openFiles[tabControl1.SelectedIndex];
                //currentFileNode = treeView1.SelectedNode;
                currentFile = dat;
                textBox2.Text = dat.name;
                string[] tags = dat.tags;
                string t = "";
                foreach (string s in tags) t += s + " ";
                t = t.Trim();
                dontUpdateTree = true;
                richTextBox1.Text = t;
                //treeView1.SelectedNode = null;
                try
                {
                    tabControl1.SelectedTab.Controls[0].Width = tabControl1.Width - 9;
                }
                catch { }
            }
            catch
            {
                textBox2.Text = "";
                richTextBox1.Text = "";
                currentFile = null;
                //treeView1.SelectedNode = null;
            }

        }
        private void ResizeTab(object sender, EventArgs eventArgs)
        {
            try
            {
                tabControl1.SelectedTab.Controls[0].Width = tabControl1.Width - 9;
            }
            catch { }
        }
        private int CheckNodeChildrenForString(TreeNode node, string str)
        {
            for (int i = 0; i < node.Nodes.Count; i++)
            {
                if (node.Nodes[i].Text == str) return i;
            }
            return -1;
        }
        private int CheckNodeChildrenForString(string str)
        {
            for (int i = 0; i < treeView1.Nodes.Count; i++)
            {
                if (treeView1.Nodes[i].Text == str) return i;
            }
            return -1;
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (!dontUpdateTree)
                UpdateDataFile();
            else dontUpdateTree = false;
        }
        private void UpdateDataFile()
        {
            string path = currentFile.GetTXTPath();
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            string name = textBox2.Text;
            string[] tags = richTextBox1.Text.Split(' ');
            string[] writeData = new string[tags.Length + 1];
            writeData[0] = name;
            for (int i = 0; i < tags.Length; i++)
            {
                writeData[i + 1] = tags[i];
            }
            File.WriteAllLines(path, writeData);
            int indexOf = GetIndexOfDataFile(currentFile);
            currentFile.name = name;
            currentFile.tags = tags;
            files[indexOf] = currentFile;
            try
            {
                currentFileNode.Text = name;
            }
            catch { }
        }
        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            LoadTreeView();
            if (textBox1.Text != "")
                treeView1.ExpandAll();
        }

        private void checkBox2_CheckedChanged_1(object sender, EventArgs e)
        {
            LoadTreeView();
            if (textBox1.Text != "")
                treeView1.ExpandAll();
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            if (!dontUpdateTree)
                UpdateDataFile();
            else dontUpdateTree = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int ind = tabControl1.SelectedIndex;
            openFiles.RemoveAt(ind);
            try
            {

                tabControl1.SelectedIndex = ind - 1;
            }
            catch { }
            tabControl1.TabPages.RemoveAt(ind);
        }
    }
    public class DataFile : IComparable
    {
        public string name;
        public string fileName;
        public string[] tags;
        public string type;
        public string subject;
        public string fullText;
        public DataFile(string n, string[] t, string y, string s, string f, string ft = "")
        {
            name = n;
            tags = t;
            type = y;
            subject = s;
            fileName = f;
            if (ft == "")
            {
                fullText = "";
                Stream fileStream = File.OpenRead(GetPDFPath());
                PdfLoadedDocument loadedDocument = new PdfLoadedDocument(fileStream);
                foreach (PdfPageBase page in loadedDocument.Pages)
                {
                    fullText += page.ExtractText(true).ToLower() + " ";
                }
                loadedDocument.Close();
                File.WriteAllText(GetFullTextPath(), fullText);
            }
            else fullText = ft;
        }
        public DataFile(string n, string y, string s)
        {
            name = n;
            type = y;
            subject = s;
            tags = new string[0];
            fileName = "";
            fullText = "";
        }
        public void UpdateTags(string s)
        {
            string[] gs = s.Split(',');
            for (int i = 0; i < gs.Length; i++) gs[i] = gs[i].Trim();
            tags = gs;
        }
        public bool MatchTags(string[] ts, bool inFile)
        {
            if (ts.Length == 0) return true;
            bool result = true;
            foreach (string t in ts)
            {
                bool matchedTag = false;
                if (name.ToLower().Contains(t.ToLower()))
                    matchedTag = true;
                foreach (string s in tags)
                    if (s.ToLower().Contains(t.ToLower()))
                        matchedTag = true;
                if (inFile)
                    if ((fullText.Contains(t.ToLower()))) matchedTag = true;
                result = matchedTag && result;
            }
            return result;
        }
        public string GetPDFPath()
        {
            return Form1.path + type + "\\" + subject + "\\" + fileName + ".pdf";
        }
        public string GetTXTPath()
        {
            return Form1.path + type + "\\" + subject + "\\" + fileName + ".txt";
        }
        public string GetFullTextPath()
        {
            return Form1.path + type + "\\" + subject + "\\" + fileName + " Full Text.txt";
        }
        public int CompareTo(object obj)
        {
            DataFile f = (DataFile)obj;
            int firstComp = f.type.CompareTo(type);
            if (firstComp == 0)
            {
                int secondComp = f.subject.CompareTo(subject);
                if (secondComp == 0)
                {
                    return -f.name.CompareTo(name);
                }
                else return -secondComp;
            }
            else return -firstComp;
        }
        public override bool Equals(object obj)
        {
            DataFile d = (DataFile)obj;
            return d.name == name && d.subject == subject && d.type == type;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
