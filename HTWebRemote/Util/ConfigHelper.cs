﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace HTWebRemote.Util
{
    class ConfigHelper
    {
        public static string WorkingPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        public static string DeviceFile = Path.Combine(WorkingPath, "HTWebRemoteDevices.txt");
        public static string browsePaths = Path.Combine(WorkingPath, "HTWebRemoteBrowsePaths.txt");
        public static string jsonButtonFiles = Path.Combine(WorkingPath, "HTWebRemoteButtons");

        public static string LocalIPAddress
        {
            get
            {
                string IP = "IPError";
                foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if ((item.NetworkInterfaceType == NetworkInterfaceType.Ethernet || item.NetworkInterfaceType == NetworkInterfaceType.Wireless80211) &&
                        !item.Description.ToLower().Contains("virtual") && !item.Name.ToLower().Contains("virtual") && item.OperationalStatus == OperationalStatus.Up)
                    {
                        foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork && !ip.Address.ToString().StartsWith("127"))
                            {
                                IP = ip.Address.ToString();
                            }
                        }
                    }
                }
                return IP;
            }
        }

        public static void Setup(string port)
        {
            string adminCMD = null;
            string firewall = RunCmd("netsh", "advfirewall firewall show rule name=HTWebRemote", false);
            if (!firewall.Contains("HTWebRemote") || !firewall.Contains(port))
            {
                adminCMD = $@"netsh advfirewall firewall add rule name=""HTWebRemote"" protocol=TCP dir=in localport={port} action=allow";
            }

            string urlacl = RunCmd("netsh", $"http show urlacl url=http://*:{port}/", false);
            if (!urlacl.Contains($"http://*:{port}/"))
            {
                if (adminCMD != null)
                {
                    adminCMD += " && ";
                }
                adminCMD += $"netsh http add urlacl url=http://*:{port}/ user=%computername%\\%username%";
            }

            if (adminCMD != null)
            {
                RunCmd("cmd", $"/C {adminCMD}", true);
            }

            FixMSEdge();
        }

        public static string RunCmd(string filename, string arguments, bool admin)
        {
            Process process = new Process();
            process.StartInfo.FileName = filename;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.CreateNoWindow = true;

            if(admin)
            {
                process.StartInfo.Verb = "runas";
            }
            else
            {
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
            }

            string output = null;
            try
            {
                process.Start();
                if (!admin)
                {
                    output = process.StandardOutput.ReadToEnd();
                }
                process.WaitForExit();
            }
            catch(Exception e)
            {
                MessageBox.Show($"Error setting up networking permissions.\n\n{e.AllMessages()}", "Error");
            }

            return output;
        }

        public static bool CheckRegKey(string path, string key)
        {
            try
            {
                RegistryKey regKey = Registry.CurrentUser.OpenSubKey(path, true);
                return (regKey.GetValueNames().Contains(key));
            }
            catch
            {
                return false;
            }
        }

        public static string GetRegKey(string path, string key)
        {
            try
            {
                RegistryKey regKey = Registry.CurrentUser.OpenSubKey(path, true);
                return regKey.GetValue(key).ToString();
            }
            catch
            {
                return "";
            }
        }

        public static string GetEmbeddedResource(string filename)
        {
            string header = "";
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string resourceName = assembly.GetManifestResourceNames().Single(str => str.EndsWith(filename));

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    header = reader.ReadToEnd();
                }
            }
            catch { }

            return header;
        }

        //stops MS Edge from blocking local IPs that failed to load
        public static void FixMSEdge()
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Classes\\Local Settings\\Software\\Microsoft\\Windows\\CurrentVersion\\AppContainer\\Storage\\microsoft.microsoftedge_8wekyb3d8bbwe\\MicrosoftEdge\\TabProcConfig", true);
                if (key != null)
                {
                    string[] values = key.GetValueNames();

                    foreach (string value in values)
                    {
                        key.DeleteValue(value, false);
                    }
                }
            }
            catch { }
        }

        public static string ConvertLegacyColor(string color)
        {
            if(color is null)
            {
                return "#FFFFFF";
            }
            if (color.StartsWith("#"))
            {
                return color;
            }
            else
            {
                string colorVal = "#808080";
                switch (color)
                {
                    case "Blue":
                        colorVal = "#007BFF";
                        break;
                    case "Green":
                        colorVal = "#28A745";
                        break;
                    case "Red":
                        colorVal = "#DC3545";
                        break;
                    case "Orange":
                        colorVal = "#FFC107";
                        break;
                    case "Teal":
                        colorVal = "#17A2C8";
                        break;
                    case "Gray":
                        colorVal = "#6C757D";
                        break;
                    case "White":
                        colorVal = "#F8F9FA";
                        break;
                    case "Black":
                        colorVal = "#343A40";
                        break;
                    default:
                        break;
                }

                return colorVal;
            }
        }

        public static void ConvertLegacyFiles()
        {
            try
            {
                foreach (string file in Directory.GetFiles(WorkingPath))
                {
                    if (file.Contains("HTPC"))
                    {
                        File.Move(file, file.Replace("HTPC", "HTWeb"));
                    }
                }
            }
            catch { }
        }
    }

    public static class MyExtensions
    {
        public static IEnumerable<string> CustomSort(this IEnumerable<string> list)
        {
            int maxLen = list.Select(s => s.Length).Max();

            return list.Select(s => new
            {
                OrgStr = s,
                SortStr = Regex.Replace(s, @"(\d+)|(\D+)", m => m.Value.PadLeft(maxLen, char.IsDigit(m.Value[0]) ? ' ' : '\xffff'))
            })
            .OrderBy(x => x.SortStr)
            .Select(x => x.OrgStr);
        }
    }
}