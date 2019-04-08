using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace SpExam
{
    public partial class Form1 : Form
    {
        Thread thSearch = null;
        EventWaitHandle evn = null;
        List<string> reportInfo = new List<string>();

        string[] dictionary = null;
        public Form1()
        {
            InitializeComponent();
            MessageBox.Show("Словарь - это файл с плохими словами, которые нужно найти при сканировании. Можете сформировать такой файл сами - просто вводите туда слова через пробел, а потом укажите на этот файл. Далее, нужно указать на директорию с файлами для сканирования, в нем могут содержаться файлы и другие папки. Путь копирования - это путь, куда будут копироваться файлы, в которых нашлись плохие слова. Так же по этому пути будет находиться файл отчета \"Report.txt\"", "Инструкция пользователя", MessageBoxButtons.OK, MessageBoxIcon.Information);
            txtLog.ScrollBars = ScrollBars.Vertical;
        }

        private void InitDictionary()
        {
            List<string> lines = new List<string>();
            using (StreamReader reader = new StreamReader(txtPathToDictionary.Text))
            {
                while (!reader.EndOfStream) lines.Add(reader.ReadLine());
            }
            foreach(var row in lines)
            {
                dictionary = row.Split(' ');
            }
        }

        private void btnPathToSearch_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                txtPathToSearch.Text = fbd.SelectedPath;
            }
        }

        private void btnPathToCopy_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                txtPathToCopy.Text = fbd.SelectedPath;
            }
        }

        private void btnPathToDictionary_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtPathToDictionary.Text = ofd.FileName;
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            InitDictionary();

            thSearch = new Thread(SearchRoutine);
            thSearch.IsBackground = true;
            thSearch.Start();
        }

        private void SearchRoutine()
        {   
            DirectoryInfo directoryInfo = new DirectoryInfo(txtPathToSearch.Text);

            DirectoryInfo[] subDirectories = directoryInfo.GetDirectories();

            //replaceWords(directoryInfo, dictionary);

            replaceWords(directoryInfo, dictionary);
                

            Report(reportInfo);

            DialogResult res = MessageBox.Show("Хотите открыть файл отчета?", "Конец работы программы", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (res == DialogResult.Yes) Process.Start(txtPathToCopy.Text + "\\Report.txt");
            else Application.Exit();
        }

        private void replaceWords(DirectoryInfo di, string[] dictionary)
        {
            //reportInfo = new List<string>();

            FileInfo[] files = di.GetFiles("*.txt");

            progressBar.Invoke(new Action<int>(
            (x) => 
            {
                    progressBar.Maximum = x;
                    progressBar.Update();
             }), files.Length);

            for (int i = 1; i <= files.Length; i++)
            {
                string textFromFile = string.Empty;
                string[] wordsFromFile = null;
                int swearCount = 0;

                using (StreamReader reader = File.OpenText(files[i-1].FullName))
                {
                    textFromFile = reader.ReadToEnd();
                }

                wordsFromFile = textFromFile.Split(' ');

                foreach(var swear in dictionary)
                {
                    foreach(var word in wordsFromFile)
                    {
                        if(swear == word)
                        {
                            swearCount++;
                            textFromFile = textFromFile.Replace(word, "*******");
                            reportInfo.Add("Путь к файлу: " + files[i-1].FullName
                                + " | Размер файла: " + files[i-1].Length
                                + " | Замененное слово: " + swear + ' ');

                            using (StreamWriter write = new StreamWriter(txtPathToCopy.Text + "\\" + files[i - 1].Name, false))
                            {
                                write.Write(textFromFile);
                            }
                        }
                    }   
                }

                progressBar.Invoke(new Action<int>((x)=> {
                    progressBar.Value = x;
                    progressBar.Update();
                }), i);
            }

            if (di.GetDirectories().Length != 0)
            {
                DirectoryInfo[] subDirs = di.GetDirectories();

                foreach(var item in subDirs) replaceWords(item, dictionary);
            }
        }

        private void Report(List<string> reportInfo)
        {
            string textFromReport = string.Empty;
            string[] wordsFromReport = null;
            SortedDictionary<int, string> raiting = new SortedDictionary<int, string>();

            using (StreamWriter write = new StreamWriter(txtPathToCopy.Text + "\\Report.txt", false))
            {
                foreach (var item in reportInfo)
                {
                    write.WriteLine(item);
                }
            }

            using (StreamReader read = new StreamReader(txtPathToCopy.Text + "\\Report.txt", false))
            {
                textFromReport = read.ReadToEnd();
            }

            wordsFromReport = textFromReport.Split(' ');

            using (StreamWriter write = new StreamWriter(txtPathToCopy.Text + "\\Report.txt", false))
            {
                int top = 1;

                write.Write("Топ замененных плохих слов:\r\n");

                txtLog.Invoke(new Action<string>((x) =>
                {
                    txtLog.Text += x;
                    txtLog.Update();
                }), "Данный отчет так же записан в соответствующий файл в указанной папке для копирования.\r\n\r\n");

                txtLog.Invoke(new Action<string>((x) =>
                {
                    txtLog.Text += x;
                    txtLog.Update();
                }), "Топ замененных плохих слов:\r\n");

                foreach (var item in dictionary)
                {
                    //write.WriteLine($"Слово {item} встречалось {Raiting(item, wordsFromReport)} раза!");
                    if(!raiting.ContainsKey(Raiting(item, wordsFromReport)))
                    {
                        raiting.Add(Raiting(item, wordsFromReport), item);
                    }
                }

                for (int index = raiting.Count; index > 0; index--)
                {
                    var item = raiting.ElementAt(index-1);
                    var itemKey = item.Key;
                    var itemValue = item.Value;
                    write.WriteLine($"{top}) Слово {item.Value} встречалось {item.Key} раза!");

                    txtLog.Invoke(new Action<string>((x)=> 
                    {
                        txtLog.Text += x;
                        txtLog.Update();
                    }), $"{top}) Слово {item.Value} встречалось {item.Key} раза!" + "\r\n");
                    top++;
                }

                write.Write("\r\n\r\n" + textFromReport);

                txtLog.Invoke(new Action<string>((x) =>
                {
                    txtLog.Text += x;
                    txtLog.Update();
                }), "\r\n" + textFromReport);
            }
        }

        private int Raiting(string swear, string[] wordsFromFile)
        {
            int count = 0;

            foreach(var item in wordsFromFile)
            {
                if (item == swear) count++;
            }

            return count;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnPause_Click(object sender, EventArgs e)
        {
            try
            {
                thSearch.Suspend();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnResume_Click(object sender, EventArgs e)
        {
            try
            {
                thSearch.Resume();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
