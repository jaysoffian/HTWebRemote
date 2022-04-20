﻿using System.Windows.Forms;

namespace HTWebRemote.Util
{
    static class ErrorHandler
    {
        public static void SendError(string error)
        {
            if (ConfigHelper.CheckRegKey(@"SOFTWARE\HTWebRemote", "ShowErrors"))
            {
                MessageBox.Show(error, "Error");
            }
        }
    }
}