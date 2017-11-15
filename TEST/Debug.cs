using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Win32;
using System.Reflection;

namespace DEBUG
{
    class Debug
    {
        public static int debug = 0;
        private static StreamWriter debugFile;
        private static string debugFileName;
        public Debug()
        { }
        public static string GetFileName()
        {
            return debugFileName;
        }
        public static string GetDebugFile()
        {
            try
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey("Software", true);
                RegistryKey newkey = key.OpenSubKey(Assembly.GetExecutingAssembly().GetName().Name);
                return newkey.GetValue("File").ToString();
            }
            catch (Exception)
            {
                return null;
            }
        }
        public static bool TestDebugFile()
        {
            try
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey("Software", true);
                RegistryKey newkey = key.OpenSubKey(Assembly.GetExecutingAssembly().GetName().Name);
                if (newkey.GetValue("Ended").ToString() == "OK")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }

        }
        static private void SetEnded(string endVale)
        {
            try
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey("Software", true);
                RegistryKey newkey = key.CreateSubKey(Assembly.GetExecutingAssembly().GetName().Name);
                newkey.SetValue("Ended", endVale);
            }
            catch
            {
            }
        }
        static private void SetFileName(string name)
        {
            try
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey("Software", true);
                RegistryKey newkey = key.CreateSubKey(Assembly.GetExecutingAssembly().GetName().Name);
                newkey.SetValue("File", name);
            }
            catch
            {
            }
        }
        static public bool Open(string logName)
        {
             try
            {
                debugFileName = logName;
                debugFile = new StreamWriter(debugFileName, true, System.Text.Encoding.UTF8);
                debugFile.AutoFlush = true;
                SetFileName(logName);
                SetEnded("START");
            }
            catch(Exception )
            {
                return false;
            }
            return true;
        }
        static public bool Open(bool append)
        {
             try
             {
                 debugFileName = Path.GetTempFileName();
                 FileInfo fileInfo = new FileInfo(debugFileName);
                 fileInfo.Attributes = FileAttributes.Temporary;
                 debugFile = new StreamWriter(debugFileName, append, System.Text.Encoding.UTF8);
                 debugFile.AutoFlush = true;
                 SetFileName(debugFileName);
                 SetEnded("START");
            }
            catch(Exception )
            {
                return false;
            }
            return true;
        }
 
        static public bool Close()
        {
            if (debugFile != null)
            {
                SetEnded("OK");
                debugFile.Close();
            }
            return true;
        }
         ~Debug()
        {
            Close();
        }
        static public void Send(string str)
        {
            if (debugFile != null)
            {
                try
                {
                    lock(debugFileName)
                       debugFile.WriteLine(DateTime.Now.ToLongTimeString() +":"+DateTime.Now.Millisecond+ ": " + str);
                }
                catch(Exception e)
                {
                }
            }
        }
        static public void Send(string str, byte[] data)
        {
            try
            {
                if (debugFile != null)
                {
                    lock (debugFileName)
                        debugFile.Write(DateTime.Now.ToLongTimeString() +":"+DateTime.Now.Millisecond+ ": " + str);
                    for (int i = 0; i < data.Length; i++)
                    {
                        lock (debugFileName)
                            debugFile.Write(data[i].ToString("X2") + " ");
                    }
                    lock (debugFileName)
                        debugFile.WriteLine(" ");
                }
            }
            catch (Exception)
            { 
            }
        }

    }
}
