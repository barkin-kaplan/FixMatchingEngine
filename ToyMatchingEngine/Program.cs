using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using QuickFix;

using ToyMatchingEngine.Connection;

namespace ToyMatchingEngine
{
    class Program
    {
        private static string ConfigPath = "fix-dma.ini";
        static void Main(string[] args)
        {
            SessionSettings settingsAcceptor = new SessionSettings(ConfigPath);
            QuickFixAcceptor acceptor = QuickFixAcceptor.GetInstance();
            IMessageStoreFactory storeFactory = new FileStoreFactory(settingsAcceptor);
            ILogFactory logFactory = new FileLogFactory(settingsAcceptor);
            IAcceptor acceptorThread = new ThreadedSocketAcceptor(acceptor, storeFactory, settingsAcceptor, logFactory);
            acceptorThread.Start();
        }
    }
}
