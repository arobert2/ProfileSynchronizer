using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Configuration;
using System.Collections.Specialized;
using System.Threading;
using RegistryTools;

namespace ProfileSynchronizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        RegTools RegistryTools = new RegTools();
        string loadkey = ConfigurationManager.AppSettings["RegistryLoadName"];
        string rootkey = ConfigurationManager.AppSettings["RegistryRootName"];

        public MainWindow()
        {
            InitializeComponent();
            RegistryTools.LoadDefaultHive();
            TreeViewItem tvi = new TreeViewItem { Header = loadkey };
            tvi.Tag = rootkey + @"\" + loadkey;
            trvRegistryKeys.Items.Add(tvi);
        }

        private void trvRegistryKeys_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeViewItem path = (TreeViewItem)trvRegistryKeys.SelectedItem;
            RegValueData[] regdata = RegistryTools.GetKeyValues(path.Tag as string);

            if (path != null && path.Items.Count == 0)
            {
                string[] newkeys = RegistryTools.GetChildKeys((string)path.Tag);
                foreach (string k in newkeys)
                    ((TreeViewItem)trvRegistryKeys.SelectedItem).Items.Add(new TreeViewItem() { Header = k, Tag = (string)path.Tag + @"\"+ k });
                path.IsExpanded = true;
            }

            lvRegistryKeyValues.Items.Clear();
            if(regdata.Length == 0 || regdata[0].Name != "")
                lvRegistryKeyValues.Items.Add(new RegValueData() { Name="(Default)", RegType = RegistryValueKind.String.ToString(), Value = "" });

            foreach (RegValueData rvd in regdata)
            {
                if (rvd.Name == "")
                    rvd.Name = "(Default)";
                lvRegistryKeyValues.Items.Add(rvd);
            }
        }
    }
}
