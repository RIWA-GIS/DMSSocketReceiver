using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace DMSSocketReceiver.dmshandler.impl
{
    /// <summary>
    /// Interaktionslogik für Window1.xaml
    /// </summary>
    public partial class SelectFileWindow : Window
    {
        public KeyValuePair<string, string> selEntry { get; private set; }

        public SelectFileWindow()
        {
            InitializeComponent();
            this.selEntry = default(KeyValuePair<string, string>);
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            this.selEntry = (KeyValuePair<string, string>)listBox.SelectedItem;
            this.Close();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            this.selEntry = default(KeyValuePair<string, string>);
            this.Close();
        }

        public static DMSDocument SelectDocument(IDictionary<string, string> previousDictionary)
        {
            IList<DMSDocument> docs = new List<DMSDocument>();
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                SelectFileWindow fw = new SelectFileWindow();
                fw.SetItems(previousDictionary);
                fw.ShowDialog();
                lock (docs)
                {
                    Monitor.Pulse(docs);
                }
                if (!default(KeyValuePair<string, string>).Equals(fw.selEntry))
                {
                    KeyValuePair<string, string> selItem = fw.selEntry;
                    docs.Add(new DMSDocument(selItem.Key, Path.GetFileName(selItem.Value)));
                }
            });
            lock (docs)
            {
                if (Monitor.Wait(docs))
                {

                }
            }
            if (docs.Count > 0)
                return docs[0];
            else
                return null;
        }

        private void SetItems(IDictionary<string, string> previousDictionary)
        {
            listBox.Items.Clear();
            foreach (KeyValuePair<string, string> prevEntry in previousDictionary)
            {
                listBox.Items.Add(prevEntry);
            }
        }
    }
}
