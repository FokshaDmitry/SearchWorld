using Microsoft.Win32;
using System;
using System.IO;
using System.Security;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Media;

namespace SearchWorld
{
    public partial class MainWindow : Window
    {
        private OpenFileDialog openFileDialog1;
        private SaveFileDialog saveFileDialog1;
        private String ConsoleText;
        private string[] Words;
        private int CountFindWord;
        private int DontCountFindWord;
        private int cnt;
        private object loker;
        private static Semaphore sem;
        private CancellationTokenSource cts;
        private DriveInfo[] allDrives;
        
        public MainWindow()
        {
            InitializeComponent();
            openFileDialog1 = new OpenFileDialog()
            {
                Filter = "Text files (*.txt)|*.txt",
                Title = "Open text file"
            };
            saveFileDialog1 = new SaveFileDialog()
            { 
                FileName = "Edit Text",
                Filter = "Text files (*.txt)|*.txt",
                Title = "Save text file"
            };
            CountFindWord = 0;
            sem = new Semaphore(1, 9);
            loker = new object();
            cts = new CancellationTokenSource();
            allDrives = DriveInfo.GetDrives();
        }
        public void OpenFileDialogForm()
        {
            Dispatcher.Invoke(new Action(() => openFileDialog1.ShowDialog()));            
            if (openFileDialog1.FileName != "")
            {
                try
                { 
                    Dispatcher.Invoke(new Action(()=> NewTextView.Text = File.ReadAllText(openFileDialog1.FileName)));
                    Dispatcher.Invoke(new Action(()=> link.Text = openFileDialog1.FileName));
                }
                catch (SecurityException ex)
                {
                    MessageBox.Show($"Security error.\n\nError message: {ex.Message}\n\n" +
                    $"Details:\n\n{ex.StackTrace}");
                }

            }
        }
        public void SaveFileForm()
        {
            Dispatcher.Invoke(new Action(() => saveFileDialog1.ShowDialog()));
            if (saveFileDialog1.CheckPathExists)
            {
                try
                {
                    File.WriteAllText(saveFileDialog1.FileName, ConsoleText);
                    MessageBox.Show("Save Ok!");
                }
                catch (SecurityException ex)
                {
                    MessageBox.Show($"Security error.\n\nError message: {ex.Message}\n\n" +
                    $"Details:\n\n{ex.StackTrace}");
                }
                
            }
        }
        public void SearchWorld(string Word)
        {
            string star = "*";
            star = star.PadLeft(Word.Length, '*');
            ConsoleText = Dispatcher.Invoke(() => NewTextView.Text);
            if (!String.IsNullOrEmpty(ConsoleText))
            {
                do
                {
                    if (ConsoleText.IndexOf(Word, StringComparison.CurrentCultureIgnoreCase) != -1)
                    {
                        int tmp = ConsoleText.IndexOf(Word, StringComparison.CurrentCultureIgnoreCase);
                        ConsoleText = ConsoleText.Remove(tmp, Word.Length);
                        for (int j = 0; j < Word.Length; j++)
                        {
                            ConsoleText = ConsoleText.Insert(tmp, "*");
                        }
                        CountFindWord++;
                    }
                    else
                    {
                        DontCountFindWord++;
                        cnt--;
                        if (cnt == 0)
                        {
                            Dispatcher.Invoke(new Action(() => NewTextView.Text = ConsoleText));
                            Dispatcher.Invoke(new Action(() => FindWord.Text = $"Find Word: {CountFindWord.ToString()}"));
                            Dispatcher.Invoke(new Action(() => DontFindWord.Text = $"Dont find Word: {DontCountFindWord.ToString()}"));
                        }
                        return;
                    }
                } while (ConsoleText.IndexOf(Word, StringComparison.CurrentCultureIgnoreCase) != -1);
                cnt--;
                if (cnt == 0)
                {
                    Dispatcher.Invoke(new Action(() => NewTextView.Text = ConsoleText));
                    Dispatcher.Invoke(new Action(() => FindWord.Text = $"Find Word: {CountFindWord.ToString()}"));
                    Dispatcher.Invoke(new Action(() => DontFindWord.Text = $"Dont find Word: {DontCountFindWord.ToString()}"));
                }
            }
            else
            {
                MessageBox.Show("Add file or write Text");
            }
            
        }
        public void GetAllFiles(string rootDirectory, string fileExtension)
        {
            sem.WaitOne();
            try
            {
                string[] directories = Directory.GetDirectories(rootDirectory);
                string[] files = Directory.GetFiles(rootDirectory, fileExtension);
                if (files.Length != 0)
                {
                    foreach (var file in files)
                    {
                        foreach (var word in Words)
                        {
                            try
                            {
                                if (File.ReadAllText(file).ToString().IndexOf(word) > -1)
                                {
                                    lock (this)
                                    {
                                        Dispatcher.Invoke(() => ListPath.Items.Add(file));
                                        Dispatcher.Invoke(() => FindFile.Text = $"Find fils: {ListPath.Items.Count}");
                                    }
                                    break;
                                }
                            }
                            catch (IOException)
                            {
                                break;
                            }

                        }
                    }
                }
                foreach (string path in directories)
                {
                    Dispatcher.Invoke(() => txtlink.Text = path);
                    Task.Run(() => GetAllFiles(path, fileExtension), cts.Token);

                }
            }
            catch (UnauthorizedAccessException)
            {
                sem.Release();
                return;
            }

            sem.Release();
        }
        public bool SaveWord()
        {
            string wordtmp = Dispatcher.Invoke(()=> WordSearch.Text);
            if (!String.IsNullOrEmpty(wordtmp))
            {
                int tmp = 33;
                for (int i = 0; i <= 104; i++)
                {
                    wordtmp = wordtmp.Replace($"{(char)(i + tmp)}", "");
                    if (i == 31) tmp += 26;
                    if (i == 37) tmp += 26;
                    if (i == 41) tmp += 2;
                }
                Words = wordtmp.Split(' ');
                cnt = Words.Length;
                return true;
            }
            else
            {
                MessageBox.Show("Write word");
                return false;
            }
        }
        private void EnterFile_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(link.Text))
            {
                Task.Run(OpenFileDialogForm);
            }
            else
            {
                try
                {
                    NewTextView.Text = File.ReadAllText(link.Text);
                }
                catch (SecurityException ex)
                {
                    MessageBox.Show($"Security error.\n\nError message: {ex.Message}\n\n" +
                    $"Details:\n\n{ex.StackTrace}");
                }
            }
        }

        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(SaveFileForm);
        }


        private void EditText_Click(object sender, RoutedEventArgs e)
        {
            if (SaveWord())
            {
                foreach (var word in Words)
                {
                    Task.Run(() => SearchWorld(word));
                }
            }

        }

        private void Search_File_Click(object sender, RoutedEventArgs e)
        {
            if (SaveWord())
            {
                ListPath.Items.Clear();
                foreach (var drider in allDrives)
                {
                    Task.Run(() => GetAllFiles(drider.Name, "*.txt"), cts.Token);
                }
            }
            
        }
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (ListPath.SelectedItem != null)
            {
                link.Text = ListPath.SelectedItem.ToString();
            }
            else
            {
                MessageBox.Show("Search File");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                cts?.Cancel();
                cts?.Dispose();
                sem?.Close();
                sem?.Dispose();
                cts = new CancellationTokenSource();
                sem = new Semaphore(1, 9);
                MessageBox.Show("Searshing Stop");
                txtlink.Text = "";
            }
            catch (Exception)
            {

                throw;
            }


        }
    }
}
