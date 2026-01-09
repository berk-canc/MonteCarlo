using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using System.Runtime.CompilerServices;

namespace MonteCarlo
{
    public static class Logger
    {
        private static StorageFile   m_file    = null;
        private static SemaphoreSlim m_mutex   = new SemaphoreSlim(1);
        private static string        m_content = "";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        async public static void Init()
        {
            await m_mutex.WaitAsync();

            if (m_file == null)
            {
                m_file = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync("MonteCarloLogs.txt", CreationCollisionOption.ReplaceExisting);

                Log("Initing logger...");
            }

            m_mutex.Release();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        async public static void Log(string msg)
        {
            await m_mutex.WaitAsync();

            if (m_file == null || msg.Trim() == "")
            {
                goto Exit;
            }

            m_content += msg + "\r\n";
            //TODO: if file is being written, throws FileLoadException
            await FileIO.WriteTextAsync(m_file, m_content);

        Exit:
            m_mutex.Release();
            return;
        }
    }
}
